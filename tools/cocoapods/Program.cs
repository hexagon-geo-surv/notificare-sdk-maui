using System.Diagnostics;
using System.IO.Compression;

var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["NotificareKit.xcframework"] = "Notificare.iOS.Bindings.Core",
    ["NotificareUtilitiesKit.xcframework"] = "Notificare.iOS.Bindings.Utilities",
    ["NotificareInAppMessagingKit.xcframework"] = "Notificare.iOS.Bindings.InAppMessaging",
    ["NotificareInboxKit.xcframework"] = "Notificare.iOS.Bindings.Inbox",
    ["NotificareNotificationServiceExtensionKit.xcframework"] = "Notificare.iOS.Bindings.NotificationServiceExtension",
    ["NotificarePushKit.xcframework"] = "Notificare.iOS.Bindings.Push",
    ["NotificarePushUIKit.xcframework"] = "Notificare.iOS.Bindings.Push.UI"
};

const string RootFolder = "Notificare/";
var repositoryRoot = new DirectoryInfo(Environment.CurrentDirectory);
while (!File.Exists(Path.Combine(repositoryRoot.FullName, "Notificare.sln")))
{
    repositoryRoot = repositoryRoot.Parent ?? throw new InvalidOperationException("Could not find repository root");
}


if(args.Contains("--swift"))
{
    foreach(var m in mapping)
    {
        var outputPath = $"{repositoryRoot.FullName}/temp/bindings/{m.Key}/";
        Directory.CreateDirectory(outputPath);
        var frameworkName = m.Key.Replace(".xcframework", "");

        var swiftBindings = Path.Combine(repositoryRoot.FullName, "dotnet-runtimelab/artifacts/bin/Swift.Bindings/Debug/net9.0/Swift.Bindings.dll");
        var abiJson = Path.Combine(repositoryRoot.FullName,
            $"temp/{frameworkName}.xcframework/ios-arm64/{frameworkName}.framework/Modules/{frameworkName}.swiftmodule/arm64-apple-ios.abi.json");
        var dylib = Path.Combine(repositoryRoot.FullName,
            $"temp/{frameworkName}.xcframework/ios-arm64/{frameworkName}.framework/{frameworkName}");

        // the swift generator needs the dylib to be present in the same folder as the abi.json
        var dylibTargetPath = Path.Combine(Path.GetDirectoryName(abiJson)!, "lib" + Path.GetFileName(abiJson).Replace(".abi.json", "") + ".dylib");
        Console.WriteLine($"Copying {dylib} to {dylibTargetPath}");
        File.Copy(dylib, dylibTargetPath, true);

        if(!File.Exists(abiJson))
        {
            Console.WriteLine($"Error, wrong path to abi.json: {abiJson}");
            Environment.Exit(1);            
        }

        Exec(Path.Combine(repositoryRoot.FullName, "dotnet-runtimelab/.dotnet/dotnet"), 
            $"{swiftBindings} --swiftabi {abiJson} -o {outputPath}", repositoryRoot.FullName);
    }
    return;
}



var version = "4.0.0";
var url = $"https://cdn.notifica.re/libs/ios/{version}/cocoapods.zip";

using var client = new HttpClient();
Console.WriteLine($"Downloading {url}");
var data = await client.GetByteArrayAsync(url);

using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);


foreach (var m in mapping)
{
    var path = Path.Combine(repositoryRoot.FullName, m.Value, "libs");
    if (Directory.Exists(path))
    {
        Directory.Delete(path, true);
    }
}


foreach (var entry in zip.Entries)
{
    if (entry.FullName.EndsWith("/") || !entry.FullName.StartsWith(RootFolder))
    {
        continue;
    }

    var endOfKitName = entry.FullName.IndexOf('/', RootFolder.Length);
    if (endOfKitName == -1)
    {
        continue;
    }

    var kitName = entry.FullName.Substring(RootFolder.Length, endOfKitName - RootFolder.Length);

    if (mapping.TryGetValue(kitName, out var libName))
    {
        var relativePath = entry.FullName.Substring(RootFolder.Length + kitName.Length + 1);


        // Skipping all .swiftmodule files, they are not needed during runtime
        // and cause problems due to MAX_PATH exceeding in windows.
        // https://github.com/xamarin/xamarin-macios/issues/21111#issuecomment-2334333870
        if(!entry.FullName.Contains(".swiftmodule/"))
        {
            var path = Path.Combine(repositoryRoot.FullName, libName, "libs", kitName, relativePath);
            Console.WriteLine($"Extracting {entry.FullName} to {path}");
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            entry.ExtractToFile(path);
        }

        var full = Path.Combine(repositoryRoot.FullName, "temp", kitName, relativePath);
        Console.WriteLine($"Extracting {entry.FullName} to {full}");
        var fullDir = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(fullDir);
        entry.ExtractToFile(full, true);
    }
}



void Exec(string filename, string arguments, string workingDirectory)
{
    using var cmd = new Process();
    cmd.StartInfo.FileName = filename;
    cmd.StartInfo.Arguments = arguments;
    cmd.StartInfo.RedirectStandardOutput = true;
    cmd.StartInfo.RedirectStandardError = true;
    cmd.OutputDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Console.WriteLine(e.Data);
        }
    };

    cmd.ErrorDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Console.WriteLine(e.Data);
        }
    };
    cmd.StartInfo.CreateNoWindow = true;
    cmd.StartInfo.UseShellExecute = false;
    cmd.StartInfo.WorkingDirectory = workingDirectory;
    Console.WriteLine($"Running: {cmd.StartInfo.FileName} {cmd.StartInfo.Arguments} in {cmd.StartInfo.WorkingDirectory}");
    cmd.Start();
    cmd.BeginOutputReadLine();
    cmd.BeginErrorReadLine();
    cmd.WaitForExit();
    Console.WriteLine($"Finished: {cmd.StartInfo.FileName} {cmd.StartInfo.Arguments} with exit code {cmd.ExitCode}");
    if(cmd.ExitCode != 0)
    {
        Environment.Exit(cmd.ExitCode);
    }
}