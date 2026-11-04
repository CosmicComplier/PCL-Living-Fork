using System;
using System.Collections.Generic;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLCS;

public sealed class ResourceProjectListContext {
    public string GameVersion = null!, LoaderFull = null!, LoaderPart = null!;
    public bool ShowGameVersion, ShowLoader;
}
public sealed class ResourceProjectListHost<TRow> where TRow : class {
    public Func<TRow> Create = null!;
    public Action<TRow, object> SetTag = null!;
    public Action<TRow, string> SetLogoSource = null!, SetLogoFallback = null!, SetVersion = null!;
    public Action<TRow, string?> SetTitle = null!;
    public Action<TRow, string> SetSubtitle = null!;
    public Action<TRow> RemoveSubtitle = null!, HideVersion = null!;
    public Action<TRow, List<string>, string> SetContent = null!;
    public Action<TRow, string, string, string> SetFooter = null!;
}
public sealed class ResourceVersionListHost<TRow, TClick, TSave> where TRow : class where TClick : class where TSave : class {
    public Func<TRow> Create = null!;
    public Action<TRow, object, string, string> Configure = null!;
    public Action<TRow, string> SetInfo = null!;
    public Func<TRow, string> Title = null!;
    public Action<TRow, TClick> AttachClick = null!;
    public Action<TRow, TSave> AttachSave = null!;
}

public static class ResourcePresentation {
    public static ResourceProjectListContext ProjectContext(ResourceProjectModelData project, bool showMcVersionDesc, bool showLoaderDesc) {
        string gameVersionDescription;
        if (project.Drops is null || !project.Drops.Any()) gameVersionDescription = "仅快照版本";
        else {
            List<string> segments = new(); bool old = false;
            for (int index = 0; index < project.Drops.Count; index++) {
                int start = project.Drops[index], end = project.Drops[index];
                if (start < 120) { if (segments.Any() && !old) break; else old = true; }
                for (int next = index + 1; next < project.Drops.Count; next++) {
                    if (project.Services.AllDrops() is null || project.Services.AllDrops()!.IndexOf(project.Drops[next]) != project.Services.AllDrops()!.IndexOf(end) + 1) break;
                    end = project.Drops[next]; index = next;
                }
                string startName = McVersion.DropToVersion(start), endName = McVersion.DropToVersion(end);
                if (start == end) segments.Add(startName);
                else if (project.Services.AllDrops() is not null && start >= project.Services.AllDrops()!.First()) {
                    if (end < 120) { segments.Clear(); segments.Add("全版本"); break; }
                    else segments.Add(endName + "+");
                } else if (end < 120) { segments.Add(startName + "-"); break; }
                else if (project.Services.AllDrops() is null || project.Services.AllDrops()!.IndexOf(end) - project.Services.AllDrops()!.IndexOf(start) == 1) segments.Add(startName + ", " + endName);
                else segments.Add(startName + "~" + endName);
            }
            gameVersionDescription = string.Join(", ", segments);
        }
        string loaderFull, loaderPart;
        List<ModLoaders> loaders = project.ModLoaders.Flags().ToList();
        switch (loaders.Count) {
            case 0:
                if (loaders.Count == 1) { loaderFull = "仅 " + loaders.Single(); loaderPart = loaders.Single().ToString(); }
                else { loaderFull = "未知"; loaderPart = ""; }
                break;
            case 1: loaderFull = "仅 " + loaders.Single(); loaderPart = loaders.Single().ToString(); break;
            default:
                int newest = project.Drops!.Any() ? project.Drops!.First() : 9999;
                if (loaders.Contains(ModLoaders.Forge) && (newest < 140 || loaders.Contains(ModLoaders.Fabric)) && (newest < 200 || loaders.Contains(ModLoaders.NeoForge))) {
                    loaderFull = "任意"; loaderPart = "";
                } else loaderFull = loaderPart = string.Join(" / ", loaders);
                break;
        }
        return new ResourceProjectListContext { GameVersion = gameVersionDescription, LoaderFull = loaderFull, LoaderPart = loaderPart, ShowGameVersion = showMcVersionDesc, ShowLoader = showLoaderDesc };
    }

    public static void ApplyLogo(ResourceProjectModelData project, Action<string> source, Action<string> fallback) {
        if (string.IsNullOrEmpty(project.LogoUrl)) source(project.Services.ImageRoot() + "Icons/NoIcon.png");
        else { source(project.LogoUrl!); fallback(project.Services.MirrorUrl(project.LogoUrl!)); }
    }
    public static TRow ProjectRow<TRow>(ResourceProjectModelData project, ResourceProjectListContext context, ResourceProjectListHost<TRow> host) where TRow : class {
        TRow row = host.Create(); host.SetTag(row, project);
        ApplyLogo(project, value => host.SetLogoSource(row, value), value => host.SetLogoFallback(row, value));
        (string? title, string subtitle) = project.GetControlTitle(true);
        host.SetTitle(row, title);
        if (subtitle == "") host.RemoveSubtitle(row); else host.SetSubtitle(row, subtitle);
        host.SetContent(row, project.Tags, StringExtensions.ReplaceLineEndings(project.Description!, ""));
        if (!context.ShowGameVersion && !context.ShowLoader) host.HideVersion(row);
        else if (context.ShowGameVersion && context.ShowLoader) host.SetVersion(row, (context.LoaderPart == "" ? "" : context.LoaderPart + " ") + context.GameVersion);
        else host.SetVersion(row, context.ShowGameVersion ? context.GameVersion : context.LoaderFull);
        string download = project.DownloadCount > 100000000 ? Math.Round(project.DownloadCount / 100000000d, 2) + " 亿" :
            project.DownloadCount > 100000 ? Math.Floor(project.DownloadCount / 10000d) + " 万" : project.DownloadCount.ToString();
        host.SetFooter(row, project.Platform.ToString(), StringUtils.FormatTimeSpan(project.LastUpdate - DateTime.Now, true), download);
        return row;
    }
    public static TRow VersionRow<TRow, TClick, TSave>(ResourceVersionModelData version, TClick clickHandler, TSave? saveHandler, bool badDisplay,
        ResourceVersionListHost<TRow, TClick, TSave> host) where TRow : class where TClick : class where TSave : class {
        TRow row = host.Create();
        string title = (badDisplay ? version.FileName : version.Display)!.BeforeLastOfAny(new[] { ".zip", ".jar", ".mrpack", ".litemod" }, true);
        // Match LF's order: title exists before the description observes it.
        string logo = version.Services.ImageRoot() + "ReleaseTypes/" + ((ResourceReleaseTypes)version.ReleaseTypeCode) + ".png";
        host.Configure(row, version, title, logo);
        host.AttachClick(row, clickHandler);
        List<string> info = new();
        if (version.ModLoaders != ModLoaders.None) info.Add(string.Join("、", version.ModLoaders.Flags()));
        if (host.Title(row) != PathUtils.GetFileNameWithoutExtension(version.FileName!)) info.Add(PathUtils.GetFileNameWithoutExtension(version.FileName!));
        if (version.Dependencies.Any()) info.Add($"{version.Dependencies.Count} 项前置");
        if (version.GameVersions.All(value => !value.Contains(".") || new[] { "w", "snapshot", "rc", "pre", "experimental", "-" }.Any(value.ContainsIgnoreCase))) info.Add("游戏版本 " + string.Join("、", version.GameVersions));
        if (version.DownloadCount > 0) info.Add("下载 " + (version.DownloadCount > 100000 ? Math.Round(version.DownloadCount / 10000d) + " 万" : version.DownloadCount.ToString() + " ") + "次");
        info.Add("更新于 " + StringUtils.FormatTimeSpan(version.ReleaseDate - DateTime.Now, false));
        if (version.ReleaseTypeCode != 1) info.Add(version.ReleaseTypeDisplay);
        host.SetInfo(row, string.Join("，", info));
        if (saveHandler is not null) host.AttachSave(row, saveHandler);
        return row;
    }
}
