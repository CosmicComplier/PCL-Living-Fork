using System;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public class MinecraftOptiFineEntryData {
    public string DisplayName = null!, FileName = null!, InstanceName = null!;
    public bool IsPreview;
    private string inherit = null!;
    public string Inherit { get => inherit; set { if (value.EndsWithF(".0")) value = Strings.Left(value, value.Length - 2); inherit = value; } }
    public string ReleaseTime = null!;
    public string? RequiredForgeVersion;
}
public abstract class MinecraftForgelikeEntryData : IComparable<MinecraftForgelikeEntryData> {
    public bool IsNeoForge;
    public abstract string LoaderName { get; }
    public abstract string FileExtension { get; }
    public Version Version = null!;
    public string VersionName = null!, Inherit = null!;
    public int CompareTo(MinecraftForgelikeEntryData? other) => Version != other!.Version ? Version.CompareTo(other.Version) : McUtils.CompareVersion(VersionName, other.VersionName);
}
public interface IMinecraftForgeVersionFields {
    string ReleaseTime { get; set; }
    string? Hash { get; set; }
    bool IsRecommended { get; set; }
    string Category { get; set; }
    string FileVersion { get; set; }
}
public interface IMinecraftNeoForgeFields {
    bool IsBeta { get; set; }
    string ApiName { get; set; }
}
public class MinecraftLiteLoaderEntryData {
    public string FileName = null!;
    public bool IsPreview;
    public string Inherit = null!;
    public bool IsLegacy;
    public string ReleaseTime = null!, MD5 = null!;
    public JToken JsonToken = null!;
}
public static class MinecraftDownloadEntries {
    public const string ForgeLoaderName = "Forge", NeoForgeLoaderName = "NeoForge", JarExtension = "jar";
    // Fixed CE ModDownload.cs constructor shape is reused selectively; exact LF
    // branch rules and NeoForge formatting are retained under the LF LICENCE.
    // CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Plain Craft Launcher 2/LICENCE.
    public static void InitializeForge<T>(T target, string version, string? branch, string inherit) where T : MinecraftForgelikeEntryData, IMinecraftForgeVersionFields {
        if (version == "11.15.1.2318" || version == "11.15.1.1902" || version == "11.15.1.1890") branch = "1.8.9";
        if (branch is null && inherit == "1.7.10" && Conversions.ToDouble(version.Split('.')[3]) >= 1300) branch = "1.7.10";
        target.IsNeoForge = false; target.VersionName = version; target.Version = new Version(version); target.Inherit = inherit;
        target.FileVersion = version + (branch is null ? "" : "-" + branch);
    }
    public static string ForgeFileExtension(string? category) => category == "installer" ? "jar" : "zip";
    public static void InitializeNeoForge<T>(T target, string apiName) where T : MinecraftForgelikeEntryData, IMinecraftNeoForgeFields {
        target.IsNeoForge = true; target.ApiName = apiName; target.IsBeta = apiName.ContainsIgnoreCase("beta") || apiName.ContainsIgnoreCase("alpha");
        if (apiName.Contains("1.20.1")) {
            target.VersionName = apiName.Replace("1.20.1-", ""); target.Version = new Version("19." + target.VersionName); target.Inherit = "1.20.1";
        } else if (apiName.StartsWithF("0.")) {
            target.VersionName = apiName; string[] segments = apiName.BeforeFirst("-").Split('.');
            target.Version = new Version(0, 0, Conversions.ToInteger(segments.Last())); target.Inherit = segments[1];
        } else {
            target.VersionName = apiName; target.Version = new Version(apiName.BeforeFirst("-"));
            target.Inherit = target.Version.Major >= 24 ? $"{target.Version.Major}.{target.Version.Minor}.{target.Version.Build}" : $"1.{target.Version.Major}.{target.Version.Minor}";
            if (target.Inherit.EndsWithF(".0")) target.Inherit = Strings.Left(target.Inherit, target.Inherit.Length - 2);
            if (target.VersionName.Contains("+")) target.Inherit += "-" + target.VersionName.AfterFirst("+");
        }
    }
    public static string NeoForgeUrlBase(MinecraftForgelikeEntryData target, string apiName) {
        string package = target.Inherit == "1.20.1" ? "forge" : "neoforge";
        return $"https://maven.neoforged.net/releases/net/neoforged/{package}/{apiName}/{package}-{apiName}";
    }
}
// Native models use the same actual constructor/rules as the legacy typed adapters.
public sealed class MinecraftForgeVersionEntry : MinecraftForgelikeEntryData, IMinecraftForgeVersionFields {
    public string ReleaseTime { get; set; } = null!;
    public string? Hash { get; set; }
    public bool IsRecommended { get; set; }
    public string Category { get; set; } = null!;
    public string FileVersion { get; set; } = null!;
    public override string LoaderName => MinecraftDownloadEntries.ForgeLoaderName;
    public override string FileExtension => MinecraftDownloadEntries.ForgeFileExtension(Category);
    public MinecraftForgeVersionEntry(string version, string? branch, string inherit) => MinecraftDownloadEntries.InitializeForge(this, version, branch, inherit);
}
public sealed class MinecraftNeoForgeEntry : MinecraftForgelikeEntryData, IMinecraftNeoForgeFields {
    public bool IsBeta { get; set; }
    public string ApiName { get; set; } = null!;
    public override string LoaderName => MinecraftDownloadEntries.NeoForgeLoaderName;
    public override string FileExtension => MinecraftDownloadEntries.JarExtension;
    public string UrlBase => MinecraftDownloadEntries.NeoForgeUrlBase(this, ApiName);
    public MinecraftNeoForgeEntry(string apiName) => MinecraftDownloadEntries.InitializeNeoForge(this, apiName);
}
