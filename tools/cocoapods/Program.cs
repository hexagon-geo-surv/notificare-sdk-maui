using System.IO.Compression;

var version = "4.0.0";
var url = $"https://cdn.notifica.re/libs/ios/{version}/cocoapods.zip";

using var client = new HttpClient();
Console.WriteLine($"Downloading {url}");
var data = await client.GetByteArrayAsync(url);

using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);

var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["NotificareKit.xcframework"] = "Notificare.iOS.Bindings.Core",
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
        var path = Path.Combine(repositoryRoot.FullName, libName, "libs", kitName, relativePath);
        Console.WriteLine($"Extracting {entry.FullName} to {path}");

        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        entry.ExtractToFile(path);
    }
}