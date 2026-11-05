using System.Buffers.Binary;
using System.ComponentModel;

namespace PCLLF.Native.Facade.Tests;

internal static partial class NativeUpdateChecks
{
    private static void Replacements(string root, string source)
    {
        string folder = Path.Combine(root, "replace"); Directory.CreateDirectory(folder);
        string target = Path.Combine(folder, "LivingPCL.exe");
        File.Copy(source, target); File.AppendAllText(target, "synthetic-old-version-overlay");
        string before = Hash(target), expectedHash = Hash(source);
        Reject(() => Call("ReplaceExecutable", source, target, new string('0', 64), false), "Invalid hash update was accepted.");
        Native.Check(Hash(target) == before, "Invalid update damaged the previous executable.");
        Call("ReplaceExecutable", source, target, expectedHash, false);
        Native.Check(Hash(target) == expectedHash && Directory.GetFiles(folder).Length == 1,
            "Actual replacement failed or left temporary files.");
        using (new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Pass("REL-06", "real Main ReplaceExecutable rejects wrong SHA without a write, then replaces old bytes and releases handles/temporary files");

        string naming = Path.Combine(root, "naming");
        string[] names = { "PCL-Living-Fork-1.3.3.exe", "PCL Living Fork - 1.3.3.exe", "LivingPCL2-Re-1.1.2.exe", "My custom launcher.exe" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = Path.Combine(naming, i.ToString(System.Globalization.CultureInfo.InvariantCulture)); Directory.CreateDirectory(path);
            string previous = Path.Combine(path, names[i]); File.Copy(source, previous); File.AppendAllText(previous, "synthetic-old-version");
            File.WriteAllText(previous + ".config", "synthetic-runtime-config");
            Call("ReplaceExecutable", source, previous, expectedHash, false);
            Native.Check(Hash(previous) == expectedHash && File.ReadAllText(previous + ".config") == "synthetic-runtime-config" &&
                Directory.EnumerateFiles(path).Count() == 2,
                "In-place update changed the historical/custom filename, config or temporary-file count: " + names[i]);
        }
        Pass("REL-07", "all four original standard/spaced/historical/custom executable and config preservation assertions");

        string missing = Path.Combine(root, "new-target"); Directory.CreateDirectory(missing);
        string firstInstall = Path.Combine(missing, "first.exe");
        Call("ReplaceExecutable", source, firstInstall, expectedHash, false);
        Native.Check(Hash(firstInstall) == expectedHash && Directory.EnumerateFiles(missing).Count() == 1,
            "Missing target install failed or left update staging.");

        // Force the real post-File.Replace restart failure without executing Main:
        // keep the real managed LF metadata/resources but mark the fixture PE as
        // a DLL. Windows CreateProcess must reject that .exe (193/216). It is not
        // a replacement product/engine; Main's genuine validator checks it first.
        string rollback = Path.Combine(root, "restart-failure"); Directory.CreateDirectory(rollback);
        string candidate = Path.Combine(root, "lf-main-dll-flag.exe");
        byte[] bytes = File.ReadAllBytes(source);
        int pe = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0x3c, 4));
        Native.Check(pe > 0 && pe + 24 < bytes.Length && bytes.AsSpan(pe, 4).SequenceEqual(new byte[] { 0x50, 0x45, 0, 0 }),
            "Actual Main has no valid PE header for the launch-failure fixture.");
        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pe + 22, 2));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(pe + 22, 2), (ushort)(flags | 0x2000));
        File.WriteAllBytes(candidate, bytes);
        string candidateHash = Hash(candidate);
        Call("ValidateExecutable", candidate, candidateHash);
        string old = Path.Combine(rollback, "launcher.exe");
        File.Copy(source, old); File.AppendAllText(old, "previous-version-rollback-sentinel");
        string oldHash = Hash(old);
        File.WriteAllText(old + ".config", "runtime-config-sentinel");
        string data = Path.Combine(rollback, "PCL"); Directory.CreateDirectory(data);
        File.WriteAllText(Path.Combine(data, "Setup.ini"), "fixture-settings-must-survive");
        string game = Path.Combine(rollback, ".minecraft"); Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game, "fixture.txt"), "fixture-world-must-survive");
        Exception failure = Reject(() => Call("ReplaceExecutable", candidate, old, candidateHash, true),
            "Windows unexpectedly launched a PE marked IMAGE_FILE_DLL; refusing to claim rollback.");
        Native.Check(failure is Win32Exception win32 && win32.NativeErrorCode is 193 or 216,
            "The intended post-replace Windows process rejection was not observed: " + failure);
        Native.Check(Hash(old) == oldHash && File.ReadAllText(old + ".config") == "runtime-config-sentinel" &&
            File.ReadAllText(Path.Combine(data, "Setup.ini")) == "fixture-settings-must-survive" &&
            File.ReadAllText(Path.Combine(game, "fixture.txt")) == "fixture-world-must-survive",
            "Actual post-replace restart failure did not restore original executable/settings/game data.");
        Native.Check(Directory.EnumerateFiles(rollback, "*", SearchOption.AllDirectories).Count() == 4 &&
            !Directory.EnumerateFiles(rollback).Any(path => Path.GetFileName(path).Contains(".update", StringComparison.Ordinal)),
            "Actual rollback left temporary/backup files.");
        using (new FileStream(old, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Pass("REL-08", "real File.Replace -> Windows launch rejection -> backup rollback; exact old hash, config/settings/game bytes, no temporary files or held handle");
    }
}
