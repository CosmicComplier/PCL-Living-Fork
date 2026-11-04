using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PCLLF.Native.Facade.Tests;
// The original workflow range is source/CLI acceptance. Keep it runnable
// independently so no one attributes these static routing results to Main UI.
internal sealed class ReviewFixesWorkflowChecks(Action<int, bool, string?> assert)
{
    private void C(int line, bool value, string? detail = null) => assert(line, value, detail);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Full(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    internal void Run()
    {
        string? input = Environment.GetEnvironmentVariable("PCL_NATIVE_REPOSITORY_ROOT") ?? Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        Require(!string.IsNullOrWhiteSpace(input), "Review-All Workflow needs the actual source checkout via GITHUB_WORKSPACE or explicit PCL_NATIVE_REPOSITORY_ROOT; never guess the copied output directory.");
        string root = Full(input!);
        Require(Full(Command("git", root, "-c", "core.quotepath=false", "rev-parse", "--show-toplevel").Trim()) == root, "Review Workflow root is not its actual Git checkout.");
        string? revision = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (!string.IsNullOrEmpty(revision)) Require(Command("git", root, "rev-parse", "HEAD").Trim() == revision, "Review workflow source checkout does not match this actual CI SHA.");
        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "build.yml"));
        string result = Command("git", root, "-c", "core.quotepath=false", "ls-files", "--", "PCLCS/**", "Plain Craft Launcher 2/**", "MeloongCore", ".gitmodules", "*.sln");
        string[] tracked = result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries); C(263, tracked.Length > 0);
        string[] buildInputs = tracked.Where(path => !Regex.IsMatch(path, @"\.md$", RegexOptions.IgnoreCase)).Concat(["PCLCS/Future/Nested/Input.cs", "Future.sln", "MeloongCore", ".gitmodules"]).ToArray();
        foreach (string eventName in new[] { "push", "pull_request" })
        {
            string[] patterns = CiPatterns(workflow, eventName);
            foreach (string path in buildInputs) C(267, CiPath(patterns, path), eventName + " " + path);
        }
        string python = OperatingSystem.IsWindows() ? "python" : "python3";
        JsonElement Selection(params string[] arguments)
        {
            string text = Command(python, root, [Path.Combine(root, "scripts", "Select-MigrationChecks.py"), .. arguments]);
            C(273, !string.IsNullOrWhiteSpace(text));
            using JsonDocument document = JsonDocument.Parse(text); return document.RootElement.Clone();
        }
        static bool Flag(JsonElement data, string name) => data.GetProperty(name).GetBoolean();
        static string[] Names(JsonElement data, string name) => data.GetProperty(name).EnumerateArray().Select(value => value.GetString()!).ToArray();
        JsonElement metadata = Selection("--path", "PCLCS/Minecraft/InstanceResourceDetailsRules.cs");
        string[] groups = Names(metadata, "groups"), consumers = Names(metadata, "windows_scripts");
        C(277, !Flag(metadata, "full") && Flag(metadata, "product_build") && Flag(metadata, "oracle") && groups.Length == 1 && groups[0] == "resource-metadata");
        C(278, consumers.Contains("Test-InstanceResources", StringComparer.Ordinal) && consumers.Contains("Test-MapDetails", StringComparer.Ordinal) && !consumers.Contains("Test-ModDependencies", StringComparer.Ordinal));
        foreach (JsonElement selection in new[] { Selection("--path", "PCLCS/Future/Nested/Input.cs"), Selection("--full") })
        {
            groups = Names(selection, "groups"); consumers = Names(selection, "windows_scripts");
            C(280, Flag(selection, "full") && Flag(selection, "product_build") && Flag(selection, "oracle") && Flag(selection, "inventory") && groups.Length != 0);
            foreach (string required in new[] { "world-metadata", "local-data", "personal-options", "keybindings", "quick-play", "resource-entry", "resource-paths", "resource-files", "resource-metadata" }) C(281, groups.Contains(required, StringComparer.Ordinal), required);
            foreach (string required in new[] { "Test-Release", "Test-ReviewFixes", "Test-ModDependencies", "Test-IconCompatibility" }) C(282, consumers.Contains(required, StringComparer.Ordinal), required);
        }
        JsonElement documentation = Selection("--path", "docs/MIGRATION_GOAL.md");
        C(285, !Flag(documentation, "full") && !Flag(documentation, "product_build") && !Flag(documentation, "migration_run") && Names(documentation, "groups").Length == 0 && Names(documentation, "windows_scripts").Length == 0);
        Console.WriteLine("PASS review Workflow SOURCE/CLI ONLY: actual same-checkout push/PR patterns over all original tracked/future inputs, real metadata/full/unknown/docs selector results. This section is not counted as Main/Windows runtime equivalence.");
    }
    private string[] CiPatterns(string text, string eventName)
    {
        Match section = Regex.Match(text, @"(?ms)^  " + Regex.Escape(eventName) + @":\r?\n(?<body>.*?)(?=^  [A-Za-z_][A-Za-z0-9_]*:|\z)");
        C(236, section.Success, eventName);
        Match filter = Regex.Match(section.Groups["body"].Value, @"(?m)^    (?<kind>paths|paths-ignore):\s*\r?\n(?<rules>(?:^      - [^\r\n]*\r?\n)+)");
        if (!filter.Success) return ["**"];
        List<string> patterns = new(); bool ignore = filter.Groups["kind"].Value == "paths-ignore";
        foreach (Match line in Regex.Matches(filter.Groups["rules"].Value, @"(?m)^      - (?<rule>[^\r\n]+)"))
        {
            string pattern = line.Groups["rule"].Value.Trim();
            if ((pattern.StartsWith('\'') && pattern.EndsWith('\'')) || (pattern.StartsWith('"') && pattern.EndsWith('"'))) pattern = pattern[1..^1];
            C(243, !string.IsNullOrEmpty(pattern)); patterns.Add(ignore ? "!" + pattern : pattern);
        }
        if (ignore) patterns.Insert(0, "**"); return patterns.ToArray();
    }
    private bool CiPath(string[] patterns, string path)
    {
        bool included = false;
        foreach (string pattern in patterns)
        {
            bool exclude = pattern.StartsWith('!'); string glob = exclude ? pattern[1..] : pattern;
            C(256, glob.IndexOfAny(['[', ']', '+']) < 0, glob);
            string expression = Regex.Escape(glob).Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal).Replace(@"\*\*", ".*", StringComparison.Ordinal).Replace(@"\*", "[^/]*", StringComparison.Ordinal).Replace(@"\?", "[^/]", StringComparison.Ordinal);
            if (Regex.IsMatch(path, @"\A" + expression + @"\z", RegexOptions.CultureInvariant)) included = !exclude;
        }
        return included;
    }
    private static string Command(string executable, string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo info = new(executable) { WorkingDirectory = workingDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);
        // Capture termination authority before launching any owned CLI child.
        // The standalone source-only probe has no product/global context and
        // must never impersonate a Native child or request process-level exit.
        bool verifiedNativeChild = IsVerifiedNativeChild();
        using Process process = Process.Start(info) ?? throw new InvalidOperationException("Review source/CLI child did not start.");
        Stopwatch deadline = Stopwatch.StartNew();
        int Remaining() => (int)Math.Clamp(10000L - deadline.ElapsedMilliseconds, 0L, int.MaxValue);
        Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
        Task[] drains = [output, error];
        foreach (Task drain in drains) _ = drain.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        Exception? failure = null;
        try
        {
            // One absolute deadline covers process exit AND both inherited pipes.
            // A successful parent exit does not imply descendants closed stdout.
            if (!process.WaitForExit(Remaining()) || !Task.WaitAll(drains, Remaining()))
                throw new TimeoutException("Owned Review source/CLI process/output exceeded the same 10-second deadline.");
            string stdout = output.GetAwaiter().GetResult(), stderr = error.GetAwaiter().GetResult();
            Require(process.ExitCode == 0, "Review source/CLI command failed: " + executable + " / " + stderr);
            return stdout;
        }
        catch (Exception caught) { failure = caught; throw; }
        finally
        {
            // Only the Process started above may be terminated. Share a second
            // absolute five-second cleanup budget across kill/wait/pipe settlement.
            Stopwatch cleanup = Stopwatch.StartNew();
            int CleanupRemaining() => (int)Math.Clamp(5000L - cleanup.ElapsedMilliseconds, 0L, int.MaxValue);
            List<Exception> cleanupErrors = new();
            bool stopped = false, settled = false;
            try
            {
                if (!process.HasExited) process.Kill(true);
                stopped = process.WaitForExit(CleanupRemaining());
            }
            catch (Exception caught) { cleanupErrors.Add(caught); }
            finally
            {
                // Close BOTH owned read streams even if termination failed.
                // Closing settles inherited-pipe reads without waiting for an
                // unrelated descendant to release its inherited write handle.
                try { process.StandardOutput.Dispose(); } catch (Exception caught) { cleanupErrors.Add(caught); }
                try { process.StandardError.Dispose(); } catch (Exception caught) { cleanupErrors.Add(caught); }
                try { settled = Task.WaitAll(drains, CleanupRemaining()); }
                catch (Exception caught) { settled = drains.All(drain => drain.IsCompleted); if (caught is not AggregateException) cleanupErrors.Add(caught); }
                finally { foreach (Task drain in drains) if (drain.IsFaulted) _ = drain.Exception; }
            }
            if (!stopped || !settled)
            {
                string reason = "Owned Review source/CLI child or output drains did not stop; retaining context until Native isolated child exit.";
                if (verifiedNativeChild)
                {
                    Console.Error.WriteLine("FAIL isolated native Review cleanup: " + reason);
                    Console.Error.Flush(); Environment.Exit(95);
                    throw new InvalidOperationException("Verified Native child termination unexpectedly returned.");
                }
                throw new InvalidOperationException("Unverified source-only probe cannot request Native process-level exit. " + reason,
                    cleanupErrors.Count == 0 ? failure : new AggregateException(cleanupErrors));
            }
            if (cleanupErrors.Count > 0)
            {
                if (failure is null) throw new AggregateException("Owned Review CLI cleanup failed.", cleanupErrors);
                Console.Error.WriteLine("Review CLI cleanup faults observed after its original failure: " + string.Join(",", cleanupErrors.Select(caught => caught.GetType().Name)));
            }
        }
    }
    private static bool IsVerifiedNativeChild()
    {
        try
        {
            var entry = System.Reflection.Assembly.GetEntryAssembly();
            if (entry is null || entry != System.Reflection.Assembly.GetExecutingAssembly() || entry.GetName().Name != "PCLLF.Native.Facade.Tests") return false;
            string[] arguments = Environment.GetCommandLineArgs(); int option = Array.IndexOf(arguments, "--fixture");
            if (!arguments.Contains("--isolated", StringComparer.Ordinal) || option < 0 || option + 1 >= arguments.Length) return false;
            string root = Full(arguments[option + 1]);
            return root.StartsWith(Full(Path.GetTempPath()) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                Regex.IsMatch(Path.GetFileName(root), @"\APclNativeFacade-[a-f0-9]{32}\z") &&
                Full(AppContext.BaseDirectory) == Full(Path.Combine(root, "output")) && Directory.Exists(root) &&
                (File.GetAttributes(root) & FileAttributes.ReparsePoint) == 0;
        }
        catch { return false; }
    }
}
