using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace PCLLF.Native.Facade.Tests;
internal static class Program
{
    private static readonly Dictionary<string, Action<FixtureEnvironment>> Cases = new(StringComparer.Ordinal)
    {
        ["map-details"] = MapResourceChecks.MapDetails,
        ["resource-entry"] = MapResourceChecks.ResourceEntries,
        ["world-details"] = WorldChecks.Run,
        ["registry-names"] = RegistryChecks.Run,
        ["registry-data"] = RegistryDataChecks.Run,
        ["theme"] = environment => ThemeScenarios.Run(Application.Current, Path.Combine(AppContext.BaseDirectory, "assets")),
        ["file-tasks"] = FileTaskScenarios.Run,
        ["mod-dependencies"] = NativeDependencyChecks.Local,
        ["mod-dependency-online"] = NativeDependencyChecks.Online,
        ["updater"] = NativeUpdateChecks.Run,
        ["backup-resources"] = BackupResourceChecks.Run,
        ["instance-clone"] = CloneNativeChecks.Run,
        ["modpack-update"] = ModpackUpdateChecks.Run,
        ["environment-history"] = EnvironmentHistoryChecks.Run,
        ["local-features"] = LocalFeatureChecks.Run,
        ["launch-dependencies"] = LaunchDependencyChecks.Run
    };
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("This host requires a real Windows .NET10/WPF runtime.");
            if (BackupResourceChecks.TryRunIdleChild(args, out int backupIdleExit))
                return backupIdleExit;
            string[] selected = ReadGroups(args);
            bool isolated = args.Contains("--isolated", StringComparer.Ordinal);
            if (!isolated)
                return RunChild(args);
            string root = ReadOption(args, "--fixture") ?? throw new ArgumentException("Missing isolated fixture root.");
            ValidateRoot(root);
            Native.Check(Native.Full(AppContext.BaseDirectory) == Native.Full(Path.Combine(root, "output")), "The child is not running from the complete GUID output directory.");
            using FixtureEnvironment environment = new(root);
            if (args.Contains("--dependency-index-child", StringComparer.Ordinal))
                return NativeDependencyChecks.IndexChild(environment, args);
            foreach (string group in selected)
            {
                Cases[group](environment);
                environment.AssertNoForbiddenIo();
                Console.WriteLine("PASS native facade " + group);
            }

            environment.AssertNoForbiddenIo();
            Native.Check(Application.Current.Windows.Count == 0, "A selected facade group left a real window open.");
            Console.WriteLine("PASS " + selected.Length + " selected groups; genuine Main data/adapters. The original data groups do not pump; theme uses an isolated Window and file-tasks drains real task Dispatcher callbacks after LF Startup hooks are detached. EntryAssembly remains the test host.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL native facade: " + error);
            return 1;
        }
    }

    private static string? ReadOption(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        return index < 0 ? null : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException("Missing " + key + " value.");
    }

    private static string[] ReadGroups(string[] args)
    {
        string? value = ReadOption(args, "--groups");
        string[] selected = value is null ? Cases.Keys.Where(name => name != "registry-data").ToArray() : value.Split(',', StringSplitOptions.TrimEntries);
        if (selected.Length == 0 || selected.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Empty facade selection.");
        foreach (string item in selected)
            if (!Cases.ContainsKey(item))
                throw new ArgumentException("Unknown facade group: " + item);
        if (selected.Contains("registry-data", StringComparer.Ordinal) && selected.Distinct(StringComparer.Ordinal).Count() != 1)
            throw new ArgumentException("Select registry-data alone in its fresh hosted-profile process.");
        return selected.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static int RunChild(string[] args)
    {
        string root = Path.Combine(Path.GetTempPath(), "PclNativeFacade-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string output = Path.Combine(root, "output");
        Process? process = null;
        try
        {
            CopyOutput(AppContext.BaseDirectory, output);
            string executable = Path.Combine(output, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".exe");
            if (!File.Exists(executable))
                throw new FileNotFoundException("Build the actual Windows apphost before running this fixture.", executable);
            ProcessStartInfo start = new(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = root,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string argument in args)
                start.ArgumentList.Add(argument);
            start.ArgumentList.Add("--isolated");
            start.ArgumentList.Add("--fixture");
            start.ArgumentList.Add(root);
            process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the actual facade child.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill(true);
                process.WaitForExit(5000);
                throw new TimeoutException("Native facade host exceeded60 seconds.");
            }

            Console.Write(stdout.GetAwaiter().GetResult());
            Console.Error.Write(stderr.GetAwaiter().GetResult());
            return process.ExitCode;
        }
        finally
        {
            if (process is not null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                        process.WaitForExit(5000);
                    }
                }
                finally
                {
                    process.Dispose();
                }
            }

            ValidateRoot(root);
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void CopyOutput(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Unexpected linked output file.");
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }

        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Unexpected linked output directory.");
            CopyOutput(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    internal static void ValidateRoot(string root)
    {
        string absolute = Native.Full(root), prefix = Native.Full(Path.GetTempPath()) + Path.DirectorySeparatorChar;
        Native.Check(absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(absolute), "^PclNativeFacade-[a-f0-9]{32}$"), "Unsafe fixture root.");
    }
}
