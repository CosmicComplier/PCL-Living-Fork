#nullable disable
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace PCLCS;
public sealed partial class LegacyLaunchEngine<TInstance, TState, TCard, TOptions, TData, TServer, TMicrosoft, TOffline, TWatcher>
    where TInstance : LegacyMcInstance<TInstance, TState, TCard>
    where TState : struct, Enum where TCard : struct, Enum where TOptions : class, ILegacyLaunchOptions<TInstance>, new()
    where TData : class, ILegacyLaunchLoginData where TServer : TData, ILegacyLaunchServerData
    where TMicrosoft : TData, ILegacyLaunchMicrosoftData where TOffline : TData, ILegacyLaunchOfflineData where TWatcher : class {
public (string OAuthAccessToken, string OAuthRefreshToken) MsLoginStep1Refresh(string Code)
    {
        McLaunchLog("开始微软登录步骤 1/6（刷新登录）");

        string Result;
        try
        {
            Result = RequestMultiple("https://login.live.com/oauth20_token.srf", HttpMethod.Post, Content: $"client_id={host.OAuthClientId()}&refresh_token={StringUtils.FormUrlEscape(Code)}&grant_type=refresh_token&scope=XboxLive.signin%20offline_access", ContentType: "application/x-www-form-urlencoded", Headers: new[,] { { "Accept-Language", "en-US,en;q=0.5" }, { "X-Requested-With", "XMLHttpRequest" } }, ThreadCount: 2);
        }
        catch (HttpRequestCodeException ex)
        {
            // 修改错误列表时，同时检查 MyMsgLogin.xaml.vb 中的对应代码
            string Response = ex.Response;
            if (Response.ContainsIgnoreCase("must sign in again") || Response.ContainsIgnoreCase("password expired") || Response.Contains("refresh_token") && Response.ContainsIgnoreCase("is not valid") || Response.ContainsIgnoreCase("expired")) // #269
                                                                                                                                                                                                                                                     // #8611
            {
                return ("Relogin", "");
            }
            else if (Response.Contains("Account security interrupt"))
            {
                MyMsgBox("该账号由于安全问题无法登陆，请前往微软账户页获取更多信息。", "登录失败", "我知道了", IsWarn: true);
                throw new OperationCanceledException();
            }
            else if (Response.Contains("service abuse"))
            {
                MyMsgBox("非常抱歉，该账号已被微软封禁，无法登录。", "登录失败", "我知道了", IsWarn: true);
                throw new OperationCanceledException();
            }
            else
            {
                throw;
            }
        }

        JObject ResultJson = (JObject)Result.DeserializeJson();
        string AccessToken = ResultJson["access_token"].ToString();
        string RefreshToken = ResultJson["refresh_token"].ToString();
        return (AccessToken, RefreshToken);
    }


public string MsLoginStep2(string AccessToken)
    {
        McLaunchLog("开始微软登录步骤 2/6");

        string Request = "{\n           \"Properties\": {\n               \"AuthMethod\": \"RPS\",\n               \"SiteName\": \"user.auth.xboxlive.com\",\n               \"RpsTicket\": \"" + (AccessToken.StartsWithF("d=") ? "" : "d=") + AccessToken + "\"\n           },\n           \"RelyingParty\": \"http://auth.xboxlive.com\",\n           \"TokenType\": \"JWT\"\n        }";
        return RequestMultiple("https://user.auth.xboxlive.com/user/authenticate", HttpMethod.Post, Content: Request, ContentType: "application/json", RequireJson: true).DeserializeJson()["Token"].ToString();
    }


public (string XSTSToken, string UHS) MsLoginStep3(string XBLToken)
    {
        McLaunchLog("开始微软登录步骤 3/6");

        string Request = "{\n           \"Properties\": {\n               \"SandboxId\": \"RETAIL\",\n               \"UserTokens\": [\"" + XBLToken + "\"]\n           },\n           \"RelyingParty\": \"rp://api.minecraftservices.com/\",\n           \"TokenType\": \"JWT\"\n        }";
        string Result;
        try
        {
            Result = RequestMultiple("https://xsts.auth.xboxlive.com/xsts/authorize", HttpMethod.Post, Content: Request, ContentType: "application/json", RequireJson: true);
        }
        catch (HttpRequestCodeException ex)
        {
            // 参考 https://github.com/PrismarineJS/prismarine-auth/blob/master/src/common/Constants.js
            if (ex.Response.Contains("2148916227"))
            {
                MyMsgBox("该账号似乎已被微软封禁，无法登录。", "登录失败", "我知道了", IsWarn: true);
                throw new OperationCanceledException();
            }
            else if (ex.Response.Contains("2148916233"))
            {
                if (MyMsgBox("你尚未注册 Xbox 账户，请在注册后再登录。", "登录提示", "注册", "取消") == 1)
                {
                    host.OpenWebsite("https://signup.live.com/signup");
                }
                throw new OperationCanceledException();
            }
            else if (ex.Response.Contains("2148916235"))
            {
                MyMsgBox($"你的网络所在的国家或地区无法登录微软账号。{Constants.vbCrLf}请使用加速器或 VPN，然后再试。", "登录失败", "我知道了");
                throw new OperationCanceledException();
            }
            else if (ex.Response.Contains("2148916238"))
            {
                if (MyMsgBox("该账号年龄不足，你需要先修改出生日期，然后才能登录。" + Constants.vbCrLf + "该账号目前填写的年龄是否在 13 岁以上？", "登录提示", "13 岁以上", "12 岁以下", "我不知道") == 1)
                {
                    host.OpenWebsite("https://account.live.com/editprof.aspx");
                    MyMsgBox("请在打开的网页中修改账号的出生日期（至少改为 18 岁以上）。" + Constants.vbCrLf + "在修改成功后等待一分钟，然后再回到 PCL，就可以正常登录了！", "登录提示");
                }
                else
                {
                    host.OpenWebsite("https://support.microsoft.com/zh-cn/account-billing/如何更改-microsoft-帐户上的出生日期-837badbc-999e-54d2-2617-d19206b9540a");
                    MyMsgBox("请根据打开的网页的说明，修改账号的出生日期（至少改为 18 岁以上）。" + Constants.vbCrLf + "在修改成功后等待一分钟，然后再回到 PCL，就可以正常登录了！", "登录提示");
                }
                throw new OperationCanceledException();
            }
            else
            {
                throw;
            }
        }

        JObject ResultJson = (JObject)Result.DeserializeJson();
        return (ResultJson["Token"].ToString(), ResultJson["DisplayClaims"]["xui"][0]["uhs"].ToString()); // XSTSToken
                                                                                                          // UHS
    }


public (string AccessToken, int ExpiresAt) MsLoginStep4((string XSTSToken, string UHS) Tokens)
    {
        McLaunchLog("开始微软登录步骤 4/6");

        string Request = new JObject(new JProperty("identityToken", $"XBL3.0 x={Tokens.UHS};{Tokens.XSTSToken}")).ToString(0);
        string Result;
        try
        {
            Result = SendRequest("https://api.minecraftservices.com/authentication/login_with_xbox", HttpMethod.Post, Content: Request, ContentType: "application/json", RequireJson: true);
        }
        catch (HttpRequestCodeException ex)
        {
            if ((int)ex.StatusCode == 429)
            {
                Logger.Warn(ex, "微软登录第 4 步汇报 429");
                throw new Exception("$登录尝试太过频繁，请等待几分钟后再试！");
            }
            else if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Warn(ex, "微软登录第 4 步汇报 403");
                if (ex.Response is not null && ex.Response.IndexOf("Invalid app registration", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new Exception("$此启动器的 Client ID 尚未获 Minecraft 游戏服务认可。" + Constants.vbCrLf + "请检查 PCL_MS_CLIENT_ID 是否正确，并确认该 AppID 已通过 Mojang 审核、加入允许名单。" + Constants.vbCrLf + "如果刚提交审核，请等待审核完成后再试。", ex);
                }
                throw new Exception("$当前 IP 的登录尝试异常。" + Constants.vbCrLf + "如果你使用了 VPN 或加速器，请把它们关掉或更换节点后再试！");
            }
            else if (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                Logger.Warn(ex, "微软登录第 4 步汇报 503");
                throw new Exception("$Mojang 服务器出现问题，请稍后再试。" + Constants.vbCrLf + "你的网络是正常的，PCL 也是正常的，是 Mojang 出问题了……");
            }
            else if (ex.Response?.Contains("ACCOUNT_SUSPENDED") == true) // #8655
            {
                MyMsgBox("该账号似乎已被微软封禁，无法登录。", "登录失败", "我知道了", IsWarn: true);
                throw new OperationCanceledException();
            }
            else
            {
                throw;
            }
        }

        JObject ResultJson = (JObject)Result.DeserializeJson();
        return (ResultJson["access_token"].ToString(), Conversions.ToInteger(checked(ResultJson["expires_in"].ToObject<int>() + ToUnixTimestampUtc(DateTime.Now) - 3600L))); // 提前 1 小时视作过期
    }


public void MsLoginStep5(string AccessToken)
    {
        McLaunchLog("开始微软登录步骤 5/6");

        string Result = RequestMultiple("https://api.minecraftservices.com/entitlements/mcstore", ContentType: "application/json", ThreadCount: 2, Headers: new[,] { { "Authorization", "Bearer " + AccessToken } }, RequireJson: true);
        try
        {
            JObject ResultJson = (JObject)Result.DeserializeJson();
            if (!(ResultJson.ContainsKey("items") && ResultJson["items"].Any()))
            {
                switch (MyMsgBox("你尚未购买正版 Minecraft，或者 Xbox Game Pass 已到期。", "登录失败", "购买 Minecraft", "取消"))
                {
                    case 1:
                        {
                            host.OpenWebsite("https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
                            break;
                        }
                }
                throw new OperationCanceledException();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"微软登录第 5 步异常：{Result}");
            throw;
        }
    }


public (string UUID, string UserName, string ProfileJson) MsLoginStep6(string AccessToken)
    {
        McLaunchLog("开始微软登录步骤 6/6");

        string Result;
        try
        {
            Result = RequestMultiple("https://api.minecraftservices.com/minecraft/profile", ContentType: "application/json", ThreadCount: 2, Headers: new[,] { { "Authorization", "Bearer " + AccessToken } }, RequireJson: true);
        }
        catch (HttpRequestCodeException ex)
        {
            switch (ex.StatusCode)
            {
                case (HttpStatusCode)429:
                    {
                        Logger.Warn(ex, "微软登录第 6 步汇报 429");
                        throw new Exception("$登录尝试太过频繁，请等待几分钟后再试！");
                    }
                case HttpStatusCode.NotFound:
                    {
                        Logger.Warn(ex, "微软登录第 6 步汇报 404");
                        host.NewThread(() => { switch (MyMsgBox("请先创建 Minecraft 玩家档案，然后再重新登录。", "登录失败", "创建档案", "取消")) { case 1: { host.OpenWebsite("https://www.minecraft.net/zh-hans/msaprofile/mygames/editprofile"); break; } } }, "Login Failed: Create Profile");
                        throw new OperationCanceledException();
                    }

                default:
                    {
                        throw;
                    }
            }
        }
        JObject ResultJson = (JObject)Result.DeserializeJson();
        string UUID = ResultJson["id"].ToString();
        string UserName = ResultJson["name"].ToString();
        return (UUID, UserName, Result);
    }


public string McLoginLegacyUuidWithCustomSkin(string UserName, int SkinType, string SkinName)
    {
        string Uuid = Conversions.ToString(McLoginLegacyUuid(UserName));
        // 根据离线皮肤获取实际使用的 Uuid
        switch (SkinType)
        {
            case 0:
                {
                    break;
                }
            // 默认，不需要处理
            case 1:
                {
                    // Steve
                    while (LegacyMinecraftText.McSkinSex(Uuid) != "Steve")
                    {
                        if (Uuid.EndsWithF("FFFFF"))
                            Uuid = Uuid.Substring(0, 27) + "00000";
                        Uuid = Uuid.Substring(0, 27) + (long.Parse(Uuid.Substring(27), NumberStyles.AllowHexSpecifier) + 1L).ToString("X").PadLeft(5, '0');
                    }

                    break;
                }
            case 2:
                {
                    // Alex
                    while (LegacyMinecraftText.McSkinSex(Uuid) != "Alex")
                    {
                        if (Uuid.EndsWithF("FFFFF"))
                            Uuid = Uuid.Substring(0, 27) + "00000";
                        Uuid = Uuid.Substring(0, 27) + (long.Parse(Uuid.Substring(27), NumberStyles.AllowHexSpecifier) + 1L).ToString("X").PadLeft(5, '0');
                    }

                    break;
                }
            case 3:
                {
                    // 使用正版用户名
                    try
                    {
                        if (!string.IsNullOrEmpty(SkinName) && McInstanceSelected is not null && McInstanceSelected.Version.Vanilla.Major < 20) // 1.20+ 或快照版不能使用该项（#3746）
                        {
                            Logger.Info($"由于离线皮肤设置，使用正版 UUID：{SkinName}");
                            Uuid = Conversions.ToString(McLoginMojangUuid(SkinName, false));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "皮肤信息获取失败，游戏将以无皮肤的方式启动", LogBehavior.Toast);
                    }

                    break;
                }
            case 4:
                {
                    // 自定义
                    while ((LegacyMinecraftText.McSkinSex(Uuid) ?? "") != ((Setting<bool>("LaunchSkinSlim") ? "Alex" : "Steve") ?? ""))
                    {
                        if (Uuid.EndsWithF("FFFFF"))
                            Uuid = Uuid.Substring(0, 27) + "00000";
                        Uuid = Uuid.Substring(0, 27) + (long.Parse(Uuid.Substring(27), NumberStyles.AllowHexSpecifier) + 1L).ToString("X").PadLeft(5, '0');
                    }

                    break;
                }
        }
        return Uuid;
    }


public object McLoginMojangUuid(string Name, bool ThrowOnNotFound)
    {
        if (Name.Trim().Length == 0)
            return new string('0', 32);
        // 从缓存获取
        string Uuid = host.ReadIni(host.TempFolder() + @"Cache\Uuid\Mojang.ini", Name, "");
        if (Strings.Len(Uuid) == 32)
            return Uuid;
        // 从官网获取
        try
        {
            JObject GotJson = (JObject)SendRequest("https://api.mojang.com/users/profiles/minecraft/" + Name, RequireJson: true).DeserializeJson();
            if (GotJson is null)
                throw new FileNotFoundException("正版玩家档案不存在（" + Name + "）");
            Uuid = (string)(GotJson["id"] ?? "");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"从官网获取正版 Uuid 失败（{Name}）");
            if (!ThrowOnNotFound && ex.GetType().Name == "FileNotFoundException")
            {
                Uuid = Conversions.ToString(McLoginLegacyUuid(Name)); // 玩家档案不存在
            }
            else
            {
                throw new Exception("从官网获取正版 Uuid 失败", ex);
            }
        }
        // 写入缓存
        if (!(Strings.Len(Uuid) == 32))
            throw new Exception("获取的正版 Uuid 长度不足（" + Uuid + "）");
        host.WriteIni(host.TempFolder() + @"Cache\Uuid\Mojang.ini", Name, Uuid);
        return Uuid;
    }


public object McLoginLegacyUuid(string Name)
    {
        string FullUuid = Name.Length.ToString("X").EnsureLength('0', 16) + Name.GetStableHashCode().ToString("X").EnsureLength('0', 16);
        return FullUuid.Substring(0, 12) + "3" + FullUuid.Substring(13, 3) + "9" + FullUuid.Substring(17, 15);
    }

    public (string OAuthAccessToken, string OAuthRefreshToken) MsLoginStep1New(LegacyLaunchTask<TMicrosoft, ILegacyLaunchLoginResult> data) {
        while (true) {
            McLaunchLog("开始微软登录步骤 1/6（原始登录）");
            JObject prepared = (JObject)SendRequest("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", HttpMethod.Post,
                Content: $"client_id={host.OAuthClientId()}&tenant=/consumers&scope=XboxLive.signin%20offline_access",
                ContentType: "application/x-www-form-urlencoded", RequireJson: true).DeserializeJson();
            McLaunchLog("网页登录地址：" + prepared["verification_uri"].ToString());
            Func<LegacyLaunchDeviceResult> prompt = host.DeviceLoginPrompt(prepared);
            LegacyLaunchDeviceResult result;
            while ((result = prompt()) == null) Thread.Sleep(100);
            if (result.Kind == LegacyLaunchDeviceResultKind.Restart) {
                if (MyMsgBox("请在登录时选择 “其他登录方法”，然后选择 “使用我的密码”。\r\n如果没有该选项，请选择 “设置密码”，设置完毕后再登录。",
                    "需要使用密码登录", "重新登录", "设置密码", "取消", Button2Action: () => host.OpenWebsite("https://account.live.com/password/Change")) == 1) continue;
                throw new OperationCanceledException();
            }
            if (result.Kind == LegacyLaunchDeviceResultKind.Error) throw result.Error;
            return (result.AccessToken, result.RefreshToken);
        }
    }

}
