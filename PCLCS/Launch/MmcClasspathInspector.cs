using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCLLF.MMCC.Core;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public sealed record MmcClasspathClassSelection(string ClassName, string ArtifactKey, string EntryPath,
    int ClasspathPosition, int Release, int ClassMajorVersion);
public sealed record MmcClasspathInspection(ImmutableArray<MmcJarClassObservation> CoreObservations,
    ImmutableArray<MmcClasspathClassSelection> EffectiveClasses);

/// <summary>Reads only locked classpath JARs and selected class headers; never loads classes or scans a mod directory.</summary>
public static class MmcClasspathInspector {
    private sealed record Found(string Path, int Release, int Major);
    private sealed record JarResult(MmcJarClassObservation Observation, Dictionary<string, Found> Found);
    private sealed record JarRuntimePolicy(int JavaMajor, int MaximumRelease, bool MultiReleaseEnabled, bool Preview);
    public static async Task<MmcClasspathInspection> ObserveAsync(MmcRuntimeLaunchPlan launch, CancellationToken cancellationToken = default,
        IReadOnlyList<string>? additionalClassNames = null) {
        ArgumentNullException.ThrowIfNull(launch);
        string[] required = launch.RequiredClassNames.Concat(additionalClassNames ?? []).Distinct(StringComparer.Ordinal).ToArray();
        ValidateNames(required);
        var profile = launch.Request.BoundPlan.CompiledProfile;
        if ((long)required.Length * profile.Classpath.Length > 65536) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_BUDGET", "Classpath observation matrix exceeds its bound.");
        var inputs = launch.Inputs.ToDictionary(i => i.Identity, StringComparer.Ordinal);
        var observations = ImmutableArray.CreateBuilder<MmcJarClassObservation>();
        Dictionary<string, MmcClasspathClassSelection> effective = new(StringComparer.Ordinal);
        var policy = RuntimePolicy(launch.Request.Java.Major, launch.EffectiveJvmArguments);
        foreach (var group in profile.Classpath.Select((artifact, index) => (artifact, index)).GroupBy(x => x.artifact.Key)) {
            var first = group.First(); var expected = inputs["artifact:" + first.artifact.Key];
            var read = await ReadJarAsync(first.artifact.Key, expected.ActualPath, expected.Sha256, expected.SizeBytes, required,
                policy, required.Where(name => !effective.ContainsKey(name)).ToHashSet(StringComparer.Ordinal), cancellationToken).ConfigureAwait(false);
            observations.Add(read.Observation);
            foreach (var entry in read.Found) if (!effective.ContainsKey(entry.Key)) effective.Add(entry.Key,
                new(entry.Key, first.artifact.Key, entry.Value.Path, first.index, entry.Value.Release, entry.Value.Major));
        }
        foreach (string name in launch.RequiredClassNames) if (!effective.ContainsKey(name)) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CLASS_MISSING", "A required launch class is absent from its locked ordered classpath.");
        return new(observations.ToImmutable(), required.Where(effective.ContainsKey).Select(name => effective[name]).ToImmutableArray());
    }

    internal static async Task VerifySupportDoesNotShadowAsync(MmcRuntimeLaunchRequest request, ImmutableArray<string> required, IReadOnlyList<string> effectiveJvmArguments, CancellationToken token) {
        ValidateNames(required);
        var policy = RuntimePolicy(request.Java.Major, effectiveJvmArguments);
        foreach (var item in new[] { ("support:wrapper", request.Wrapper!), ("support:bridge", request.Bridge!) }) {
            string ownClass = item.Item1 == "support:wrapper" ? "oolloo.jlw.Wrapper" : MmcRuntimeBridgeTransport.EntryPoint;
            var observed = await ReadJarAsync(item.Item1, item.Item2.ActualPath, item.Item2.Sha256, item.Item2.SizeBytes, required.Append(ownClass).Distinct(StringComparer.Ordinal).ToArray(),
                policy, new HashSet<string>(StringComparer.Ordinal) { ownClass }, token).ConfigureAwait(false);
            if (required.Any(observed.Found.ContainsKey)) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_SUPPORT_SHADOW", "A host support JAR shadows a required game launch class.");
            if (!observed.Found.ContainsKey(ownClass)) throw new MmcRuntimeLaunchException("MMC_RUNTIME_SUPPORT_ENTRY", "An owned support JAR is missing its required host entrypoint.");
        }
    }

    private static async Task<JarResult> ReadJarAsync(string key, string path, string expectedSha256, long expectedSize,
        string[] required, JarRuntimePolicy policy, HashSet<string> compatibilityRequired, CancellationToken token) {
        MmcRuntimeFiles.Hash(expectedSha256); MmcInstancePaths.EnsureNoLinks(path);
        using FileStream source = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        string digest = await MmcRuntimeFiles.HashStreamAsync(source, token).ConfigureAwait(false);
        if (!digest.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase) || source.Length != expectedSize)
            throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CHANGED", "A classpath/support JAR differs from its adopted bytes.");
        source.Position = 0;
        var index = MmcReadOnlyJarPreflight.Inspect(source);
        source.Position = 0;
        using ZipArchive archive = new(source, ZipArchiveMode.Read, true, new UTF8Encoding(false, true));
        if (archive.Entries.Count != index.Names.Length || archive.Entries.Where((entry, position) => entry.FullName != index.Names[position]).Any()) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_ZIP", "Classpath ZIP index changed during bounded inspection.");
        var manifests = archive.Entries.Where(e => e.FullName.Equals("META-INF/MANIFEST.MF", StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        if (manifests.Length > 1) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "Ambiguous JAR manifest entries.");
        var manifest = manifests.Length == 0 ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : ReadManifest(manifests[0]);
        if (manifest.TryGetValue("Class-Path", out string? referenced) && !string.IsNullOrWhiteSpace(referenced))
            throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST_INPUT", "Implicit manifest Class-Path inputs are not a verified ordered launch plan.");
        bool multiRelease = policy.MultiReleaseEnabled && manifest.TryGetValue("Multi-Release", out string? enabled) && enabled.Equals("true", StringComparison.OrdinalIgnoreCase);
        var names = required.ToDictionary(name => name.Replace('.', '/') + ".class", name => name, StringComparer.Ordinal);
        Dictionary<string, (ZipArchiveEntry Entry, int Release)> selected = new(StringComparer.Ordinal);
        HashSet<string> relevantEntries = new(StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in archive.Entries) {
            token.ThrowIfCancellationRequested();
            string candidate = entry.FullName; int release = 0;
            if (candidate.StartsWith("META-INF/versions/", StringComparison.Ordinal)) {
                if (!multiRelease) continue;
                int separator = candidate.IndexOf('/', "META-INF/versions/".Length);
                if (separator < 0 || !int.TryParse(candidate.AsSpan("META-INF/versions/".Length, separator - "META-INF/versions/".Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out release) ||
                    release < 9 || release > policy.MaximumRelease || !candidate.AsSpan("META-INF/versions/".Length, separator - "META-INF/versions/".Length).SequenceEqual(release.ToString(System.Globalization.CultureInfo.InvariantCulture))) continue;
                candidate = candidate[(separator + 1)..];
            }
            if (!names.TryGetValue(candidate, out string? name)) continue;
            if (!relevantEntries.Add(name + ":" + release)) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CLASS_AMBIGUOUS", "Duplicate effective class entry in one locked JAR.");
            if (!selected.TryGetValue(name, out var previous) || release > previous.Release) selected[name] = (entry, release);
        }
        Dictionary<string, Found> found = new(StringComparer.Ordinal);
        foreach (var item in selected) {
            if (!compatibilityRequired.Contains(item.Key)) {
                // The JVM never defines this shadowed required class. Keep its
                // explicit presence/path without rejecting an unused header.
                found.Add(item.Key, new(item.Value.Entry.FullName, item.Value.Release, 0));
                continue;
            }
            using Stream input = item.Value.Entry.Open(); byte[] header = new byte[8];
            try { await input.ReadExactlyAsync(header, token).ConfigureAwait(false); }
            catch (EndOfStreamException) { throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CLASS_HEADER", "Truncated required class header."); }
            int minor = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4)), major = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(6));
            if (BinaryPrimitives.ReadUInt32BigEndian(header) != 0xcafebabe || major < 45 || major > policy.JavaMajor + 44 || minor == 65535 && (!policy.Preview || major != policy.JavaMajor + 44))
                throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CLASS_VERSION", "Required class bytecode does not match the selected Java/preview capability.");
            found.Add(item.Key, new(item.Value.Entry.FullName, item.Value.Release, major));
        }
        return new(new(key, digest, required.Select(name => found.TryGetValue(name, out var value) ? new MmcJarClassPresence(name, true, value.Path) : new(name, false)).ToImmutableArray()), found);
    }

    // Oracle JarFile implementation properties, applied once at VM startup.
    // 'force' enables runtime versioning but does not invent a missing manifest
    // attribute. Standard URL class loaders already request runtimeVersion().
    private static JarRuntimePolicy RuntimePolicy(int javaMajor, IEnumerable<string> jvm) {
        string? version = null, enabled = null; bool preview = false;
        foreach (string token in jvm) {
            if (token == "--enable-preview") preview = true;
            if (token == "-Djdk.util.jar.version") version = "";
            else if (token.StartsWith("-Djdk.util.jar.version=", StringComparison.Ordinal)) version = token["-Djdk.util.jar.version=".Length..];
            if (token == "-Djdk.util.jar.enableMultiRelease") enabled = "";
            else if (token.StartsWith("-Djdk.util.jar.enableMultiRelease=", StringComparison.Ordinal)) enabled = token["-Djdk.util.jar.enableMultiRelease=".Length..];
        }
        if (javaMajor < 9) return new(javaMajor, 8, false, preview);
        int maximum = version is null ? javaMajor : Math.Clamp(JavaInteger(version), 8, javaMajor);
        return new(javaMajor, maximum, enabled != "false", preview);
    }
    private static int JavaInteger(string value) {
        int index = 0; bool negative = false;
        if (value.StartsWith('+') || value.StartsWith('-')) { negative = value[0] == '-'; index++; }
        if (index == value.Length) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MR_VERSION", "The effective JAR runtime-version property is not a Java integer.");
        long result = 0, limit = negative ? 2147483648L : int.MaxValue;
        for (; index < value.Length; index++) {
            int digit = System.Globalization.CharUnicodeInfo.GetDecimalDigitValue(value[index]);
            if (digit < 0 || result > (limit - digit) / 10) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MR_VERSION", "The effective JAR runtime-version property is not a Java integer.");
            result = result * 10 + digit;
        }
        return negative ? (int)-result : (int)result;
    }

    private static Dictionary<string, string> ReadManifest(ZipArchiveEntry entry) {
        if (entry.Length > 1024 * 1024) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "JAR manifest exceeds its byte budget.");
        using Stream input = entry.Open(); using MemoryStream bytes = new(); byte[] buffer = new byte[4096];
        while (true) { int read = input.Read(buffer, 0, buffer.Length); if (read == 0) break; if (bytes.Length + read > 1024 * 1024) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "JAR manifest grew beyond its byte budget."); bytes.Write(buffer, 0, read); }
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes.ToArray()); }
        catch (DecoderFallbackException) { throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "JAR manifest is not valid UTF-8."); }
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase); string? current = null;
        foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            if (line.Length == 0) break;
            if (line[0] == ' ') { if (current is null) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "Invalid manifest continuation."); result[current] += line[1..]; continue; }
            int split = line.IndexOf(':'); if (split <= 0) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "Invalid manifest attribute.");
            current = line[..split]; if (!result.TryAdd(current, line[(split + 1)..].TrimStart(' '))) throw new MmcRuntimeLaunchException("MMC_CLASSPATH_MANIFEST", "Duplicate manifest main attribute.");
        }
        return result;
    }
    private static void ValidateNames(IEnumerable<string> names) {
        foreach (string name in names) if (string.IsNullOrWhiteSpace(name) || name.Length > 512 || name.Any(c => char.IsControl(c) || c is '/' or '\\' or ':' or '{' or '}' or ' '))
            throw new MmcRuntimeLaunchException("MMC_CLASSPATH_CLASS", "Invalid required class binary name.");
    }
}
