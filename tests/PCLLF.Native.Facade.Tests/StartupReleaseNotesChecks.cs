using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;

internal static class StartupReleaseNotesChecks
{
    internal static void Run()
    {
        const string version = "1.4.0", summary = "- 汇总四个预发布版\r\n- 本次配置与世界数据修复";
        const string markdown = "# 当前版本更新\n\n**完整发布说明**\n\n- 索引与主题\n- 数据保护\n";
        const string exact = "https://api.github.com/repos/DDZmumo/PCL-Living-Fork/releases/tags/v1.4.0";
        object github = Native.Enum("PCL.ModLauncherUpdate+UpdateSource", "GitHub")!, cnb = Native.Enum("PCL.ModLauncherUpdate+UpdateSource", "Cnb")!;
        bool Uses(string name, bool preview, ModBase.BuildTypes build) => (bool)Native.Static("PCL.ModLauncherUpdate", "UsesPublishedReleaseNotes", name, preview, build)!;
        Native.Check(Uses(version, false, ModBase.BuildTypes.Release) && !Uses("dev_edition", false, ModBase.BuildTypes.Release) &&
            !Uses("1.4.0-pre-4", false, ModBase.BuildTypes.Release) && !Uses(version, true, ModBase.BuildTypes.Release) &&
            !Uses(version, false, ModBase.BuildTypes.Debug), "Startup release messages leaked into development/prerelease builds.");
        JObject Release(string tag = "v1.4.0") => new() { ["tag_name"] = tag, ["draft"] = false, ["prerelease"] = false, ["body"] = markdown };
        object Read(string name, object source, Func<string, JObject> request)
        {
            object transport = Activator.CreateInstance(Native.Type("PCL.ModLauncherUpdate+UpdateTransport"), true)!;
            Native.Field(transport.GetType(), "ReadJson").SetValue(transport, request);
            return Native.Static("PCL.ModLauncherUpdate", "ReadCurrentReleaseNotes", name, summary, source, transport)!;
        }
        List<string> requests = new();
        object online = Read(version, cnb, url => { requests.Add(url); return url == exact ? Release() : Release("v1.4.1"); });
        Native.Check((bool)Native.Get(online, "FromRelease")! && (string)Native.Get(online, "Markdown")! == markdown &&
            requests.Count == 2 && requests[1] == exact && !requests.Any(url => url.EndsWith("/latest", StringComparison.Ordinal)),
            "A newer mirror release replaced the current-version startup message or bypassed the exact tag request.");
        Native.Check((string)Native.Get(online, "Url")! == "https://github.com/DDZmumo/PCL-Living-Fork/releases/tag/v1.4.0", "Startup release page is not pinned to its displayed version.");
        var view = (FlowDocumentScrollViewer)Activator.CreateInstance(Native.Type("PCL.ReleaseNotesView"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object?[] { Native.Get(online, "Markdown"), (Action<string>)(_ => throw new Exception("No browser permitted.")) }, null)!;
        string rendered = new TextRange(view.Document.ContentStart, view.Document.ContentEnd).Text;
        Native.Check(rendered.Contains("完整发布说明", StringComparison.Ordinal) && !rendered.Contains("**", StringComparison.Ordinal) && view.Document.Blocks.OfType<List>().Any(),
            "The startup release body was not rendered through the actual native Markdown view.");
        foreach (string invalid in new[] { "wrong-tag", "draft", "preview", "empty", "status-type", "oversize" })
        {
            JObject release = Release();
            if (invalid == "wrong-tag") release["tag_name"] = "v1.3.5";
            if (invalid == "draft") release["draft"] = true;
            if (invalid == "preview") release["prerelease"] = true;
            if (invalid == "empty") release["body"] = " ";
            if (invalid == "status-type") release["draft"] = "false";
            if (invalid == "oversize") release["body"] = new string('x', 1024 * 1024 + 1);
            object fallback = Read(version, github, _ => release);
            Native.Check(!(bool)Native.Get(fallback, "FromRelease")! && (string)Native.Get(fallback, "Markdown")! == summary,
                "Invalid release metadata replaced the local startup summary: " + invalid);
        }
        object offline = Read(version, github, _ => throw new IOException("Fixture offline."));
        Native.Check(!(bool)Native.Get(offline, "FromRelease")! && (string)Native.Get(offline, "Markdown")! == summary, "Offline startup lost its current local summary.");
        Read("dev_edition", github, _ => throw new Exception("Development notices must not request a published release."));
        bool canceled = false;
        try { Read(version, github, _ => throw new OperationCanceledException()); } catch (OperationCanceledException) { canceled = true; }
        Native.Check(canceled, "Canceled startup release lookup became another popup instead of stopping.");
        Console.WriteLine("PASS startup release notes: exact current tag, preferred mirror fallback, draft/type/size checks, offline local summary, real WPF Markdown and cancellation; no HTTP, modal, profile writes or browser.");
    }
}
