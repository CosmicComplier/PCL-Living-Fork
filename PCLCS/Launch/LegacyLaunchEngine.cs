using System;
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

public enum LegacyLaunchLoginType { Legacy = 0, Nide = 2, Auth = 3, Ms = 5 }
public struct LegacyLaunchSize(double width, double height) { public double Width = width, Height = height; }

public sealed partial class LegacyLaunchEngine<TInstance, TState, TCard, TOptions, TData, TServer, TMicrosoft, TOffline, TWatcher>(
    LegacyLaunchHost<TInstance, TOptions, TData, TServer, TMicrosoft, TOffline, TWatcher> host)
    where TInstance : LegacyMcInstance<TInstance, TState, TCard>
    where TState : struct, Enum where TCard : struct, Enum
    where TOptions : class, ILegacyLaunchOptions<TInstance>, new()
    where TData : class, ILegacyLaunchLoginData
    where TServer : TData, ILegacyLaunchServerData where TMicrosoft : TData, ILegacyLaunchMicrosoftData
    where TOffline : TData, ILegacyLaunchOfflineData where TWatcher : class {
    private TOptions CurrentLaunchOptions { get => host.Options(); set => host.StoreOptions(value); }
    private TInstance McInstanceSelected { get => host.SelectedInstance(); set => host.StoreSelectedInstance(value); }
    private Java McLaunchJavaSelected { get => host.SelectedJava(); set => host.StoreSelectedJava(value); }
    private string McLaunchArgument { get => host.Arguments(); set => host.StoreArguments(value); }
    private string CanceledHint { get => host.CanceledHint(); set => host.StoreCanceledHint(value); }
    private Encoding McLaunchEncoding { get => host.Encoding(); set => host.StoreEncoding(value); }
    private LegacyLaunchTask<TOptions, object> McLaunchLoader => host.LaunchLoader();
    private LegacyLaunchTask<TData, ILegacyLaunchLoginResult> McLoginLoader => host.LoginLoader();

    private static long ToUnixTimestampUtc(DateTime time) => Conversions.ToLong((time.ToUniversalTime().Ticks - 621355968000000000L) / 10000000d);
    private string ArgumentReplace(string text, bool ReplaceTime = true) => host.ArgumentReplace(text, ReplaceTime);

    private T Setting<T>(string key, TInstance? Instance = null) {
        object? value;
        if (typeof(T) == typeof(string)) value = host.ReadString(key, Instance);
        else if (typeof(T) == typeof(bool)) value = host.ReadBoolean(key, Instance);
        else if (typeof(T) == typeof(int)) value = host.ReadInteger(key, Instance);
        else if (typeof(T).IsEnum) value = Enum.ToObject(typeof(T), host.ReadInteger(key, Instance));
        else throw new InvalidOperationException("Unsupported launch setting type: " + typeof(T));
        return (T)value!;
    }
    private int MyMsgBox(string Caption, string Title = "提示", string Button1 = "确定", string Button2 = "", string Button3 = "",
                         bool IsWarn = false, bool HighLight = true, bool ForceWait = false,
                         Action? Button1Action = null, Action? Button2Action = null, Action? Button3Action = null) => host.Message(new LegacyLaunchMessage {
        Text = Caption, Title = Title, Button1 = Button1, Button2 = Button2, Button3 = Button3,
        IsWarn = IsWarn, HighLight = HighLight, ForceWait = ForceWait,
        Button1Action = Button1Action, Button2Action = Button2Action, Button3Action = Button3Action
    });
    private string SendRequest(string Url, HttpMethod? Method = null, object? Content = null, string? ContentType = null,
                           string[,]? Headers = null, bool RequireJson = false, Encoding? Encoding = null, int? ThreadCount = null) =>
        host.Request(new LegacyLaunchRequest { Url = Url, Method = Method, Content = Content, ContentType = ContentType,
            Headers = Headers, RequireJson = RequireJson, Encoding = Encoding, ThreadCount = ThreadCount, Kind = LegacyLaunchRequestKind.Retry });
    private string RequestMultiple(string Url, HttpMethod? Method = null, object? Content = null, string? ContentType = null,
                                   string[,]? Headers = null, bool RequireJson = false, Encoding? Encoding = null, int? ThreadCount = null) =>
        host.Request(new LegacyLaunchRequest { Url = Url, Method = Method, Content = Content, ContentType = ContentType,
            Headers = Headers, RequireJson = RequireJson, Encoding = Encoding, ThreadCount = ThreadCount, Kind = LegacyLaunchRequestKind.Multiple });
    private static Encoding LegacyEncoding(string name) {
#if NETFRAMEWORK
        return Encoding.GetEncoding(name);
#else
        return CodePagesEncodingProvider.Instance.GetEncoding(name) ?? Encoding.GetEncoding(name);
#endif
    }
    private static Encoding LegacyEncoding(int codePage) {
#if NETFRAMEWORK
        return Encoding.GetEncoding(codePage);
#else
        return CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
#endif
    }
    public void McLaunchLog(string text, LogLevel level = LogLevel.Info) {
        text = host.FilterUserName(host.FilterAccessToken(text, '*'), '*');
        if (host.Debug()) host.Dispatch(() => host.AppendLog($"[{DateTime.Now:HH':'mm':'ss'.'fff}] {text}\r\n"));
        Logger.Log(text, level);
    }
    private void Hint(string text, LegacyLaunchHint kind = LegacyLaunchHint.Default) => host.Hint(text, kind);
}
