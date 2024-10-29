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


if(args.Contains("--sharpie"))
{
    foreach(var m in mapping)
    {
        var frameworkName = m.Key.Replace(".xcframework", ".framework");
        var workingDirectory = Path.Combine(repositoryRoot.FullName, m.Value);
        using var cmd = new Process();
        cmd.StartInfo.FileName = "sharpie";
        var headerPath = $"libs/{m.Key}/ios-arm64/{frameworkName}/Headers";
        var headerFiles = Directory.GetFiles(Path.Combine(workingDirectory, headerPath), "*.h").Select(f => headerPath + "/" + Path.GetFileName(f)).ToArray();
        cmd.StartInfo.Arguments = $"bind -sdk=iphoneos18.0 -output=sharpie --namespace={m.Value} --scope={headerPath} {string.Join(" ", headerFiles)} -c -I{headerPath} -arch arm64";
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

    // Skipping all .swiftmodule files, they are not needed during runtime
    // and cause problems due to MAX_PATH exceeding in windows.
    // https://github.com/xamarin/xamarin-macios/issues/21111#issuecomment-2334333870
    if(entry.FullName.Contains(".swiftmodule/"))
    {
        continue;
    }

    var kitName = entry.FullName.Substring(RootFolder.Length, endOfKitName - RootFolder.Length);

    if (mapping.TryGetValue(kitName, out var libName))
    {
        var relativePath = entry.FullName.Substring(RootFolder.Length + kitName.Length + 1);
        var path = Path.Combine(repositoryRoot.FullName, libName, "libs", kitName, relativePath);

        Console.WriteLine($"Extracting {entry.FullName} to {path}");

        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        entry.ExtractToFile(path);
    }
}

