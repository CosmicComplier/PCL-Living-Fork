using System;
using System.IO;
using System.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLLF.Presentation.Launch;

// LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 PageLaunchLeft.xaml.vb;
// PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 is the source baseline.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Pages/PageLaunch/PageLaunchLeft.xaml.cs
// and PageLaunchRight.xaml.cs were reviewed for C# structure (CE Apache LICENSE /
// PCL LICENCE). CE profiles, localization, custom-home live watchers and revised
// decisions are not imported. LF repository LICENCE applies.
public sealed class LegacyLaunchSkinHost<TTask> {
    public Func<string, string> ReadString = null!;
    public Func<string, int> ReadInteger = null!;
    public Action<string, object> WriteSetting = null!;
    public Func<bool> OfflinePageReloaded = null!;
    public Func<string> OfflineName = null!;
    public Func<TTask, (string, string)> Input = null!;
    public Action<TTask> ResetInput = null!;
    public Action<TTask, string> Output = null!;
    public Func<TTask, bool> Canceled = null!;
    public Action<Action> Dispatch = null!;
    public Action<string> ClearSkin = null!;
    public Func<string, bool> PageExists = null!;
    public Func<string, Action> RenderSkin = null!;
    public Func<string> ImagePath = null!, AppDataPath = null!;
    public Func<string, string> OfflineUuid = null!, SkinSex = null!, DownloadSkin = null!;
    public Func<string, string, string> SkinAddress = null!;
    public Func<string, bool, string> MojangUuid = null!;
    public Action<string> Hint = null!;
}
public sealed class LegacyLaunchSkins<TTask> {
    private readonly LegacyLaunchSkinHost<TTask> host;
    public LegacyLaunchSkins(LegacyLaunchSkinHost<TTask> host) => this.host = host;
    public (string, string) Input(string Kind) => Kind == "Legacy" ? OfflineInput() : (host.ReadString("Cache" + (Kind == "Ms" ? "MsV2" : Kind) + "Name"), host.ReadString("Cache" + (Kind == "Ms" ? "MsV2" : Kind) + "Uuid"));
    private (string, string) OfflineInput() {
        int kind = host.ReadInteger("LaunchSkinType");
        switch (kind) {
            case 0:
                if (host.OfflinePageReloaded()) return ("0", host.OfflineName().Trim() ?? "");
                else if (string.IsNullOrEmpty(host.ReadString("LoginLegacyName"))) return ("0", "");
                else return ("0", host.ReadString("LoginLegacyName").ToString().BeforeFirst("¨") ?? "");
            case 3: return ("3", host.ReadString("LaunchSkinID"));
            default: return (kind.ToString(), "");
        }
    }
    private string DefaultSkin(string user) => host.ImagePath() + "Skins/" + host.SkinSex(host.OfflineUuid(user)) + ".png";
    public void Official(TTask Data, string Kind) {
        host.Dispatch(() => host.ClearSkin(Kind));
        string user = host.Input(Data).Item1, uuid = host.Input(Data).Item2;
        string description = Kind == "Ms" ? "微软正版" : Kind == "Nide" ? "统一通行证" : " Authlib-Injector";
        if (string.IsNullOrEmpty(user)) {
            host.Output(Data, Kind == "Auth" ? host.ImagePath() + "Skins/Steve.png" : DefaultSkin(user));
            Logger.Info("获取" + description + "皮肤失败，ID 为空"); goto Finish;
        }
        try {
            string result = host.SkinAddress(uuid, Kind);
            if (host.Canceled(Data)) throw new OperationCanceledException("当前任务已取消：" + user);
            result = host.DownloadSkin(result);
            if (host.Canceled(Data)) throw new OperationCanceledException("当前任务已取消：" + user);
            host.Output(Data, result);
        } catch (Exception error) {
            if (error.IsCanceled()) { host.Output(Data, ""); return; }
            else if (error.GetDisplay(false).Contains("(429)")) {
                host.Output(Data, Kind == "Ms" ? DefaultSkin(user) : host.ImagePath() + "Skins/Steve.png");
                Logger.Error("获取" + (Kind == "Ms" ? "正版" : description) + $"皮肤失败（{user}）：获取皮肤太过频繁，请 5 分钟后再试！", LogBehavior.Toast);
            } else if (error.GetDisplay(false).Contains("未设置自定义皮肤")) {
                host.Output(Data, Kind == "Ms" ? DefaultSkin(user) : host.ImagePath() + "Skins/Steve.png"); Logger.Info("用户未设置自定义皮肤，跳过皮肤加载");
            } else { host.Output(Data, Kind == "Ms" ? DefaultSkin(user) : host.ImagePath() + "Skins/Steve.png"); Logger.Error(error, "获取" + description + $"皮肤失败（{user}）", LogBehavior.Toast); }
        }
    Finish:
        Finish(Data, Kind);
    }
    private void Finish(TTask Data, string Kind) {
        if (host.PageExists(Kind)) host.Dispatch(host.RenderSkin(Kind));
        else if (!host.Canceled(Data)) host.ResetInput(Data);
    }
    public void Offline(TTask Data) {
        host.Dispatch(() => host.ClearSkin("Legacy"));
        switch (Conversions.ToInteger(host.Input(Data).Item1)) {
            case 0: host.Output(Data, DefaultSkin(host.Input(Data).Item2)); break;
            case 1:
            UseDefault:
                host.Output(Data, host.ImagePath() + "Skins/Steve.png"); break;
            case 2: host.Output(Data, host.ImagePath() + "Skins/Alex.png"); break;
            case 3:
                string id = host.Input(Data).Item2;
                try {
                    if (id.Count() < 2) host.Output(Data, host.ImagePath() + "Skins/Steve.png");
                    else {
                        string result = host.MojangUuid(id, true);
                        if (host.Canceled(Data)) throw new OperationCanceledException("当前任务已取消：" + id);
                        result = host.SkinAddress(result, "Mojang");
                        if (host.Canceled(Data)) throw new OperationCanceledException("当前任务已取消：" + id);
                        result = host.DownloadSkin(result);
                        if (host.Canceled(Data)) throw new OperationCanceledException("当前任务已取消：" + id);
                        host.Output(Data, result);
                    }
                } catch (Exception error) {
                    if (error.IsCanceled()) { host.Output(Data, ""); return; }
                    else if (error.GetDisplay(false).Contains("(429)")) { host.Output(Data, DefaultSkin(id)); Logger.Info($"获取离线登录使用的正版皮肤失败（{id}）：获取皮肤太过频繁，请 5 分钟后再试！"); }
                    else { host.Output(Data, DefaultSkin(id)); Logger.Warn(error, $"获取离线登录使用的正版皮肤失败（{id}）"); }
                }
                break;
            case 4:
                if (!FileUtils.Exists(host.AppDataPath() + "CustomSkin.png")) {
                    host.Hint("未找到离线皮肤自定义文件，可能它已被删除。PCL 将使用默认的 Steve 皮肤！"); host.WriteSetting("LaunchSkinType", 1); goto UseDefault;
                }
                host.Output(Data, host.AppDataPath() + "CustomSkin.png"); break;
        }
        Finish(Data, "Legacy");
    }
}
