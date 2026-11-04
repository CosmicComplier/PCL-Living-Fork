using System.Text.RegularExpressions;
using PCLCS;
using static PCLLF.Native.Facade.Tests.EnvironmentHistoryChecks;

namespace PCLLF.Native.Facade.Tests;

internal static class EnvironmentRecentChangesChecks
{
    internal static void Run(Scope scope)
    {
        string version = Path.Combine(scope.Root, "recent/minecraft/versions/example");
        Put(Path.Combine(version, "example.json"), "{\"id\":\"example\",\"inheritsFrom\":\"1.20.1\"}");
        foreach (var file in new[] { ("mods/update-1.0.jar", "old-update"), ("mods/disabled.jar", "same-disable-content"), ("mods/re-enabled.jar.old", "same-enable-content"), ("mods/content.jar", "old-content"), ("config/private.toml", "api_key=fixture-private-before"), ("config/deleted.toml", "old-config"), ("saves/world/level.dat", "original-world"), ("screenshots/fixture.png", "original-shot"), ("config/xaero/waypoints.txt", "original-markers") }) Put(Path.Combine(version, file.Item1), file.Item2);
        object empty = Build(version, version); Check("R034", !(bool)Native.Get(empty, "HasBaseline")! && Analysis(empty).Contains("尚无正常运行记录", StringComparison.Ordinal));
        string healthy = Capture(version, version, "healthy", description: "fixture baseline"); History("MarkHealthy", version, healthy, true);
        object unchanged = Build(version, version); Check("R038", (bool)Native.Get(unchanged, "HasBaseline")! && (string)Native.Get(unchanged, "BaselineId")! == healthy && Changes(unchanged).Count == 0);
        string unhealthy = Capture(version, version, "newer unhealthy", description: "not healthy");
        File.Move(Path.Combine(version, "mods/disabled.jar"), Path.Combine(version, "mods/disabled.jar.disabled")); File.Move(Path.Combine(version, "mods/re-enabled.jar.old"), Path.Combine(version, "mods/re-enabled.jar")); File.Delete(Path.Combine(version, "mods/update-1.0.jar"));
        foreach (var file in new[] { ("mods/update-2.0.jar", "new-update"), ("mods/content.jar", "new-content"), ("mods/added.jar.disabled", "new-disabled"), ("config/private.toml", "api_key=fixture-private-after"), ("config/new.toml", "new-config"), ("example.json", "{\"id\":\"example\",\"inheritsFrom\":\"1.21.1\"}"), ("saves/world/level.dat", "world-changed"), ("screenshots/fixture.png", "shot-changed"), ("config/xaero/waypoints.txt", "markers-changed") }) Put(Path.Combine(version, file.Item1), file.Item2);
        File.Delete(Path.Combine(version, "config/deleted.toml")); object report = Build(version, version);
        Check("R054", (string)Native.Get(report, "BaselineId")! == healthy && (string)Native.Get(report, "BaselineId")! != unhealthy && Native.Get(report, "BaselineCreatedUtc") is DateTime);
        foreach (var change in new[] { ("R055", "g/mods/disabled.jar.disabled", "禁用"), ("R056", "g/mods/re-enabled.jar", "^启用"), ("R057", "g/mods/update-2.0.jar", "文件版本变化"), ("R058", "g/mods/content.jar", "更新内容"), ("R059", "g/mods/added.jar.disabled", "新增"), ("R060", "g/config/private.toml", "修改"), ("R061", "g/config/deleted.toml", "删除"), ("R062", "g/config/new.toml", "新增"), ("R063", "v/example.json", "修改") }) Check(change.Item1, Changes(report).Count(row => row.Relative == change.Item2 && Regex.IsMatch(row.Kind ?? "", change.Item3)) == 1);
        Native.Check(report.GetType().Assembly == Native.Main && report.GetType().FullName == "PCL.RecentEnvironmentChangesReport" && Changes(report).All(row => row.GetType().Assembly == Native.Main && row.GetType().FullName == "PCL.RecentEnvironmentChange"), "Recent-change report replaced Main DTO identities or copied models.");
        string text = Analysis(report);
        Check("R065", !Regex.IsMatch(text, "fixture-private|api_key|original-world|world-changed|shot-changed|waypoints|markers-changed") && !text.Contains(scope.Root, StringComparison.Ordinal));
        Check("R066", text.Contains("不能据此确定崩溃原因", StringComparison.Ordinal) && text.Contains("1.0", StringComparison.Ordinal) && text.Contains("2.0", StringComparison.Ordinal));
        string appended = (string)Native.Static("PCL.RecentEnvironmentChanges", "AppendAnalysis", "fixture crash reason", report)!;
        Check("R068", appended.StartsWith("fixture crash reason", StringComparison.Ordinal) && appended.Contains("上次正常运行后改了什么", StringComparison.Ordinal));
        string other = Path.Combine(scope.Root, "different-game-root"); Directory.CreateDirectory(other); object mismatch = Build(version, other);
        Check("R071", !(bool)Native.Get(mismatch, "HasBaseline")! && Analysis(mismatch).Contains("不同的游戏目录", StringComparison.Ordinal));
        using (CancellationTokenSource canceled = new()) { canceled.Cancel(); Native.Check(Reject("R073", () => Build(version, version, canceled.Token)) is OperationCanceledException, "Recent comparison cancellation changed exception kind."); }
        string outside = Path.Combine(scope.Root, "recent-outside"); Put(Path.Combine(outside, "keep.txt"), "keep-outside");
        WithJunction(Path.Combine(version, "config/linked"), outside, () => Reject("R076", () => Build(version, version)));
        Check("R078", File.ReadAllText(Path.Combine(outside, "keep.txt")) == "keep-outside");
        Console.WriteLine("PASS environment-history recent: healthy-only baselines, exact change kinds, file-version/enable/disable classification, relative/private-free report, append API, cancellation and link rejection.");
    }
}
