#nullable disable // Mechanically retained original C# fixture; intentional null-input ABI cases.
// LF Test-Toolbox.ps1 through the actual Native .NET10 Main, with original assertion line IDs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PCLLF.Native.Facade.Tests;

internal static class ToolboxCleanupChecks {
    internal static void Run(Assembly assembly) {
        CheckMemory(assembly);
        string root = null;
        try {
            root = CreateFixture();
            foreach (string[] pair in Junctions(root)) CreateJunction(pair[0], pair[1]);
            CheckCleaner(assembly, root);
        } finally { if (root != null) Cleanup(root); }
    }
    private static void CreateJunction(string path, string target) {
        Directory.CreateDirectory(path);
        string full = Path.GetFullPath(target);
        string substitute = full.StartsWith("\\\\", StringComparison.Ordinal) ? "\\??\\UNC\\" + full.Substring(2) : "\\??\\" + full;
        byte[] name = System.Text.Encoding.Unicode.GetBytes(substitute), print = System.Text.Encoding.Unicode.GetBytes(full);
        byte[] data = new byte[16 + name.Length + 2 + print.Length + 2];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data, 0xA0000003);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), checked((ushort)(data.Length - 8)));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(10), checked((ushort)name.Length));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(12), checked((ushort)(name.Length + 2)));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(14), checked((ushort)print.Length));
        name.CopyTo(data, 16); print.CopyTo(data, 16 + name.Length + 2);
        using (Microsoft.Win32.SafeHandles.SafeFileHandle handle = CreateFileW(path, 0x40000000, 0, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero))
            if (handle.IsInvalid || !DeviceIoControl(handle, 0x000900A4, data, data.Length, IntPtr.Zero, 0, out _, IntPtr.Zero))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Actual toolbox fixture junction creation failed.");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) throw new IOException("No actual NTFS junction was created.");
    }
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll",ExactSpelling=true,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle handle,uint code,byte[] input,int inputSize,IntPtr output,int outputSize,out int returned,IntPtr overlapped);
    private const string Marker = "LivingPCL-Toolbox-";
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private sealed class FakeLease : IDisposable {
        public int Disposals;
        public void Dispose() { Disposals++; }
    }

    private static void Require(int line, bool value, string message) => ToolboxChecks.Check("Test-Toolbox.ps1", line, value, message);

    private static Exception Failure(Action action) {
        try { action(); }
        catch (Exception error) {
            while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException;
            return error;
        }
        throw new Exception("Expected a synthetic failure, but the operation succeeded");
    }

    public static void CheckMemory(Assembly assembly) {
        Type optimizer = assembly.GetType("PCL.NativeMemoryOptimizer", true);
        MethodInfo execute = optimizer.GetMethod("ExecuteCommands", StaticFlags);
        Require(40, execute != null, "Missing injectable memory command executor");
        // Deliberately never look up or invoke Run, AcquireProfilePrivilege, or any P/Invoke.
        Type token = optimizer.GetNestedType("PrivilegeToken", BindingFlags.NonPublic);
        Require(43, token != null && Marshal.SizeOf(token) == 16, "PrivilegeToken must occupy 16 bytes");
        Require(44, Marshal.OffsetOf(token, "Luid").ToInt32() == 4, "PrivilegeToken.Luid must be at offset 4");
        Require(45, Marshal.OffsetOf(token, "Attributes").ToInt32() == 12, "PrivilegeToken.Attributes must be at offset 12");

        List<int> calls = new List<int>();
        FakeLease lease = new FakeLease();
        int acquisitions = 0;
        Func<IDisposable> acquire = () => { acquisitions++; return lease; };
        Func<int, int> apply = command => { calls.Add(command); return 0; };
        execute.Invoke(null, new object[] { acquire, apply });
        Require(53, acquisitions == 1 && calls.SequenceEqual(new[] { 2, 3, 4 }) && lease.Disposals == 1,
            "Memory commands must execute 2/3/4 under one disposed fake lease");

        foreach (int failureCommand in new[] { 2, 3, 4 }) {
            calls.Clear(); lease = new FakeLease(); acquisitions = 0;
            acquire = () => { acquisitions++; return lease; };
            apply = command => { calls.Add(command); return command == failureCommand ? unchecked((int)0xC0000001) : 0; };
            Exception failure = Failure(() => execute.Invoke(null, new object[] { acquire, apply }));
            Require(61, failure is InvalidOperationException && calls.SequenceEqual(new[] { 2, 3, 4 }.Take(failureCommand - 1)) &&
                acquisitions == 1 && lease.Disposals == 1, "A failed memory command did not stop and dispose its fake lease");
        }

        calls.Clear(); lease = new FakeLease();
        Exception applyFailure = new IOException("synthetic command failure");
        acquire = () => lease;
        apply = command => { calls.Add(command); if (command == 3) throw applyFailure; return 0; };
        Require(69, Object.ReferenceEquals(Failure(() => execute.Invoke(null, new object[] { acquire, apply })), applyFailure) &&
            calls.SequenceEqual(new[] { 2, 3 }) && lease.Disposals == 1, "A throwing command leaked its fake lease or continued execution");

        calls.Clear(); acquisitions = 0;
        Exception acquisitionFailure = new IOException("synthetic acquisition failure");
        acquire = () => { acquisitions++; throw acquisitionFailure; };
        apply = command => { calls.Add(command); return 0; };
        Require(76, Object.ReferenceEquals(Failure(() => execute.Invoke(null, new object[] { acquire, apply })), acquisitionFailure) &&
            acquisitions == 1 && calls.Count == 0, "Acquisition failure must prevent all memory commands");
        acquire = () => null;
        Require(79, Failure(() => execute.Invoke(null, new object[] { acquire, apply })) is InvalidOperationException && calls.Count == 0,
            "A missing fake lease must prevent all memory commands");
        acquisitions = 0;
        acquire = () => { acquisitions++; return new FakeLease(); };
        Require(83, Failure(() => execute.Invoke(null, new object[] { acquire, null })) is ArgumentNullException && acquisitions == 0,
            "A null command delegate must fail before acquiring a lease");
        Require(85, Failure(() => execute.Invoke(null, new object[] { null, apply })) is ArgumentNullException && calls.Count == 0,
            "A null acquisition delegate must prevent all memory commands");
    }

    private static string ValidateRoot(string root) {
        root = Path.GetFullPath(root).TrimEnd('\\');
        string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\');
        string leaf = Path.GetFileName(root);
        Guid id;
        if (!String.Equals(Path.GetDirectoryName(root), temp, StringComparison.OrdinalIgnoreCase) ||
            !root.StartsWith(temp + "\\", StringComparison.OrdinalIgnoreCase) ||
            !leaf.StartsWith(Marker, StringComparison.Ordinal) ||
            !Guid.TryParseExact(leaf.Substring(Marker.Length), "N", out id)) throw new Exception("Unsafe toolbox fixture root");
        if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new Exception("The toolbox fixture root must not be a link");
        return root;
    }

    private static string FixturePath(string root, string relative) {
        root = ValidateRoot(root);
        string path = Path.GetFullPath(Path.Combine(root, relative));
        Require(106, path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase), "Toolbox fixture path escaped its root");
        return path;
    }

    public static string CreateFixture() {
        string root = Path.Combine(Path.GetTempPath(), Marker + Guid.NewGuid().ToString("N"));
        ValidateRoot(root);
        Directory.CreateDirectory(root);
        foreach (string relative in new[] { "Game\\logs", "Game\\versions", "Outside", "LinkedVersionsGame" })
            Directory.CreateDirectory(FixturePath(root, relative));
        return root;
    }

    public static string[][] Junctions(string root) {
        return new[] {
            new[] { FixturePath(root, "Game\\logs\\linked-world"), FixturePath(root, "Outside") },
            new[] { FixturePath(root, "Game\\versions\\linked-version"), FixturePath(root, "Outside") },
            new[] { FixturePath(root, "LinkedRoot"), FixturePath(root, "Game") },
            new[] { FixturePath(root, "LinkedVersionsGame\\versions"), FixturePath(root, "Outside") }
        };
    }

    private static string WriteFixture(string root, string relative, string content) {
        string path = FixturePath(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
        return path;
    }

    private static string Hash(string path) {
        using (SHA256 algorithm = SHA256.Create())
        using (FileStream input = File.OpenRead(path)) return Convert.ToBase64String(algorithm.ComputeHash(input));
    }

    private static void VerifyKept(IDictionary<string, string> hashes) {
        foreach (KeyValuePair<string, string> item in hashes)
            Require(142, File.Exists(item.Key) && Hash(item.Key) == item.Value, "A protected synthetic file changed: " + Path.GetFileName(item.Key));
    }

    private static object Clean(MethodInfo clean, string[] roots, Func<bool> stop) {
        return clean.Invoke(null, new object[] { roots, stop });
    }

    private static long Value(object result, string property) {
        return Convert.ToInt64(result.GetType().GetProperty(property).GetValue(result, null));
    }

    public static void CheckCleaner(Assembly assembly, string root) {
        root = ValidateRoot(root);
        Type cleaner = assembly.GetType("PCL.GameGarbageCleaner", true);
        MethodInfo clean = cleaner.GetMethod("Clean", StaticFlags);
        Require(157, clean != null, "Missing synthetic game cleaner entry point");
        string[] garbage = {
            "Game\\logs\\latest.log", "Game\\logs\\nested\\debug.txt", "Game\\.mixin.out\\generated.class",
            "Game\\crash-reports\\crash.txt", "Game\\debug\\profile.json", "Game\\loose.LOG", "Game\\hs_err_pid100.log",
            "Game\\WailaErrorOutput.txt", "Game\\Game-natives\\native.dll", "Game\\natives-windows-x86_64\\native.dll",
            "Game\\versions\\v1\\logs\\latest.log", "Game\\versions\\v1\\v1-natives\\native.dll", "Game\\versions\\v1\\direct.log"
        };
        string[] keep = {
            "Game\\saves\\world\\level.dat", "Game\\mods\\mod.jar", "Game\\config\\settings.json", "Game\\screenshots\\image.png",
            "Game\\resourcepacks\\pack.zip", "Game\\shaderpacks\\shader.zip", "Game\\libraries\\library.jar",
            "Game\\assets\\objects\\asset", "Game\\options.txt", "Game\\hs_err_pid100.hprof", "Game\\hs_err_pid100.dmp",
            "Game\\hs_err_pid-user-data.txt", "Game\\versions\\v1\\v1.jar", "Game\\versions\\v1\\v1.json",
            "Game\\versions\\v1\\saves\\world\\level.dat", "Game\\versions\\v1\\config\\settings.json",
            "Outside\\world\\level.dat", "Outside\\outside.log", "Game\\logs\\locked.log"
        };
        long expectedBytes = 0;
        foreach (string relative in garbage) expectedBytes += new FileInfo(WriteFixture(root, relative, "delete:" + relative)).Length;
        Dictionary<string, string> hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string relative in keep) {
            string file = WriteFixture(root, relative, "keep:" + relative);
            hashes.Add(file, Hash(file));
        }
        string game = FixturePath(root, "Game");
        object result;
        using (FileStream held = File.Open(FixturePath(root, "Game\\logs\\locked.log"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            result = Clean(clean, new[] { game, game + "\\", game.ToUpperInvariant() }, null);
        Require(183, Value(result, "DeletedFiles") == garbage.Length && Value(result, "DeletedBytes") == expectedBytes,
            "Cleaner deletion totals do not match the unique synthetic garbage files");
        Require(185, Value(result, "SkippedItems") >= 3, "Locked files and junctions must be reported as skipped");
        foreach (string relative in garbage) Require(186, !File.Exists(FixturePath(root, relative)), "An allowed garbage file survived");
        VerifyKept(hashes);

        object linked = Clean(clean, new[] { FixturePath(root, "LinkedRoot") }, null);
        Require(190, Value(linked, "DeletedFiles") == 0 && Value(linked, "SkippedItems") > 0, "A linked game root was traversed");
        object linkedVersions = Clean(clean, new[] { FixturePath(root, "LinkedVersionsGame") }, null);
        Require(192, Value(linkedVersions, "DeletedFiles") == 0 && Value(linkedVersions, "SkippedItems") > 0, "A linked versions directory was traversed");

        string nestedKeep = WriteFixture(root, "NestedCase\\logs\\OtherGame\\saves\\world\\level.dat", "nested synthetic world");
        string nestedLog = WriteFixture(root, "NestedCase\\logs\\OtherGame\\logs\\latest.log", "nested synthetic log");
        string nestedHash = Hash(nestedKeep);
        object nested = Clean(clean, new[] { FixturePath(root, "NestedCase"), FixturePath(root, "NestedCase\\logs\\OtherGame") }, null);
        Require(198, Value(nested, "DeletedFiles") == 1 && !File.Exists(nestedLog) && File.Exists(nestedKeep) && Hash(nestedKeep) == nestedHash,
            "A nested registered game's protected content was removed");

        string canceled = FixturePath(root, "Cancellation");
        WriteFixture(root, "Cancellation\\first.log", "first synthetic log");
        WriteFixture(root, "Cancellation\\second.log", "second synthetic log");
        Func<bool> stop = () => Directory.GetFiles(canceled, "*.log").Length < 2;
        Require(205, Failure(() => Clean(clean, new[] { canceled }, stop)) is OperationCanceledException &&
            Directory.GetFiles(canceled, "*.log").Length == 1, "Cancellation did not stop before the next deletion");
        Require(207, Failure(() => Clean(clean, new[] { canceled }, () => true)) is OperationCanceledException &&
            Directory.GetFiles(canceled, "*.log").Length == 1, "Immediate cancellation changed a synthetic file");
        object missing = Clean(clean, new[] { "", FixturePath(root, "MissingGame") }, null);
        Require(210, Value(missing, "DeletedFiles") == 0 && Value(missing, "SkippedItems") == 2, "Invalid and missing roots were not skipped");

        // Remove each test junction non-recursively, then verify its target and every protected hash.
        foreach (string[] pair in Junctions(root)) {
            Require(214, (File.GetAttributes(pair[0]) & FileAttributes.ReparsePoint) != 0, "A test junction disappeared or changed type");
            Directory.Delete(pair[0], false);
            Require(216, !Directory.Exists(pair[0]) && Directory.Exists(pair[1]), "Junction removal changed its synthetic target");
            VerifyKept(hashes);
        }
        Require(219, File.Exists(nestedKeep) && Hash(nestedKeep) == nestedHash, "Nested game content changed after junction removal");
    }

    public static void Cleanup(string root) {
        root = ValidateRoot(root);
        if (!Directory.Exists(root)) return;
        string prefix = root + "\\";
        Stack<Tuple<string, bool>> pending = new Stack<Tuple<string, bool>>();
        pending.Push(Tuple.Create(root, false));
        while (pending.Count > 0) {
            Tuple<string, bool> entry = pending.Pop();
            string path = Path.GetFullPath(entry.Item1);
            ValidateRoot(root);
            Require(232, String.Equals(path, root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase),
                "Unsafe fixture cleanup path");
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0) {
                if ((attributes & FileAttributes.Directory) != 0) Directory.Delete(path, false); else File.Delete(path);
            } else if ((attributes & FileAttributes.Directory) == 0) {
                File.Delete(path);
            } else if (entry.Item2) {
                Directory.Delete(path, false);
            } else {
                string[] children = Directory.GetFileSystemEntries(path);
                pending.Push(Tuple.Create(path, true));
                foreach (string child in children) pending.Push(Tuple.Create(child, false));
            }
        }
        Require(247, !Directory.Exists(root), "Toolbox fixture cleanup did not finish");
    }
}