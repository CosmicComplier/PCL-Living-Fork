using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Shell;

public sealed class MainHelpHost<TEvent> {
    public Func<string, Func<string, string>, string> ArgumentReplace = null!;
    public Func<string> BasePath = null!, TempPath = null!, ImagePath = null!;
    public Func<int> VersionCode = null!, HelpVersion = null!;
    public Action<int> SetHelpVersion = null!;
    public Action<string, string> ExtractResources = null!;
    public TEvent PopupEvent = default!;
    public Func<MyListItemView> CreateRow = null!;
    public Action<MyListItemView> ClearCustomEvent = null!;
    public Action<MyListItemView, Action<object>> ConnectClick = null!;
    public Action<object> OpenEntry = null!;
}

public class MainHelpEntryData<TEvent> {
    public string RawPath;
    public string? Title, Desc, Search;
    public List<string> Types = null!;
    public string? Logo;
    public bool ShowInSearch = true, ShowInPublic = true, ShowInSnapshot = true;
    public bool IsEvent;
    public TEvent EventType = default!;
    public string? EventData, XamlContent;
    private readonly MainHelpHost<TEvent> host;

    public MainHelpEntryData(string filePath, MainHelpHost<TEvent> host) {
        this.host = host;
        RawPath = filePath;
        if (!FileUtils.Exists(filePath)) throw new FileNotFoundException("未找到帮助文件：" + filePath, filePath);
        JObject json = (JObject)host.ArgumentReplace(FileUtils.ReadAsString(filePath), StringUtils.XmlEscape).DeserializeJson()!;
        if (json["Title"] is not null) Title = (string?)json["Title"];
        else throw new ArgumentException("未找到 Title 项");
        Desc = (string?)(json["Description"] ?? "");
        Search = (string?)(json["Keywords"] ?? "");
        Logo = (string?)json["Logo"];
        ShowInSearch = (bool)(json["ShowInSearch"] ?? ShowInSearch);
        ShowInPublic = (bool)(json["ShowInPublic"] ?? ShowInPublic);
        ShowInSnapshot = (bool)(json["ShowInSnapshot"] ?? ShowInSnapshot);
        Types = new List<string>();
        foreach (JToken type in json["Types"] ?? new JArray()) Types.Add(((string?)type)!);
        if ((bool)(json["IsEvent"] ?? false)) {
            EventType = json["EventType"]!.ToObject<TEvent>()!;
            EventData = (string?)(json["EventData"] ?? "");
            IsEvent = true;
        } else {
            string xamlAddress = Path.ChangeExtension(filePath, ".xaml");
            if (!FileUtils.Exists(xamlAddress)) throw new FileNotFoundException("未找到帮助条目 .json 对应的 .xaml 文件（" + xamlAddress + "）");
            XamlContent = FileUtils.ReadAsString(xamlAddress);
            IsEvent = false;
        }
    }

    public MyListItemView ToListItem() => SetToListItem(host.CreateRow());
    public MyListItemView SetToListItem(MyListItemView item) {
        string logo = IsEvent
            ? (EqualityComparer<TEvent>.Default.Equals(EventType, host.PopupEvent) ? "Blocks/GrassPath.png" : "Blocks/CommandBlock.png")
            : "Blocks/Grass.png";
        logo = host.ImagePath() + logo;
        item.SnapsToDevicePixels = true; item.Title = Title!; item.Info = Desc!; item.Logo = Logo ?? logo;
        item.Height = 42; item.Type = MyListItemView.CheckType.Clickable; item.Tag = this;
        host.ClearCustomEvent(item);
        host.ConnectClick(item, sender => host.OpenEntry(((FrameworkElement)sender).Tag));
        return item;
    }
}

public sealed class MainHelpRuntime<TEntry, TEvent> where TEntry : MainHelpEntryData<TEvent> {
    private readonly MainHelpHost<TEvent> host;
    private readonly Func<string, TEntry> create;
    private readonly object loadGate = new();
    public MainHelpRuntime(MainHelpHost<TEvent> host, Func<string, TEntry> create) { this.host = host; this.create = create; }

    public void Load(Func<bool> isCanceled, Action<List<TEntry>> setOutput) {
        lock (loadGate) {
            try {
                TryExtract();
                List<string> files = new();
                try {
                    List<string> ignoreList = new();
                    if (DirectoryUtils.Exists(host.BasePath() + "PCL\\Help\\")) {
                        foreach (string file in DirectoryUtils.EnumerateFiles(host.BasePath() + "PCL\\Help\\", true)) {
                            switch (PathUtils.GetExtension(file)) {
                                case "helpignore":
                                    Logger.Info($"发现 .helpignore 文件：{file}");
                                    foreach (string line in FileUtils.ReadAsLines(file, true)) {
                                        string real = line.BeforeFirst("#").Trim();
                                        if (string.IsNullOrWhiteSpace(real)) continue;
                                        ignoreList.Add(real);
                                        Logger.Trace($"> {real}");
                                    }
                                    break;
                                case "json": files.Add(file); break;
                            }
                        }
                    }
                    Logger.Info($"已扫描 PCL 文件夹下的帮助文件，目前总计 {files.Count} 条");
                    foreach (string file in DirectoryUtils.EnumerateFiles(host.TempPath() + "Help", true)) {
                        if (PathUtils.RemoveLastPart(file).Substring((host.TempPath() + "Help").Length).Contains("\\.") || PathUtils.GetExtension(file) != "json") continue;
                        string realPath = file.Substring((host.TempPath() + "Help\\").Length);
                        bool ignored = false;
                        foreach (string ignore in ignoreList) {
                            if (!realPath.RegexCheck(ignore)) continue;
                            Logger.Trace($"已忽略 {realPath}：{ignore}");
                            ignored = true; break;
                        }
                        if (!ignored) files.Add(file);
                    }
                    Logger.Info($"已扫描缓存文件夹下的帮助文件，目前总计 {files.Count} 条");
                } catch (Exception ex) { Logger.Error(ex, "检查帮助文件夹失败", LogBehavior.Alert); }
                if (isCanceled()) return;
                List<TEntry> entries = new();
                foreach (string path in files) {
                    try {
                        TEntry entry = create(path);
                        entries.Add(entry);
                        Logger.Trace($"已加载的帮助条目：{entry.Title} ← {path}");
                    } catch (Exception ex) { Logger.Error(ex, $"初始化帮助条目失败（{path}）", LogBehavior.Alert); }
                }
                if (!entries.Any()) throw new Exception("未找到可用的帮助；若不需要帮助页面，可以在 设置 → 个性化 → 功能隐藏 中将其隐藏");
                if (isCanceled()) return;
                setOutput(entries);
            } catch (Exception ex) { Logger.Warn(ex, "帮助列表初始化失败"); throw; }
        }
    }

    public void TryExtract() {
        if (host.HelpVersion() == host.VersionCode() && FileUtils.Exists(host.TempPath() + "Help\\启动器\\备份设置.xaml")) return;
        DirectoryUtils.Delete(host.TempPath() + "Help");
        host.ExtractResources(host.TempPath() + "Cache\\Help.zip", "Help");
        FileUtils.ExtractToDirectory(host.TempPath() + "Cache\\Help.zip", host.TempPath() + "Help");
        host.SetHelpVersion(host.VersionCode());
        Logger.Info($"已解压内置帮助文件，目前状态：{FileUtils.Exists(host.TempPath() + "Help\\启动器\\备份设置.xaml")}");
    }
}
