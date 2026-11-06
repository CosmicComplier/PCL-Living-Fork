using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public enum PackDependencyFileRole {
    AuthorUnchanged, AuthorRenamed, AuthorModified, AuthorDisabled, AuthorMissing,
    OptionalAbsent, PersonalAdded, Ambiguous, Unknown
}
public sealed class PackDependencyFile {
    public string Relative = "";
    public string? LocalRelative, Identity, FileIdentity, ExpectedHash, ActualHash;
    public PackDependencyFileRole Role;
    public bool Disabled;
    public List<string> Candidates = new();
}
public sealed class PackDependencyOverride {
    public string AuthorRelative = "", LocalRelative = "", ActualHash = "";
    public string? FileIdentity, Reason;
}

/// <summary>Read-only author provenance. It is not evidence that a dependency is satisfied or compatible.</summary>
public sealed class PackDependencyOrigin {
    public bool IsPack, Complete, NeedsAttention;
    public string Fingerprint = "";
    public List<string> Notes = new();
    public List<PackDependencyFile> Files = new();
    private readonly HashSet<string> projects = new(StringComparer.Ordinal);
    private string versionFolder = "", gameRoot = "";
    private bool packMode;
    private Func<string, JObject?> readManifest = null!;
    private Func<string, string, CancellationToken, string?> digest = null!;
    private JObject? validatedManifest;
    private static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;
    public string GameRoot => gameRoot;
    public string VersionFolder => versionFolder;

    public static PackDependencyOrigin Read(string versionFolder, string gameRoot, bool packMode,
        Func<string, JObject?> readManifest, Func<string, string, CancellationToken, string?> digest,
        CancellationToken token = default) {
        if (readManifest is null) throw new ArgumentNullException(nameof(readManifest));
        if (digest is null) throw new ArgumentNullException(nameof(digest));
        token.ThrowIfCancellationRequested();
        PackDependencyOrigin result = new() {
            versionFolder = Path.GetFullPath(versionFolder), gameRoot = Path.GetFullPath(gameRoot),
            packMode = packMode, readManifest = readManifest, digest = digest, IsPack = packMode
        };
        result.ReadCore(token);
        return result;
    }
    public PackDependencyOrigin Refresh(CancellationToken token = default) =>
        Read(versionFolder, gameRoot, packMode, readManifest, digest, token);

    public bool IsPersonal(string fullPath) {
        if (!Complete) return false;
        PackDependencyFile? match = FindLocal(fullPath);
        return match is not null && match.Role == PackDependencyFileRole.PersonalAdded && match.ActualHash is not null;
    }
    public PackDependencyFile? FindLocal(string fullPath) {
        string path;
        try { path = Path.GetFullPath(fullPath); } catch (ArgumentException) { return null; }
        var matches = Files.Where(file => !string.IsNullOrEmpty(file.LocalRelative) &&
            Paths.Equals(Path.GetFullPath(Path.Combine(gameRoot, file.LocalRelative!.Replace('/', Path.DirectorySeparatorChar))), path)).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
    // Only plan the next JSON; callers commit it together with their explicitly
    // accepted file replacement. Author expectations never become local bytes.
    public JObject BuildOverrideManifest(IReadOnlyList<PackDependencyOverride> overrides, CancellationToken token = default) {
        if (overrides is null) throw new ArgumentNullException(nameof(overrides));
        if (!IsPack || !Complete || validatedManifest is null) throw new IOException("作者基线不完整，不能记录作者文件替换；请先用正确旧包核对。");
        PackDependencyOrigin current = Refresh(token);
        if (!current.Complete || current.validatedManifest is null || current.Fingerprint != Fingerprint)
            throw new IOException("作者清单或模组文件已变化，请重新检查后再选择替换。");
        JObject next = (JObject)current.validatedManifest.DeepClone();
        JObject expected = (JObject)next["files"]!;
        HashSet<string> authors = new(Paths), destinations = new(Paths);
        foreach (PackDependencyOverride item in overrides) {
            if (item is null) throw new IOException("作者替换记录无效。");
            string authorPath = Relative(item.AuthorRelative), localPath = Relative(item.LocalRelative);
            if (!ModFile(authorPath) || !ModFile(localPath) || !authors.Add(authorPath) || !destinations.Add(localPath))
                throw new IOException("作者替换记录包含重复或不支持的路径。");
            JProperty? declaration = expected.Properties().SingleOrDefault(property => Paths.Equals(property.Name, authorPath));
            PackDependencyFile[] source = current.Files.Where(file => Paths.Equals(file.Relative, authorPath) && file.ExpectedHash is not null).ToArray();
            if (declaration?.Value is not JObject entry || source.Length != 1 ||
                source[0].Role is PackDependencyFileRole.Ambiguous or PackDependencyFileRole.Unknown or PackDependencyFileRole.PersonalAdded)
                throw new IOException("无法唯一确认被替换文件的作者归属：" + authorPath);
            if (source[0].Disabled && !Disabled(localPath))
                throw new IOException("不能在记录作者替换时隐式启用已禁用文件：" + authorPath);
            EnsureSafe(gameRoot, Path.Combine(gameRoot, localPath.Replace('/', Path.DirectorySeparatorChar)));
            if (expected.Properties().Any(property => !Paths.Equals(property.Name, authorPath) &&
                (Paths.Equals(property.Name, localPath) || Paths.Equals(Text(property.Value["local"]?["relative"]), localPath))))
                throw new IOException("替换后的本地路径与其他作者文件关联冲突：" + localPath);
            PackDependencyFile? occupied = current.FindLocal(Path.Combine(gameRoot, localPath.Replace('/', Path.DirectorySeparatorChar)));
            if (occupied is not null && !Paths.Equals(occupied.Relative, authorPath))
                throw new IOException("替换后的本地路径已属于其他文件，未认领个人新增内容：" + localPath);
            string actualHash = ValidHash(item.ActualHash);
            // Keep the author's three independent expected identities intact.
            entry["state"] = "modified";
            entry["local"] = new JObject(new JProperty("relative", localPath), new JProperty("hash", actualHash),
                new JProperty("ownership", "author"), new JProperty("fileIdentity", item.FileIdentity), new JProperty("reason", item.Reason));
            entry["baselineRelative"] ??= authorPath;
            entry["originalHash"] ??= entry["hash"]!.DeepClone();
        }
        return next;
    }
    public bool ProtectsProject(string platform, string projectId) =>
        projects.Contains(ProjectKey(platform, projectId));
    private static string ProjectKey(string platform, string projectId) =>
        platform.ToLowerInvariant() + ":" + projectId;
    private static bool Disabled(string path) => path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".old", StringComparison.OrdinalIgnoreCase);
    private static bool ModFile(string relative) =>
        relative.StartsWith("mods/", StringComparison.OrdinalIgnoreCase) && relative.IndexOf('/', 5) < 0 &&
        ModName(relative);
    private static bool ModName(string path) => Disabled(path) ||
        new[] { ".jar", ".zip", ".litemod" }.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    private void UnverifiedVersionFolder() {
        Complete = false; NeedsAttention = true;
        const string note = "作者声明或本地文件包含 mods 版本子目录；当前来源服务尚不能按加载上下文完整核对它，未将任何额外文件认作可自动修改的个人新增。";
        if (!Notes.Contains(note)) Notes.Add(note);
    }

    private void ReadCore(CancellationToken token) {
        List<string> stamps = new() { "pack-origin-v1", versionFolder, gameRoot, "mode:" + packMode };
        JObject? manifest = null;
        string manifestPath = Path.Combine(versionFolder, "PCL", "ModpackOrigin.json");
        try {
            // Hint checks are bounded metadata checks and never load arbitrary package content.
            IsPack |= Exists(manifestPath);
            foreach (string root in new[] { versionFolder, gameRoot }.Distinct(Paths))
                foreach (string name in new[] { "modrinth.index.json", "manifest.json" }) {
                    string hint = Path.Combine(root, name);
                    if (!Exists(hint)) { stamps.Add("hint-missing:" + hint); continue; }
                    IsPack = true;
                    EnsureSafe(root, hint);
                    stamps.Add("hint:" + hint + ":" + Hash(hint, "sha256", token));
                }
            if (Exists(manifestPath)) {
                EnsureSafe(versionFolder, manifestPath);
                if (new FileInfo(manifestPath).Length > 16 * 1024 * 1024) throw new IOException("作者记录超过读取限制");
                stamps.Add("manifest:" + Hash(manifestPath, "sha256", token));
            } else stamps.Add("manifest:missing");
            manifest = readManifest(versionFolder);
            if (manifest is not null) {
                IsPack = true;
                stamps.Add("validated:" + manifest.ToString(Formatting.None));
                Complete = manifest["schema"]?.Type == JTokenType.Integer && manifest["schema"]!.ToString() == "2" &&
                    manifest["complete"]?.Type == JTokenType.Boolean && manifest["complete"]!.Value<bool>() &&
                    !(manifest["baselineNeedsRepair"]?.Type == JTokenType.Boolean && manifest["baselineNeedsRepair"]!.Value<bool>());
                if (manifest["files"] is not JObject) throw new IOException("作者文件清单无效");
            }
        } catch (OperationCanceledException) { throw; }
        catch (Exception error) when (ReadError(error)) {
            IsPack = true; Complete = false; NeedsAttention = true;
            Notes.Add("整合包来源记录无法完整读取，未按它授权修改文件；请用正确旧包核对或补录。");
            stamps.Add("manifest-error:" + error.GetType().Name + ":" + error.Message);
        }
        if (!IsPack) {
            SetFingerprint(stamps);
            return;
        }
        if (manifest is null || !Complete) {
            NeedsAttention = true;
            Notes.Add("作者基线缺失、旧版或不完整；未列出的本地文件来源未知，不视作个人新增。");
        }
        Dictionary<string, LocalFile> locals = new(Paths);
        try {
            string mods = Path.Combine(gameRoot, "mods");
            EnsureSafe(gameRoot, mods);
            if (Directory.Exists(mods)) {
                foreach (string path in Directory.GetFiles(mods).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ThenBy(path => path, StringComparer.Ordinal)) {
                    token.ThrowIfCancellationRequested();
                    string relative = "mods/" + Path.GetFileName(path);
                    if (!ModFile(relative)) continue;
                    LocalFile file = new() { Relative = relative, FullPath = path };
                    try { EnsureSafe(gameRoot, path); file.ActualHash = Hash(path, "sha1", token); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error) when (ReadError(error)) { file.Error = error.GetType().Name + ":" + error.Message; Complete = false; NeedsAttention = true; }
                    if (!locals.TryAdd(relative, file)) throw new IOException("模组路径存在大小写歧义");
                    stamps.Add("local:" + relative + ":" + file.ActualHash + ":" + file.Error);
                }
                // The legacy Forge inspector also reads mods/<MCVersion>. This
                // provenance service has no loader context, so do not silently
                // certify its top-level inventory as complete when such files exist.
                foreach (string folder in Directory.GetDirectories(mods).OrderBy(path => path, Paths)) {
                    token.ThrowIfCancellationRequested();
                    if (!Regex.IsMatch(Path.GetFileName(folder), @"\A[0-9.]+\z")) continue;
                    EnsureSafe(gameRoot, folder);
                    foreach (string path in Directory.GetFiles(folder).Where(path => ModName(path) && !Disabled(path)).OrderBy(path => path, Paths)) {
                        token.ThrowIfCancellationRequested();
                        UnverifiedVersionFolder(); EnsureSafe(gameRoot, path);
                        stamps.Add("unverified-version-file:" + path + ":" + Hash(path, "sha1", token));
                    }
                }
            }
        } catch (OperationCanceledException) { throw; }
        catch (Exception error) when (ReadError(error)) {
            Complete = false; NeedsAttention = true;
            Notes.Add("本地模组列表无法完整读取，文件归属仅作提示，不授权自动修改。");
            stamps.Add("local-error:" + error.GetType().Name + ":" + error.Message);
        }
        List<Declaration> declarations = new();
        try {
            if (manifest?["files"] is JObject entries) {
                HashSet<string> seen = new(Paths);
                foreach (JProperty property in entries.Properties()) {
                    token.ThrowIfCancellationRequested();
                    string relative = Relative(property.Name);
                    if (!seen.Add(relative)) throw new IOException("作者路径重复");
                    if (property.Value is not JObject record) throw new IOException("作者条目无效");
                    string expected = ValidHash(record["hash"]?.ToString());
                    // Protect all author project declarations, not only currently installed files.
                    string? identity = Text(record["identity"]);
                    if (!string.IsNullOrEmpty(identity)) projects.Add(IdentityKey(identity!));
                    if (relative.StartsWith("mods/", StringComparison.OrdinalIgnoreCase) && relative.IndexOf('/', 5) >= 0 && ModName(relative)) {
                        UnverifiedVersionFolder();
                        Files.Add(new() { Relative = relative, Identity = identity, FileIdentity = Text(record["fileIdentity"]),
                            ExpectedHash = expected, Role = PackDependencyFileRole.Unknown });
                        continue;
                    }
                    if (!ModFile(relative)) continue;
                    EnsureSafe(gameRoot, Path.Combine(gameRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                    string? observed = Text(record["local"]?["relative"]);
                    if (observed is not null) observed = Relative(observed);
                    declarations.Add(new() {
                        Relative = relative, ExpectedHash = expected, Identity = identity,
                        FileIdentity = Text(record["fileIdentity"]), Optional = (bool?)record["optional"] == true,
                        LocalRelative = observed, LocalOwnership = Text(record["local"]?["ownership"]),
                        Record = record
                    });
                }
            }
        } catch (OperationCanceledException) { throw; }
        catch (Exception error) when (ReadError(error)) {
            Complete = false; NeedsAttention = true; declarations.Clear();
            Notes.Add("作者条目包含无效路径、结构或哈希；请使用正确旧包核对，未认领个人文件。");
            stamps.Add("declaration-error:" + error.GetType().Name + ":" + error.Message);
        }
        HashSet<string> claimed = new(Paths);
        Dictionary<string, string?> hashes = new(StringComparer.Ordinal);
        foreach (Declaration entry in declarations) {
            token.ThrowIfCancellationRequested();
            PackDependencyFile file = new() {
                Relative = entry.Relative, Identity = entry.Identity, FileIdentity = entry.FileIdentity, ExpectedHash = entry.ExpectedHash
            };
            if (entry.LocalOwnership is "personal-added" or "ambiguous" &&
                entry.LocalRelative is not null && locals.TryGetValue(entry.LocalRelative, out LocalFile? protectedLocal)) {
                file.LocalRelative = protectedLocal.Relative; file.ActualHash = protectedLocal.ActualHash;
                file.Role = entry.LocalOwnership == "personal-added" && Complete ? PackDependencyFileRole.PersonalAdded : PackDependencyFileRole.Ambiguous;
                file.Disabled = Disabled(protectedLocal.Relative); file.Candidates.Add(protectedLocal.Relative);
            } else {
                var candidates = locals.Values.Where(local => Matches(local, entry.ExpectedHash, hashes, token)).Select(local => local.Relative).ToList();
                AddExisting(entry.Relative);
                AddExisting(entry.LocalRelative);
                AddExisting(entry.Relative + ".disabled");
                AddExisting(entry.Relative + ".old");
                foreach (JToken candidate in entry.Record["localCandidates"] as JArray ?? new JArray()) AddExisting(Relative(candidate.ToString()));
                file.Candidates = candidates;
                if (candidates.Count > 1) file.Role = PackDependencyFileRole.Ambiguous;
                else if (candidates.Count == 0) file.Role = entry.Optional ? PackDependencyFileRole.OptionalAbsent : PackDependencyFileRole.AuthorMissing;
                else {
                    LocalFile actual = locals[candidates[0]];
                    file.LocalRelative = actual.Relative; file.ActualHash = actual.ActualHash; file.Disabled = Disabled(actual.Relative);
                    file.Role = actual.ActualHash is null ? PackDependencyFileRole.Unknown : file.Disabled ? PackDependencyFileRole.AuthorDisabled :
                        Matches(actual, entry.ExpectedHash, hashes, token) ?
                            Paths.Equals(actual.Relative, entry.Relative) ? PackDependencyFileRole.AuthorUnchanged : PackDependencyFileRole.AuthorRenamed :
                            PackDependencyFileRole.AuthorModified;
                }
                void AddExisting(string? candidate) {
                    if (candidate is not null && locals.ContainsKey(candidate) && !candidates.Contains(candidate, Paths)) candidates.Add(candidate);
                }
            }
            foreach (string candidate in file.Candidates) claimed.Add(candidate);
            if (!string.IsNullOrEmpty(file.LocalRelative)) claimed.Add(file.LocalRelative);
            Files.Add(file);
        }
        // A physical file cannot be the unique author or personal counterpart of two declarations.
        foreach (var collision in Files.Where(file => !string.IsNullOrEmpty(file.LocalRelative)).GroupBy(file => file.LocalRelative!, Paths).Where(group => group.Count() > 1))
            foreach (PackDependencyFile file in collision) file.Role = PackDependencyFileRole.Ambiguous;
        JObject? retained = manifest?["retained"] as JObject;
        foreach (LocalFile local in locals.Values.Where(local => !claimed.Contains(local.Relative))) {
            token.ThrowIfCancellationRequested();
            JToken? preserved = retained?[local.Relative];
            Files.Add(new() {
                Relative = local.Relative, LocalRelative = local.Relative, ActualHash = local.ActualHash, Disabled = Disabled(local.Relative),
                ExpectedHash = preserved?["hash"]?.ToString(), Identity = Text(preserved?["identity"]), FileIdentity = Text(preserved?["fileIdentity"]),
                Role = local.ActualHash is null ? PackDependencyFileRole.Unknown : preserved is not null ? PackDependencyFileRole.AuthorModified :
                    Complete ? PackDependencyFileRole.PersonalAdded : PackDependencyFileRole.Unknown
            });
        }
        foreach (PackDependencyFile file in Files) {
            if (file.Role is PackDependencyFileRole.AuthorMissing or PackDependencyFileRole.Ambiguous or PackDependencyFileRole.Unknown) NeedsAttention = true;
            stamps.Add("file:" + file.Relative + "|" + file.LocalRelative + "|" + file.Role + "|" + file.ExpectedHash + "|" + file.ActualHash + "|" +
                file.Identity + "|" + file.FileIdentity + "|" + file.Disabled + "|" + string.Join(";", file.Candidates.OrderBy(value => value, Paths)));
        }
        if (Complete && manifest is not null) validatedManifest = (JObject)manifest.DeepClone();
        Notes.Add("作者声明用于核对文件来源，不证明模组依赖或运行兼容；保留个人修改、禁用和选装状态。");
        SetFingerprint(stamps);
    }
    private bool Matches(LocalFile file, string expected, Dictionary<string, string?> hashes, CancellationToken token) {
        if (file.ActualHash is null) return false;
        string algorithm = expected.Split(':')[0], key = algorithm + "\0" + file.FullPath;
        if (algorithm == "sha1") return string.Equals(file.ActualHash, expected, StringComparison.OrdinalIgnoreCase);
        if (!hashes.TryGetValue(key, out string? actual)) {
            try { EnsureSafe(gameRoot, file.FullPath); actual = Hash(file.FullPath, algorithm, token); }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) when (ReadError(error)) { actual = null; Complete = false; NeedsAttention = true; }
            hashes[key] = actual;
        }
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
    private string? Hash(string path, string algorithm, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        string? value = digest(path, algorithm, token);
        token.ThrowIfCancellationRequested();
        return value is null ? null : ValidHash(value);
    }
    private static string ValidHash(string? value) {
        if (value is null) throw new IOException("缺少哈希");
        string[] parts = value.Split(':');
        if (parts.Length != 2) throw new IOException("无效哈希");
        string algorithm = parts[0].ToLowerInvariant();
        int length = algorithm switch { "md5" => 32, "sha1" => 40, "sha256" => 64, "sha512" => 128, _ => throw new IOException("未知哈希算法") };
        if (!Regex.IsMatch(parts[1], @"\A[0-9a-fA-F]{" + length + @"}\z")) throw new IOException("无效哈希");
        return algorithm + ":" + parts[1].ToLowerInvariant();
    }
    private static string IdentityKey(string identity) {
        int split = identity.IndexOf(':');
        return split > 0 ? ProjectKey(identity.Substring(0, split), identity.Substring(split + 1)) : identity;
    }
    private static string? Text(JToken? value) => value?.Type is null or JTokenType.Null ? null : value.ToString();
    private static bool Exists(string path) {
        try { File.GetAttributes(path); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }
    private static bool ReadError(Exception error) => error is IOException or UnauthorizedAccessException or FormatException or
        ArgumentException or JsonException or InvalidOperationException or OverflowException;
    private void SetFingerprint(IEnumerable<string> stamps) {
        using SHA256 hash = SHA256.Create();
        Fingerprint = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", stamps.OrderBy(value => value, StringComparer.Ordinal)))));
    }
    private static string Relative(string path) {
        path = path.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) throw new IOException("无效相对路径");
        foreach (string part in path.Split('/'))
            if (part.Length == 0 || part is "." or ".." || part.EndsWith(".") || part.EndsWith(" ") ||
                part.Any(value => value < 32 || "<>:\"|?*".Contains(value)) ||
                Regex.IsMatch(part, @"\A(?:CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|$)", RegexOptions.IgnoreCase))
                throw new IOException("无效相对路径");
        return path;
    }
    private static void EnsureSafe(string root, string path) {
        string parent = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string full = Path.GetFullPath(path);
        StringComparison comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(parent, full, comparison) && !full.StartsWith(parent + Path.DirectorySeparatorChar, comparison)) throw new IOException("路径超出实例目录");
        string? current = full;
        while (!string.IsNullOrEmpty(current)) {
            try { if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("拒绝链接路径"); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            current = Path.GetDirectoryName(current);
        }
    }
    private sealed class LocalFile {
        public string Relative = "", FullPath = "";
        public string? ActualHash, Error;
    }
    private sealed class Declaration {
        public string Relative = "", ExpectedHash = "";
        public string? Identity, FileIdentity, LocalRelative, LocalOwnership;
        public bool Optional;
        public JObject Record = null!;
    }
}
