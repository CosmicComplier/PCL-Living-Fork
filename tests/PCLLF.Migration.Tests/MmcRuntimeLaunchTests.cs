using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcRuntimeLaunchTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC exact bridge has deterministic owned support bytes and reviewed source", BridgeIdentity);
        yield return ("MMC production runtime preserves actual Java8 and Java17 argv through original Wrapper and exact bridge", ActualArgv);
        yield return ("MMC runtime macros distinguish Minecraft version instance name session and owned paths", MacrosAndPrivacy);
        yield return ("MMC runtime refuses unbound execution variables account overrides Java conflicts and changed inputs", Refusals);
        yield return ("MMC classpath inspector preserves shadow order and resolves Java multi-release entries", ClasspathAndMultiRelease);
    }
    private static void BridgeIdentity() {
        byte[] bytes = MmcRuntimeBridgeTransport.CreateJarBytes();
        string digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        if (digest != MmcRuntimeBridgeTransport.JarSha256() || !MmcRuntimeBridgeTransport.JavaSource.Contains("getSystemClassLoader", StringComparison.Ordinal)) throw new Exception("Bridge identity/source missing.");
    }
    private static readonly string Java17 = Environment.GetEnvironmentVariable("MMC_TEST_JAVA17") ?? "C:/Program Files/Java/jdk-17.0.21.0.3/bin/java.exe";
    private static readonly string Java8 = Environment.GetEnvironmentVariable("MMC_TEST_JAVA8") ?? "C:/Program Files/Java/jre1.8.0_491/bin/java.exe";
    private static readonly Lazy<Jars> Compiled = new(BuildJars);
    private sealed record Jars(byte[] First, byte[] Second, byte[] Main, byte[] MultiRelease, byte[] Wrapper);
    private const string ProbeSource = """
package fixture;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
public final class Probe {
  public static void main(String[] args) {
    for (String arg : args) System.out.println("MARG:" + Base64.getEncoder().encodeToString(arg.getBytes(StandardCharsets.UTF_8)));
    System.out.println("MMODE:__MODE__");
    for (String key : new String[]{"fixture.value","java.class.path","user.dir","java.io.tmpdir","oolloo.jlw.tmpdir","org.lwjgl.system.SharedLibraryExtractPath"})
      System.out.println("MPROP:" + key + ":" + Base64.getEncoder().encodeToString(System.getProperty(key, "").getBytes(StandardCharsets.UTF_8)));
    System.out.println("MLOADER:" + ClassLoader.getSystemClassLoader().getClass().getName());
  }
}
""";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static Jars BuildJars() {
        string root = Path.Combine(Path.GetTempPath(), "PCLLF-MmcRuntimeTests", "compile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        string javac = Path.Combine(Path.GetDirectoryName(Java17)!, "javac.exe");
        byte[] CompileProbe(string mode, int release) {
            string folder = Path.Combine(root, mode); Directory.CreateDirectory(Path.Combine(folder, "fixture"));
            string source = Path.Combine(folder, "fixture", "Probe.java"); File.WriteAllText(source, ProbeSource.Replace("__MODE__", mode, StringComparison.Ordinal), new UTF8Encoding(false));
            RunTool(javac, ["--release", release.ToString(), "-encoding", "UTF-8", "-g:none", source]);
            return File.ReadAllBytes(Path.Combine(folder, "fixture", "Probe.class"));
        }
        byte[] first = CompileProbe("first", 8), second = CompileProbe("second", 8), main = CompileProbe("main", 8), mr = CompileProbe("mr17", 17);
        string loaderSource = Path.Combine(root, "TestLoader.java"); File.WriteAllText(loaderSource, "package fixture; public final class TestLoader extends ClassLoader { public TestLoader(ClassLoader parent) { super(parent); } }", new UTF8Encoding(false));
        string tweakSource = Path.Combine(root, "Tweak.java"); File.WriteAllText(tweakSource, "package fixture; public final class Tweak {}", new UTF8Encoding(false));
        RunTool(javac, ["--release", "8", "-g:none", "-d", root, loaderSource, tweakSource]);
        byte[] loader = File.ReadAllBytes(Path.Combine(root, "fixture", "TestLoader.class")), tweak = File.ReadAllBytes(Path.Combine(root, "fixture", "Tweak.class"));
        string repo = Directory.GetCurrentDirectory(); while (!File.Exists(Path.Combine(repo, "PCLCS", "Launch", "JavaWrapper.jar"))) repo = Path.GetDirectoryName(repo) ?? throw new Exception("Cannot locate known project Wrapper input.");
        byte[] wrapper = File.ReadAllBytes(Path.Combine(repo, "PCLCS", "Launch", "JavaWrapper.jar"));
        return new(Jar(new() { ["fixture/Probe.class"] = first, ["fixture/TestLoader.class"] = loader, ["fixture/Tweak.class"] = tweak }),
            Jar(new() { ["fixture/Probe.class"] = second }), Jar(new() { ["fixture/Probe.class"] = main }),
            Jar(new() { ["fixture/Probe.class"] = first, ["META-INF/versions/17/fixture/Probe.class"] = mr }, "Manifest-Version: 1.0\r\nMulti-Release: true\r\n\r\n"), wrapper);
    }
    private static byte[] Jar(Dictionary<string, byte[]> files, string? manifest = null) {
        using MemoryStream output = new();
        using (ZipArchive jar = new(output, ZipArchiveMode.Create, true)) {
            if (manifest is not null) files = new(files) { ["META-INF/MANIFEST.MF"] = Encoding.UTF8.GetBytes(manifest) };
            foreach (var item in files) { var entry = jar.CreateEntry(item.Key); entry.LastWriteTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero); using var target = entry.Open(); target.Write(item.Value); }
        }
        return output.ToArray();
    }
    private static void RunTool(string executable, string[] arguments) {
        ProcessStartInfo start = new(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in arguments) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new Exception("Self-authored Java fixture compiler did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new Exception("Fixture compiler timeout."); }
        Task.WaitAll(output, errors); Require(process.ExitCode == 0, "Self-authored Java fixture compilation failed.");
    }
    private sealed class Fixture : IDisposable {
        internal string Root { get; }
        internal MmcRuntimeLaunchRequest Request { get; }
        internal Fixture(int javaMajor = 17, bool multiRelease = false, bool loader = true, string? unknownArgument = null, MmcRuntimeWrapperMode wrapperMode = MmcRuntimeWrapperMode.JavaWrapper) {
            Root = Path.Combine(Path.GetTempPath(), "PCLLF-MmcRuntimeTests", "run-" + Guid.NewGuid().ToString("N"));
            string instance = Path.Combine(Root, "versions", "实例 space"); Directory.CreateDirectory(Path.Combine(instance, "minecraft"));
            var paths = MmcInstancePaths.Resolve(instance, "minecraft"); string view = new('f', 64);
            byte[] first = multiRelease ? Compiled.Value.MultiRelease : Compiled.Value.First;
            var jarBytes = new Dictionary<string, byte[]> { ["first"] = first, ["second"] = Compiled.Value.Second, ["main"] = Compiled.Value.Main };
            object Library(string name, byte[] bytes) => new { name, downloads = new { artifact = new { url = "https://example.invalid/" + name.Replace(':', '-'), sha1 = Convert.ToHexStringLower(SHA1.HashData(bytes)), sha256 = Hash(bytes), size = bytes.Length } } };
            string game = "--username ${auth_player_name} --uuid ${auth_uuid} --accessToken ${auth_access_token} --session ${auth_session} --userType ${user_type} --userProperties ${user_properties} --profile ${profile_name} --mc ${version_name} --type ${version_type} --gameDir ${game_directory} --assetsDir ${assets_root} --assetIndex ${assets_index_name} --libraries ${library_directory} --primary ${primary_jar} --width ${resolution_width} --height ${resolution_height} --value '' --trail 'tail\\' --中文 '中文 path'";
            if (unknownArgument is not null) game += " " + unknownArgument;
            string patch = JsonSerializer.Serialize(new Dictionary<string, object?> {
                ["uid"] = "net.minecraft", ["version"] = "1.7.10", ["type"] = "release", ["mainClass"] = "fixture.Probe", ["mainJar"] = Library("fixture:main:1:client", Compiled.Value.Main),
                ["libraries"] = new[] { Library("fixture:first:1", first), Library("fixture:second:1", Compiled.Value.Second) },
                ["minecraftArguments"] = game, ["compatibleJavaMajors"] = new[] { 8, 17, 21, 24, 26 },
                ["+jvmArgs"] = loader ? new[] { "-Djava.system.class.loader=fixture.TestLoader" } : [], ["+tweakers"] = loader ? new[] { "fixture.Tweak" } : []
            });
            string manifest = "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.7.10\"}]}";
            Dictionary<string, string> metadata = new() { ["mmc-pack.json"] = manifest, ["patches/net.minecraft.json"] = patch };
            var parsed = MmcPackageReader.Parse(new(metadata.Select(m => new MmcArchiveEntry(m.Key)).ToArray(), metadata));
            var resolved = MmcComponentResolver.Resolve(parsed, []); Require(resolved.IsComplete, "Runtime fixture source resolution failed.");
            var compiled = MmcProfileCompiler.Compile(resolved.Profile!, new("windows", "amd64", javaMajor)); Require(!compiled.HasErrors, "Runtime fixture compile failed.");
            var plan = compiled.Profile!;
            List<MmcMaterializedArtifactFact> facts = []; List<MmcRuntimeArtifactTarget> targets = [];
            foreach (var artifact in plan.Classpath) {
                byte[] bytes = jarBytes[artifact.Coordinate!.Artifact]; string relative = "PCL/MmcRuntime/" + view + "/libraries/" + artifact.RelativePath;
                string path = paths.ResolveRelative(MmcPathRole.Instance, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes);
                string recovery = "PCL/MmcArtifacts/sha256/" + Hash(bytes); string original = paths.ResolveRelative(MmcPathRole.Instance, recovery); Directory.CreateDirectory(Path.GetDirectoryName(original)!); File.WriteAllBytes(original, bytes);
                facts.Add(new(artifact.Key, artifact.SourceRevision, Hash(bytes), bytes.Length, recovery, relative, Convert.ToHexStringLower(SHA1.HashData(bytes))));
                targets.Add(new(artifact.Key, relative, path));
            }
            string bundle = new('b', 64), prefix = "PCL/MmcSource/" + bundle + "/";
            var definition = new MmcInstanceDefinition(1, "Mmc", "prism-v1", Guid.NewGuid(), 1, new(new string('a', 64), "fixture.zip"),
                new("", bundle, plan.SourceFingerprint, [new("mmc-pack.json", Hash(Encoding.UTF8.GetBytes(manifest)), prefix + "mmc-pack.json", MmcSourceRecordKind.Manifest),
                    new("patches/net.minecraft.json", Hash(Encoding.UTF8.GetBytes(patch)), prefix + "patches/net.minecraft.json", MmcSourceRecordKind.Component, "net.minecraft", "1.7.10", MmcComponentSourceKind.PackageLocal)]), new("minecraft"), [], [], []);
            var bound = MmcPlanBinder.Bind(definition, plan, facts, new(1, 1, MmcBindingIntent.InitialImport, Hash(Encoding.UTF8.GetBytes("{}")), 1)); Require(!bound.HasErrors, "Runtime fixture byte binding failed.");
            foreach (var item in metadata) { string path = paths.ResolveRelative(MmcPathRole.Instance, prefix + item.Key); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, item.Value, new UTF8Encoding(false)); }
            File.WriteAllText(paths.ResolveRelative(MmcPathRole.Instance, "PCL/InstanceDefinition.json"), MmcDefinitionCodec.Serialize(bound.Definition!), new UTF8Encoding(false));
            string supportRoot = paths.ResolveRelative(MmcPathRole.Runtime, view + "/support"); Directory.CreateDirectory(supportRoot);
            string wrapper = Path.Combine(supportRoot, "JavaWrapper.jar"), bridge = Path.Combine(supportRoot, "MmcRuntimeBridge.jar");
            File.WriteAllBytes(wrapper, Compiled.Value.Wrapper); byte[] bridgeBytes = MmcRuntimeBridgeTransport.CreateJarBytes(); File.WriteAllBytes(bridge, bridgeBytes);
            string java = javaMajor == 8 ? Java8 : Java17; string javaHash = Hash(File.ReadAllBytes(java));
            var settings = new MmcRuntimeSettingsSnapshot("Instance Display", "PCLLF", "test", 16, 64, 640, 360, Guid.NewGuid()) {
                WrapperMode = wrapperMode, UserJvmTokens = ["-Dfixture.value=regex\\\\d+ trailing\\"], ExtraGameTokens = ["", "中文 supplementary 😀", "double\\\\slashes", "trailing\\", "quoted\"value"], AppendResolutionArguments = false
            };
            Request = new(bound.Definition!, bound.Plan!, paths, targets.ToImmutableArray(), new(java, javaHash, javaHash, javaMajor, "amd64", 64), settings,
                new("PlayerOne", new string('1', 32), "synthetic-secret-token-one", "token:synthetic-secret-token-one:uuid", "legacy", "{\"synthetic\":\"property\"}"),
                new(view, paths.ResolveRelative(MmcPathRole.Runtime, view + "/natives"), paths.ResolveRelative(MmcPathRole.Runtime, view + "/assets"), paths.ResolveRelative(MmcPathRole.Runtime, view + "/assets/virtual/legacy")),
                new(wrapper, Hash(Compiled.Value.Wrapper), Compiled.Value.Wrapper.Length), new(bridge, Hash(bridgeBytes), bridgeBytes.Length));
        }
        internal void RemoveMutable(MmcRuntimeLaunchPlan plan) {
            string target = Path.GetFullPath(plan.MutableDirectories.SessionRoot);
            Require(target.StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Test cleanup escaped its owned fixture.");
            Directory.Delete(target, true);
        }
        public void Dispose() {
            string root = Path.GetFullPath(Root), parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "PCLLF-MmcRuntimeTests"));
            Require(root.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && Guid.TryParseExact(Path.GetFileName(root)["run-".Length..], "N", out _), "Fixture cleanup target is not owned.");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
    private sealed record Output(ImmutableArray<string> Arguments, Dictionary<string, string> Properties, string Mode, string Loader);
    private static Output Run(MmcRuntimeLaunchPlan plan) {
        using var process = Process.Start(plan.CreateStartInfo()) ?? throw new Exception("Controlled Java runtime did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new Exception("Controlled Java runtime timeout."); }
        Task.WaitAll(stdout, stderr);
        Require(process.ExitCode == 0, "Controlled Java runtime failed: " + plan.Redact(stderr.Result));
        var arguments = ImmutableArray.CreateBuilder<string>(); Dictionary<string, string> properties = new(); string mode = "", loader = "";
        foreach (string line in stdout.Result.Replace("\r\n", "\n").Split('\n')) {
            if (line.StartsWith("MARG:", StringComparison.Ordinal)) arguments.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line[5..])));
            if (line.StartsWith("MPROP:", StringComparison.Ordinal)) { int split = line.IndexOf(':', 6); properties[line[6..split]] = Encoding.UTF8.GetString(Convert.FromBase64String(line[(split + 1)..])); }
            if (line.StartsWith("MMODE:", StringComparison.Ordinal)) mode = line[6..];
            if (line.StartsWith("MLOADER:", StringComparison.Ordinal)) loader = line[8..];
        }
        return new(arguments.ToImmutable(), properties, mode, loader);
    }
    private static void ActualArgv() {
        foreach (int major in new[] { 8, 17 }) {
            using Fixture fixture = new(major);
            var plan = MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request).GetAwaiter().GetResult();
            var observed = Run(plan);
            Require(observed.Mode == "first" && observed.Loader == "fixture.TestLoader", "Actual VM CP order or startup system loader differs from the bound plan.");
            Require(observed.Arguments.TakeLast(5).SequenceEqual(fixture.Request.Settings.ExtraGameTokens), "Actual Wrapper/bridge argv lost empty, Unicode, slash or quote semantics.");
            Require(observed.Properties["fixture.value"] == "regex\\\\d+ trailing\\", "Wrapper reparsing corrupted JVM property values.");
            Require(observed.Properties["user.dir"].Equals(fixture.Request.Paths.GameRoot, StringComparison.OrdinalIgnoreCase), "Actual process working directory is not GameRoot.");
            foreach (string property in new[] { "java.io.tmpdir", "oolloo.jlw.tmpdir", "org.lwjgl.system.SharedLibraryExtractPath" }) Require(observed.Properties[property].StartsWith(plan.MutableDirectories.SessionRoot.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase), "Native/Wrapper extraction escaped the per-session mutable root.");
            var separator = observed.Properties["java.class.path"].Split(';');
            Require(separator.TakeLast(plan.OrderedGameClasspath.Length).SequenceEqual(plan.OrderedGameClasspath.Select(p => p.Replace('\\', '/'))), "Game classpath order/mainJar tail changed behind Wrapper prefix.");
            MmcRuntimeLaunchBuilder.RevalidateFilesAsync(plan).GetAwaiter().GetResult();
        }
    }
    private static string Flag(Output output, string name) { int i = output.Arguments.IndexOf(name); Require(i >= 0 && i + 1 < output.Arguments.Length, "Expected controlled game flag missing."); return output.Arguments[i + 1]; }
    private static void MacrosAndPrivacy() {
        using Fixture fixture = new(); var first = MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request).GetAwaiter().GetResult(); var observed = Run(first);
        Require(Flag(observed, "--profile") == "Instance Display" && Flag(observed, "--mc") == "1.7.10" && Flag(observed, "--type") == "release" && Flag(observed, "--session") == fixture.Request.Account.Session, "MMC version/profile/auth_session semantics were flattened.");
        Require(Flag(observed, "--libraries").Contains("/PCL/MmcRuntime/", StringComparison.Ordinal) && Flag(observed, "--gameDir") == fixture.Request.Paths.GameRoot.Replace('\\', '/'), "Runtime macro used a global library/game root.");
        Require(!string.Join("|", first.RedactedArguments).Contains(fixture.Request.Account.AccessToken, StringComparison.Ordinal) && first.RedactedArguments[^1] == "[sensitive argv frame]", "Sensitive frame/token leaked to log-safe arguments.");
        string frame = first.CreateStartInfo().ArgumentList[^1]; Require(first.Redact("frame=" + frame) == "frame=[redacted]", "Sensitive opaque transport frame was not redacted.");
        fixture.RemoveMutable(first);
        var second = MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request with { Account = new("PlayerTwo", new string('2', 32), "synthetic-secret-token-two", "different-session-token", "msa", "{\"different\":\"properties\"}") }).GetAwaiter().GetResult();
        Require(first.NonSensitiveArgumentFingerprint == second.NonSensitiveArgumentFingerprint && first.RuntimeInputFingerprint == second.RuntimeInputFingerprint, "Account values influenced a persisted non-sensitive runtime fingerprint.");
        Require(!first.CreateStartInfo().Environment.ContainsKey("JDK_JAVA_OPTIONS") && !first.CreateStartInfo().Environment.ContainsKey("JAVA_TOOL_OPTIONS"), "Unobserved environment JVM options were inherited.");
    }
    private static void Refusals() {
        using (Fixture unknown = new(unknownArgument: "${unknown_critical}")) Expect("MMC_RUNTIME_VARIABLE", () => MmcRuntimeLaunchBuilder.BuildAsync(unknown.Request).GetAwaiter().GetResult());
        using (Fixture fixture = new()) {
            Expect("MMC_RUNTIME_UNBOUND_INPUT", () => MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request with { Settings = fixture.Request.Settings with { UserJvmTokens = ["@unbound.args"] } }).GetAwaiter().GetResult());
            Expect("MMC_RUNTIME_UNBOUND_INPUT", () => MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request with { Settings = fixture.Request.Settings with { UserJvmTokens = ["-javaagent:unbound.jar"] } }).GetAwaiter().GetResult());
            Expect("MMC_RUNTIME_JAVA", () => MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request with { Account = new("Player", "uuid", "token", "session", "legacy", "{}", javaConstraints: [new("account", MaximumMajor: 8)]) }).GetAwaiter().GetResult());
            string jar = fixture.Request.Targets[0].ActualPath; File.AppendAllText(jar, "tamper");
            Expect("MMC_RUNTIME_INPUT_CHANGED", () => MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request).GetAwaiter().GetResult());
        }
    }
    private static void ClasspathAndMultiRelease() {
        using (Fixture fixture = new()) {
            var plan = MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request).GetAwaiter().GetResult();
            var inspection = MmcClasspathInspector.ObserveAsync(plan).GetAwaiter().GetResult();
            var selected = inspection.EffectiveClasses.Single(c => c.ClassName == "fixture.Probe");
            Require(selected.ClasspathPosition == 0 && inspection.CoreObservations.Count(j => j.Classes.Any(c => c.ClassName == "fixture.Probe" && c.Present)) == 3, "Classpath shadow order/presence was flattened.");
        }
        foreach (int major in new[] { 8, 17 }) {
            using Fixture fixture = new(major, multiRelease: true, loader: false);
            var plan = MmcRuntimeLaunchBuilder.BuildAsync(fixture.Request).GetAwaiter().GetResult();
            var inspection = MmcClasspathInspector.ObserveAsync(plan).GetAwaiter().GetResult();
            var selected = inspection.EffectiveClasses.Single(c => c.ClassName == "fixture.Probe");
            var output = Run(plan);
            Require(selected.Release == (major == 8 ? 0 : 17) && output.Mode == (major == 8 ? "first" : "mr17"), "Manifest MR flag/actual Java release did not match observed and executed class selection.");
        }
    }
    private static void Expect(string code, Action operation) { try { operation(); } catch (MmcRuntimeLaunchException error) when (error.Code == code) { return; } throw new Exception("Expected runtime refusal " + code); }
}
