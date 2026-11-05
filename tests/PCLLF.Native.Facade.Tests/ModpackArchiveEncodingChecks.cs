using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PCLLF.Native.Facade.Tests;
// Main's actual
// ReadPackage/Manifest/Plan run, but Commit, downloads and real-user I/O never run.
internal static class ModpackArchiveEncodingChecks
{
    [DllImport("kernel32.dll", ExactSpelling = true)] private static extern uint GetACP();
    internal static void Run(string root)
    {
        string owned = Path.Combine(root, "legacy-archive-names"); Directory.CreateDirectory(owned);
        int codePage = checked((int)GetACP());
        Encoding acp = CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.UTF8;
        // Chinese on the user's ACP936 Windows; CI's ACP1252 cannot represent it,
        // so choose another non-ASCII ACP character there. UTF8 Chinese is also tested.
        string label = new[] { "中文", "café", "Ж", "あ", "Ω", "가" }.FirstOrDefault(s => acp.GetString(acp.GetBytes(s)) == s) ?? "ascii";
        string relative = "config/" + label + ".cfg";
        string oldZip = Pack(owned, "old", relative, "old config", Encoding.UTF8);
        string nextZip = Pack(owned, "next", relative, "new config", acp);
        if (codePage != 65001) Require(NoUtf8Flags(nextZip), "Legacy fixture unexpectedly has EFS set.");
        object oldPackage = ModpackUpdateChecks.Read(oldZip, Path.Combine(owned, "old-stage"));
        JObject baseline = (JObject)ModpackUpdateChecks.Update("Manifest", oldPackage)!;
        string game = Path.Combine(owned, "game"); string existing = Path.Combine(game, relative);
        ModpackUpdateChecks.Put(existing, "old config");
        object incoming = ModpackUpdateChecks.Read(nextZip, Path.Combine(owned, "next-stage"));
        IDictionary files = (IDictionary)Native.Get(incoming, "Files")!;
        Require(files.Count == 1 && files.Contains(relative), "Main ReadPackage changed an ACP archive filename.");
        object plan = ModpackUpdateChecks.Update("Plan", game, baseline, incoming)!;
        var changes = ModpackUpdateChecks.Changes(plan);
        Require(changes.Count == 1 && ModpackUpdateChecks.Text(changes[0], "Relative") == relative && Native.Get(changes[0], "Incoming") is not null && !ModpackUpdateChecks.Conflict(changes[0]), "Main Plan split an update into deletion plus a mojibake filename.");
        Require(File.ReadAllText(existing) == "old config", "Planning modified original configuration bytes.");
        // Unchanged legacy package must produce no changes, including no deletion.
        string unchangedZip = Pack(owned, "unchanged", relative, "old config", acp);
        object unchanged = ModpackUpdateChecks.Read(unchangedZip, Path.Combine(owned, "unchanged-stage"));
        Require(ModpackUpdateChecks.Changes(ModpackUpdateChecks.Update("Plan", game, baseline, unchanged)!).Count == 0, "Unchanged ACP package scheduled a deletion or renamed file.");
        string utf8 = Pack(owned, "utf8", "config/中文.cfg", "utf8 config", Encoding.UTF8);
        object utf8Package = ModpackUpdateChecks.Read(utf8, Path.Combine(owned, "utf8-stage"));
        Require(((IDictionary)Native.Get(utf8Package, "Files")!).Contains("config/中文.cfg"), "EFS UTF8 Chinese entry changed under explicit ACP decoding.");
        Console.WriteLine("PASS Main modpack legacy ZIP names: ACP=" + codePage + ", legacy=" + label + ", UTF8 Chinese, unchanged/updated plans, no Commit.");
    }
    private static string Pack(string folder, string name, string relative, string content, Encoding encoding)
    {
        string path = Path.Combine(folder, name + ".mrpack");
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create, encoding);
        JObject manifest = new() { ["formatVersion"] = 1, ["game"] = "minecraft", ["versionId"] = name, ["name"] = "Synthetic encoding pack", ["dependencies"] = new JObject { ["minecraft"] = "1.20.1" }, ["files"] = new JArray() };
        ModpackUpdateChecks.ZipText(archive, "modrinth.index.json", manifest.ToString());
        ModpackUpdateChecks.ZipText(archive, "overrides/" + relative, content);
        return path;
    }
    private static bool NoUtf8Flags(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int eocd = bytes.Length - 22;
        Require(BitConverter.ToUInt32(bytes, eocd) == 0x06054b50, "Unexpected synthetic ZIP footer.");
        int count = BitConverter.ToUInt16(bytes, eocd + 10), position = checked((int)BitConverter.ToUInt32(bytes, eocd + 16));
        for (int i = 0; i < count; i++)
        {
            Require(BitConverter.ToUInt32(bytes, position) == 0x02014b50, "Unexpected synthetic ZIP directory.");
            if ((BitConverter.ToUInt16(bytes, position + 8) & 0x0800) != 0) return false;
            position += 46 + BitConverter.ToUInt16(bytes, position + 28) + BitConverter.ToUInt16(bytes, position + 30) + BitConverter.ToUInt16(bytes, position + 32);
        }
        return true;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
