using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;

// Pure static credential/bounded-file checks only. This host does not construct
// PCL.Application, start Main, load account settings or access a user profile.
internal static class Program
{
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly string[] Variables = ["PCL_CURSEFORGE_API_KEY", "PCL_MS_CLIENT_ID"];
    private static int checks;
    private static int Main(string[] args)
    {
        try
        {
            string? main = null, native = null, synthetic = null;
            bool keyless = false;
            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--main": main = Value(); break;
                    case "--native": native = Value(); break;
                    case "--synthetic-factory": synthetic = Value(); break;
                    case "--keyless": keyless = true; break;
                    default: throw new ArgumentException("Unknown credential check option.");
                }
                string Value() => ++index < args.Length ? Path.GetFullPath(args[index]) : throw new ArgumentException("Missing credential check argument.");
            }
            if (synthetic is not null)
            {
                Check(main is null && native is null && !keyless, "Synthetic factory mode cannot claim product or keyless acceptance.");
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(synthetic);
                Check(assembly.GetName().Name != "LivingPCL", "Synthetic factory cannot use the product assembly identity.");
                Exercise(assembly, [synthetic], false);
                Console.WriteLine("PASS synthetic CSharp credential factory/source contract; no Main/bundle/entry acceptance implied. Checks=" + checks);
            }
            else
            {
                Check(main is not null && native is not null, "Actual Main DLL and actual native bundle are both required; a bridge cannot replace Main.");
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(main!);
                // The real bounded reader checks NET10 target/runtime metadata;
                // byte equality below binds that result to this loaded module.
                // Do not instantiate assembly-wide WPF attributes in a pure host.
                Check(assembly.GetName().Name == "LivingPCL", "Expected the actual LivingPCL Main module.");
                BindBundle(assembly, main!, native!);
                Exercise(assembly, [main!, native!], keyless);
                Console.WriteLine("PASS actual Native Main credentials: exact bundle/Main module SHA, embedded readback, full-file plaintext/Base64 absence, runtime override and authenticated Decode/buffer cleanup. No Application/startup/profile/network executed. Checks=" + checks);
            }
            return 0;
        }
        catch (Exception error)
        {
            // Inputs and credential exception values are never emitted.
            Console.Error.WriteLine("FAIL credential protection after checks=" + checks + ": " + error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
    private static object? Call(MethodInfo method, params object?[] args)
    {
        try { return method.Invoke(null, args); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, Flags) ?? throw new MissingMethodException("Actual credential method is missing: " + name);
    private static void BindBundle(Assembly main, string mainPath, string nativePath)
    {
        // Use the actual production bounded reader, not FileVersionInfo of a
        // native shell and never Assembly.LoadFrom(native/legacy-bridge.exe).
        Type reader = main.GetType("PCL.ModernBundleIdentity", true)!;
        object identity = Call(Method(reader, "Read"), nativePath)!;
        object? Get(string name) => identity.GetType().GetProperty(name)!.GetValue(identity);
        Check((string?)Get("AssemblyName") == "LivingPCL" && (string?)Get("Product") == "PCL Living Fork", "Bundle did not identify the genuine LF product.");
        long offset = (long)Get("EntryOffset")!, length = (long)Get("EntrySize")!;
        Check(length == new FileInfo(mainPath).Length && length > 0 && length <= 64L * 1024 * 1024, "Native embedded Main length differs from the tested module.");
        using FileStream input = File.OpenRead(nativePath);
        input.Position = offset;
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[128 * 1024];
        for (long remaining = length; remaining > 0;)
        {
            int count = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (count == 0) throw new EndOfStreamException("Bound native Main entry ended early.");
            hash.AppendData(buffer, 0, count); remaining -= count;
        }
        using FileStream tested = File.OpenRead(mainPath);
        Check(hash.GetHashAndReset().AsSpan().SequenceEqual(SHA256.HashData(tested)), "Native bundle contains a different Main module; no credential result may be attributed to it.");
    }
    private static void Exercise(Assembly assembly, string[] binaries, bool keyless)
    {
        Type type = assembly.GetType("PCL.BuildCredentials", true)!;
        MethodInfo cf = Method(type, "ReadCurseForgeKey"), ms = Method(type, "ReadMicrosoftClientId"), decode = Method(type, "Decode");
        string?[] saved = Variables.Select(Environment.GetEnvironmentVariable).ToArray();
        try
        {
            foreach (string name in Variables) Environment.SetEnvironmentVariable(name, null);
            Check((string?)Call(cf) == (keyless ? "" : saved[0] ?? "") && (string?)Call(ms) == (keyless ? "" : saved[1] ?? ""), "Embedded credential readback did not match build inputs.");
            List<byte[]> needles = new();
            foreach (string? value in saved)
                if (!string.IsNullOrEmpty(value))
                {
                    needles.Add(Encoding.UTF8.GetBytes(value)); needles.Add(Encoding.Unicode.GetBytes(value));
                    needles.Add(Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(value))));
                }
            foreach (string binary in binaries) Check(!ContainsAny(binary, needles), "A build input is directly visible in tested binary bytes.");
            Environment.SetEnvironmentVariable(Variables[0], "synthetic-runtime-cf"); Environment.SetEnvironmentVariable(Variables[1], "synthetic-runtime-ms");
            Check((string?)Call(cf) == "synthetic-runtime-cf" && (string?)Call(ms) == "synthetic-runtime-ms", "Runtime credential override failed.");
            foreach (string name in Variables) Environment.SetEnvironmentVariable(name, null);
            DecodeContracts(decode);
        }
        finally { for (int index = 0; index < Variables.Length; index++) Environment.SetEnvironmentVariable(Variables[index], saved[index]); }
    }
    private static bool ContainsAny(string path, List<byte[]> needles)
    {
        if (needles.Count == 0) return false;
        int overlap = needles.Max(value => value.Length) - 1, retained = 0;
        byte[] buffer = new byte[128 * 1024 + overlap];
        using FileStream file = File.OpenRead(path);
        while (true)
        {
            int count = file.Read(buffer, retained, buffer.Length - retained), total = retained + count;
            if (count == 0) return false;
            foreach (byte[] needle in needles) if (buffer.AsSpan(0, total).IndexOf(needle) >= 0) return true;
            retained = Math.Min(overlap, total); buffer.AsSpan(total - retained, retained).CopyTo(buffer);
        }
    }
    private static void DecodeContracts(MethodInfo decode)
    {
        byte[] material = Enumerable.Range(1, 64).Select(value => (byte)value).ToArray();
        byte[] shareA = Enumerable.Range(65, 64).Select(value => (byte)value).ToArray();
        byte[] shareB = shareA.Select((value, index) => (byte)(value ^ material[index])).ToArray();
        try
        {
            byte[] Blob(byte[] plain, PaddingMode padding = PaddingMode.PKCS7)
            {
                using Aes aes = Aes.Create(); aes.Key = material[..32]; aes.IV = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray(); aes.Mode = CipherMode.CBC; aes.Padding = padding;
                using ICryptoTransform encrypt = aes.CreateEncryptor(); byte[] cipher = encrypt.TransformFinalBlock(plain, 0, plain.Length);
                byte[] body = [(byte)1, .. aes.IV, .. cipher];
                using HMACSHA256 mac = new(material[32..]); return [.. body, .. mac.ComputeHash(body)];
            }
            byte[] blob = Blob(Encoding.UTF8.GetBytes("synthetic-authenticated-value"));
            byte[] a = (byte[])shareA.Clone(), b = (byte[])shareB.Clone();
            Check((string?)Call(decode, blob, a, b) == "synthetic-authenticated-value" && Empty(a) && Empty(b), "Decode/successful share cleanup failed.");
            foreach (int location in new[] { 0, 1, 17, blob.Length - 1, -1, -2 })
            {
                byte[] changed = (byte[])blob.Clone(); a = (byte[])shareA.Clone(); b = (byte[])shareB.Clone();
                if (location >= 0) changed[location] ^= 1; else if (location == -1) a[0] ^= 1; else b[0] ^= 1;
                bool rejected = false; try { Call(decode, changed, a, b); } catch (CryptographicException) { rejected = true; }
                Check(rejected && Empty(a) && Empty(b), "Tampered credential material was accepted or failed share cleanup.");
            }
            byte[] empty = Blob([]);
            Check(empty.Length == 65 && (string?)Call(decode, empty, (byte[])shareA.Clone(), (byte[])shareB.Clone()) == "", "Minimum authenticated empty payload failed.");
            foreach (byte[] bad in new[] { Blob([255, 254]), Blob(new byte[16], PaddingMode.None) })
            {
                a = (byte[])shareA.Clone(); b = (byte[])shareB.Clone(); bool rejected = false;
                try { Call(decode, bad, a, b); } catch { rejected = true; }
                Check(rejected && Empty(a) && Empty(b), "Authenticated invalid UTF8/padding was accepted or failed share cleanup.");
            }
        }
        finally { Array.Clear(material); Array.Clear(shareA); Array.Clear(shareB); }
        static bool Empty(byte[] bytes) => bytes.All(value => value == 0);
    }
}
