using MeloongCore;
using Newtonsoft.Json.Linq;

namespace PCLLF.Native.Facade.Tests;
// Only the genuine backend's injectable transport runs here. This is neither
// the four-argument updater helper protocol nor product entry cancellation.
internal static class FileTaskUpdateCancellationScenarios
{
    internal static void Run(string root)
    {
        const string executableName = "PCL-Living-Fork-99.0.0.exe";
        const string releaseBase = "https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/v99.0.0/";
        JObject info = new()
        {
            ["tag_name"] = "v99.0.0",
            ["draft"] = false,
            ["prerelease"] = false,
            ["assets"] = new JArray(new JObject { ["name"] = executableName, ["size"] = 128, ["browser_download_url"] = releaseBase + executableName }, new JObject { ["name"] = "SHA256SUMS.txt", ["size"] = 200, ["browser_download_url"] = releaseBase + "SHA256SUMS.txt" })
        };
        object transport = Activator.CreateInstance(Native.Type("PCL.ModLauncherUpdate+UpdateTransport"), true)!;
        int reads = 0, downloads = 0, fallbacks = 0;
        OperationCanceledException sentinel = new("Synthetic actual transport cancellation.");
        Native.Set(transport, "ReadText", new Func<string, string>(address =>
        {
            reads++;
            Native.Check(address == releaseBase + "SHA256SUMS.txt", "Cancellation fixture requested an unexpected hash endpoint.");
            return new string ('0', 64) + "  " + executableName;
        }));
        Native.Set(transport, "ReadJson", new Func<string, JObject>(_ =>
        {
            fallbacks++;
            throw new InvalidOperationException("Canceled download must not read fallback release metadata.");
        }));
        Native.Set(transport, "ReadReleaseList", new Func<string, JArray>(_ =>
        {
            fallbacks++;
            throw new InvalidOperationException("Canceled download must not enumerate fallback releases.");
        }));
        string target = Path.Combine(root, "updater-canceled-" + Guid.NewGuid().ToString("N") + ".exe");
        Native.Check(!File.Exists(target), "Cancellation fixture target unexpectedly exists.");
        Native.Set(transport, "Download", new Action<string, string, FileChecker>((address, destination, checker) =>
        {
            downloads++;
            Native.Check(address == releaseBase + executableName && Native.Full(destination) == Native.Full(target), "Actual backend changed the confirmed download target.");
            throw sentinel;
        }));
        bool canceled = false;
        try
        {
            Native.Static("PCL.ModLauncherUpdate", "DownloadUpdate", info, target, transport);
        }
        catch (OperationCanceledException error)when (ReferenceEquals(error, sentinel))
        {
            canceled = true;
        }

        Native.Check(canceled && reads == 1 && downloads == 1 && fallbacks == 0 && !File.Exists(target), "Actual Main swallowed cancellation, retried a fallback or wrote a staged executable.");
        Console.WriteLine("PASS native file-tasks updater backend cancellation: same OperationCanceledException, one actual transport attempt, no fallback and no staged executable; helper protocol not exercised.");
    }
}
