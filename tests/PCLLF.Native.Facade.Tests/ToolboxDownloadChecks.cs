#nullable disable // Mechanically retained original C# fixture; intentional null-input ABI cases.
// LF Test-ToolboxDownload.ps1 through the actual Native .NET10 Main, with original assertion line IDs.
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PCLLF.Native.Facade.Tests;
internal static class ToolboxDownloadChecks {
    static Type session;
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Call(string name, object instance, params object[] args) {
        try { return session.GetMethod(name, Flags).Invoke(instance, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }
    static string Field(object instance, string name) { return (string)session.GetField(name, Flags).GetValue(instance); }
    static void ExpectFailure(int line, Action action, Type kind) {
        Exception rejected = null; try { action(); } catch (Exception error) { rejected = error; }
        ToolboxChecks.Check("Test-ToolboxDownload.ps1", line, rejected != null && kind.IsInstanceOfType(rejected), "Expected rejection: " + kind.Name + "; actual=" + rejected);
    }
    static void Check(int line, bool passed, string message) => ToolboxChecks.Check("Test-ToolboxDownload.ps1", line, passed, message);
    static void CleanFixture(string root) {
        string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\') + "\\";
        Guid id;
        const string marker = "LivingPCL-ToolboxDownload-";
        root = Path.GetFullPath(root);
        string leaf = Path.GetFileName(root);
        if (!root.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !leaf.StartsWith(marker) ||
            !Guid.TryParseExact(leaf.Substring(marker.Length), "N", out id)) throw new Exception("Unsafe fixture root");
        if (!Directory.Exists(root)) return;
        foreach (string path in Directory.GetFileSystemEntries(root)) {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new Exception("Unexpected fixture link");
        }
        Directory.Delete(root, true);
    }
    public static void Run(Assembly assembly) {
        session = assembly.GetType("PCL.ToolboxDownloadSession", true);
        Check(40, (string)Call("ValidateName", null, "normal.zip") == "", "Normal filename rejected");
        foreach (string name in new [] { "", "..", "../a", "a\\b", "a:b", "NUL.txt", "COM1", "LPT\u00b2.zip", "a.", " a", "a " })
            Check(42, (string)Call("ValidateName", null, name) != "", "Invalid filename accepted: " + name);
        foreach (string url in new [] { "file:///C:/a", "ftp://example.com/a", "https://user:pass@example.com/a", "not-a-url" })
            ExpectFailure(44, () => Call("ParseUrl", null, url), typeof(ArgumentException));
        Call("ParseUrl", null, "http://127.0.0.1/a");
        Check(46, (string)Call("SuggestName", null, "https://example.com/a+b.zip?file=bad#fragment") == "a+b.zip", "Query/plus filename changed");
        Check(47, (string)Call("SuggestName", null, "https://example.com/%E6%B5%8B%E8%AF%95.zip") == "\u6d4b\u8bd5.zip", "Unicode filename changed");
        Check(48, (string)Call("SuggestName", null, "https://example.com/a%2Fb.zip") == "", "Decoded traversal accepted");
        Type credentials = assembly.GetType("PCL.BuildCredentials", true);
        MethodInfo policy = credentials.GetMethod("IsCurseForgeRequest", Flags);
        foreach (string uri in new [] { "https://api.curseforge.com/v1/mods", "https://API.CURSEFORGE.COM:443/v1/mods" })
            Check(52, (bool)policy.Invoke(null, new object[] { new Uri(uri) }), "Official API rejected");
        foreach (string uri in new [] { "https://evil.example/?api.curseforge.com", "https://api.curseforge.com.evil.example/", "http://api.curseforge.com/", "https://api.curseforge.com:444/", "https://user@api.curseforge.com/", "https://media.forgecdn.net/file" })
            Check(54, !(bool)policy.Invoke(null, new object[] { new Uri(uri) }), "API header policy accepts untrusted destination");

        string root = Path.Combine(Path.GetTempPath(), "LivingPCL-ToolboxDownload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            ExpectFailure(59, () => Call("ResolveTarget", null, "relative", "file.zip"), typeof(ArgumentException));
            ExpectFailure(60, () => Call("ResolveTarget", null, root, "..\\escape.zip"), typeof(ArgumentException));
            string target = Path.Combine(root, "saved.zip");
            File.WriteAllText(target, "original-content");
            object active = Call("Create", null, root, "saved.zip");
            ExpectFailure(64, () => Call("Create", null, root.ToUpperInvariant(), "SAVED.ZIP"), typeof(IOException));
            string stage = Field(active, "StagePath");
            File.WriteAllText(stage, "new-content");
            Check(67, File.ReadAllText(target) == "original-content", "Original changed before commit");
            ExpectFailure(68, () => Call("Commit", active, false, new Func<bool>(() => false)), typeof(IOException));
            Check(69, File.ReadAllText(target) == "original-content", "Overwrite rejection changed original");
            Call("Complete", active, false);
            Check(71, !Directory.Exists(Field(active, "StageFolder")), "Canceled staging retained");
            // A late merge can recreate the canceled staging file; the callback removes only that exact file.
            Directory.CreateDirectory(Field(active, "StageFolder"));
            File.WriteAllText(stage, "late-merge");
            Call("OnCanceledMergeFinished", active);
            Check(76, !Directory.Exists(Field(active, "StageFolder")), "Late merge was retained");
            Call("Complete", active, false);
            foreach (bool existed in new [] { false, true }) {
                string name = existed ? "saved.zip" : "new.zip";
                string destination = Path.Combine(root, name);
                active = Call("Create", null, root, name);
                File.WriteAllText(Field(active, "StagePath"), "cancel-before");
                ExpectFailure(83, () => Call("Commit", active, true, new Func<bool>(() => true)), typeof(OperationCanceledException));
                Check(84, existed ? File.ReadAllText(destination) == "original-content" : !File.Exists(destination), "Pre-commit cancellation changed destination");
                Call("Complete", active, false);

                active = Call("Create", null, root, name);
                File.WriteAllText(Field(active, "StagePath"), "cancel-before-combo-finish");
                Call("Commit", active, true, new Func<bool>(() => false));
                Call("Complete", active, false);
                Check(91, existed ? File.ReadAllText(destination) == "original-content" : !File.Exists(destination), "Cancel after save did not restore destination");

                active = Call("Create", null, root, name);
                File.WriteAllText(Field(active, "StagePath"), "cancel-after");
                int checks = 0;
                ExpectFailure(96, () => Call("Commit", active, true, new Func<bool>(() => ++checks > 1)), typeof(OperationCanceledException));
                Check(97, existed ? File.ReadAllText(destination) == "original-content" : !File.Exists(destination), "Post-commit cancellation did not roll back");
                Call("Complete", active, false);

                active = Call("Create", null, root, name);
                File.WriteAllText(Field(active, "StagePath"), "completed-content");
                Call("Commit", active, true, new Func<bool>(() => false));
                Call("Complete", active, true);
                Check(104, File.ReadAllText(destination) == "completed-content", "Successful save failed");
                Check(105, !Directory.Exists(Field(active, "StageFolder")), "Successful staging retained");
            }
            Check(107, Directory.GetDirectories(root).Length == 0, "Unexpected remaining staging directory");
            active = Call("Create", null, root, "locked.zip");
            File.WriteAllText(Field(active, "StagePath"), "locked-new-file");
            Call("Commit", active, true, new Func<bool>(() => false));
            using (FileStream locked = File.Open(Path.Combine(root, "locked.zip"), FileMode.Open, FileAccess.Read, FileShare.None)) {
                Call("Complete", active, false);
                string pending = (string)session.GetProperty("UnrevertedPath", Flags).GetValue(active, null);
                Check(114, pending == Path.Combine(root, "locked.zip"), "Unreverted new file was not reported");
            }
        } finally { CleanFixture(root); }
    }
}