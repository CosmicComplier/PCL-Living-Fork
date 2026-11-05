using System;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using PCLCS;

namespace PCLLF.Presentation.Shell;

/// <summary>Live, typed projections of LF's existing settings/models, not a model snapshot.</summary>
public sealed class MainArgumentHost {
    public Func<string?> VersionName = null!, VersionBranch = null!, BuildTypeDisplay = null!, BuildType = null!, Identify = null!;
    public Func<int> VersionCode = null!;
    public Func<string> BasePath = null!, ExePath = null!, TempPath = null!, RegistryFolder = null!;
    public Func<string?> JavaFolder = null!, MinecraftFolder = null!;
    public Func<bool> InstanceLoaded = null!;
    public Func<string?> InstancePath = null!, InstanceIndie = null!, InstanceName = null!, VanillaName = null!;
    public Func<ModLoaders> InstanceLoaders = null!;
    public Func<ModLoaders, string?> LoaderVersion = null!;
    public Func<bool> LoginFinished = null!;
    public Func<string?> LoginName = null!, LoginUuid = null!;
    public Func<int> LoginType = null!;
    public int LegacyLogin, MsLogin = 5, NideLogin = 2, AuthLogin = 3;
    public Func<string> RandomHint = null!, RandomCave = null!;
    public Func<string, object?> Setting = null!;
}

public sealed class MainArgumentRuntime {
    private readonly MainArgumentHost host;
    private readonly object replaceGate = new();
    private Func<string?, string>? replacer;
    public MainArgumentRuntime(MainArgumentHost host) { this.host = host; }

    public string? Replace(string? text, Func<string, string>? escapeHandler = null, bool replaceTime = true) {
        if (text is null || !text.Contains("{")) return text;
        // LF's Static local captures the escape handler of the FIRST qualifying call.
        // Do not silently change this to a fresh per-call replacer during an equivalent migration.
        lock (replaceGate) {
            replacer ??= value => {
                if (value is null) return "";
                if (escapeHandler is null) return value;
                if (value.Contains(":\\")) {
                    bool folder = value.EndsWithF("\\") || value.EndsWithF("/");
                    value = PathUtils.ToShortPath(value);
                    if (folder) value = PathUtils.AddSlashSuffix(value);
                }
                return escapeHandler(value);
            };
        }
        Func<string?, string> replace = replacer;
        text = text.Replace("{pcl_version}", replace(host.VersionName()));
        text = text.Replace("{pcl_version_code}", replace(Conversions.ToString(host.VersionCode())));
        text = text.Replace("{pcl_version_branch}", replace(host.BuildTypeDisplay()));
        text = text.Replace("{pcl_build_type}", replace(host.BuildType()));
        text = text.Replace("{pcl_branch}", replace(host.VersionBranch()));
        text = text.Replace("{identify}", replace(host.Identify()));
        text = text.Replace("{path}", replace(host.BasePath()));
        text = text.Replace("{path_with_name}", replace(host.ExePath()));
        text = text.Replace("{path_temp}", replace(host.TempPath()));
        text = text.Replace("{pcl_md5}", () => replace(CryptographyUtils.ComputeFileHash(host.ExePath(), CryptographyUtils.HashMethod.Md5)));
        text = text.Replace("{pcl_sha1}", () => replace(CryptographyUtils.ComputeFileHash(host.ExePath(), CryptographyUtils.HashMethod.Sha1)));
        if (replaceTime) {
            text = text.Replace("{date}", replace(DateTime.Now.ToString("yyyy'/'M'/'d")));
            text = text.Replace("{time}", replace(DateTime.Now.ToString("HH':'mm':'ss")));
        }
        text = text.Replace("{java}", replace(host.JavaFolder()));
        text = text.Replace("{minecraft}", replace(host.MinecraftFolder()));
        if (host.InstanceLoaded()) {
            text = text.Replace("{version_path}", replace(host.InstancePath())); text = text.Replace("{verpath}", replace(host.InstancePath()));
            text = text.Replace("{version_indie}", replace(host.InstanceIndie())); text = text.Replace("{verindie}", replace(host.InstanceIndie()));
            text = text.Replace("{name}", replace(host.InstanceName()));
            text = text.Replace("{version}", replace(new[] { "unknown", "old", "pending" }.Contains(host.VanillaName()!.Lower()) ? host.InstanceName() : host.VanillaName()));
            text = text.Replace("{forge}", replace(host.InstanceLoaders().HasFlagF(ModLoaders.Forge) ? host.LoaderVersion(ModLoaders.Forge) : ""));
            text = text.Replace("{fabric}", replace(host.InstanceLoaders().HasFlagF(ModLoaders.Fabric) ? host.LoaderVersion(ModLoaders.Fabric) : ""));
            text = text.Replace("{neoforge}", replace(host.InstanceLoaders().HasFlagF(ModLoaders.NeoForge) ? host.LoaderVersion(ModLoaders.NeoForge) : ""));
            text = text.Replace("{liteloader}", replace(host.InstanceLoaders().HasFlagF(ModLoaders.LiteLoader) ? "true" : ""));
        } else {
            text = text.Replace("{version_path}", replace(null)); text = text.Replace("{verpath}", replace(null));
            text = text.Replace("{version_indie}", replace(null)); text = text.Replace("{verindie}", replace(null));
            text = text.Replace("{name}", replace(null)); text = text.Replace("{version}", replace(null));
            text = text.Replace("{forge}", replace(null)); text = text.Replace("{fabric}", replace(null));
            text = text.Replace("{neoforge}", replace(null)); text = text.Replace("{liteloader}", replace(null));
        }
        if (host.LoginFinished()) {
            text = text.Replace("{user}", replace(host.LoginName()));
            text = text.Replace("{uuid}", replace(host.LoginUuid()?.Lower()));
            int login = host.LoginType();
            if (login == host.LegacyLogin) text = text.Replace("{login}", replace("离线"));
            else if (login == host.MsLogin) text = text.Replace("{login}", replace("正版"));
            else if (login == host.NideLogin) text = text.Replace("{login}", replace("统一通行证"));
            else if (login == host.AuthLogin) text = text.Replace("{login}", replace("Authlib-Injector"));
        } else {
            text = text.Replace("{user}", replace(null)); text = text.Replace("{uuid}", replace(null)); text = text.Replace("{login}", replace(null));
        }
        text = text.RegexReplace("\\{hint\\}", _ => replace(host.RandomHint()));
        text = text.RegexReplace("\\{cave\\}", _ => replace(host.RandomCave()));
        text = text.RegexReplace("\\{setup:([a-zA-Z0-9]+)\\}", match => replace(Conversions.ToString(host.Setting(match.Groups[1].Value))));
        text = text.RegexReplace("\\{varible:([^:\\}]+)(?::([^\\}]+))?\\}", match => replace(Conversions.ToString(
            RegistryUtils.TryRead($"HKEY_CURRENT_USER\\Software\\{host.RegistryFolder()}", $"CustomEvent{match.Groups[1].Value}", match.Groups[2].Value))));
        text = text.RegexReplace("\\{variable:([^:\\}]+)(?::([^\\}]+))?\\}", match => replace(Conversions.ToString(
            RegistryUtils.TryRead($"HKEY_CURRENT_USER\\Software\\{host.RegistryFolder()}", $"CustomEvent{match.Groups[1].Value}", match.Groups[2].Value))));
        return text;
    }
}
