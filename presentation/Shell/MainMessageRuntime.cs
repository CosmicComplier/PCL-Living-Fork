using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using MeloongCore;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLLF.Presentation.Shell;

// LF ModMain.vb (b8d00480c4797abcbe9b90a70cf58a0c4d877466, LF LICENCE).
// Closed enum/rule parameters keep the actual original nested enum and Validate types;
// there are no copied converter fields or parallel message queues.
public class MainMessageData<TKind, TValidation> {
    public TKind Type = default!;
    public string? Title, Text;
    public object? Content;
    public Func<FrameworkElement>? ContentFactory;
    public Collection<TValidation>? ValidateRules;
    public string HintText = "";
    public bool HighLight;
    public string? Button1 = "确定", Button2 = "", Button3 = "";
    public Action? Button1Action, Button2Action, Button3Action;
    public bool IsWarn;
    public int WarningButton = 1;
    public bool ForceWait;
    public DispatcherFrame WaitFrame = new(true);
    public bool IsExited;
    public object? Result;
}

public sealed class MainMessageHost<TMessage, TKind> {
    public Func<Window?> Window = null!;
    public Func<Panel?> Panel = null!;
    public Action DragStop = null!;
    public Func<bool> IsUiThread = null!;
    public Func<ConcurrentQueue<TMessage>> Queue = null!;
    public Func<TMessage> Create = null!;
    public TKind Text = default!, Select = default!, Input = default!, Login = default!;
    public Func<TMessage, UIElement> CreateInput = null!, CreateSelect = null!, CreateText = null!, CreateLogin = null!;
}

public sealed class MainMessageRuntime<TMessage, TKind, TValidation>
    where TMessage : MainMessageData<TKind, TValidation> {
    private readonly MainMessageHost<TMessage, TKind> host;
    public MainMessageRuntime(MainMessageHost<TMessage, TKind> host) { this.host = host; }

    public int Show(string? caption, string? title = "提示", string? button1 = "确定", string? button2 = "", string? button3 = "",
        bool isWarn = false, bool highLight = true, bool forceWait = false,
        Action? button1Action = null, Action? button2Action = null, Action? button3Action = null) {
        caption ??= ""; title ??= "提示"; button1 ??= "确定"; button2 ??= ""; button3 ??= "";
        TMessage converter = host.Create();
        converter.Type = host.Text; converter.Button1 = button1; converter.Button2 = button2; converter.Button3 = button3;
        converter.Text = caption; converter.IsWarn = isWarn; converter.Title = title;
        converter.HighLight = highLight; converter.ForceWait = true;
        converter.Button1Action = button1Action; converter.Button2Action = button2Action; converter.Button3Action = button3Action;
        if (button2.Length > 0 || forceWait) {
            // Preserve LF's exact precedence and background-thread fallback boundary.
            if (host.Window() is null || host.Panel() is null && host.IsUiThread()) {
                if (button2.Length > 0) {
                    MsgBoxResult result = Interaction.MsgBox(caption,
                        (button3.Length > 0 ? MsgBoxStyle.YesNoCancel : MsgBoxStyle.YesNo) +
                        (isWarn ? (int)MsgBoxStyle.Critical : (int)MsgBoxStyle.Question), title);
                    switch (result) {
                        case MsgBoxResult.Yes: converter.Result = 1; break;
                        case MsgBoxResult.No: converter.Result = 2; break;
                        case MsgBoxResult.Cancel: converter.Result = 3; break;
                    }
                } else {
                    Interaction.MsgBox(caption, MsgBoxStyle.OkOnly + (isWarn ? (int)MsgBoxStyle.Critical : (int)MsgBoxStyle.Question), title);
                    converter.Result = 1;
                }
                Logger.Warn($"主窗体加载完成前出现意料外的等待弹窗：{button1},{button2},{button3}");
            } else {
                host.Queue().Enqueue(converter);
                try {
                    host.DragStop();
                    if (host.IsUiThread()) Tick();
                    ComponentDispatcher.PushModal();
                    Dispatcher.PushFrame(converter.WaitFrame);
                } finally { ComponentDispatcher.PopModal(); }
            }
            Logger.Info($"普通弹框返回：{converter.Result ?? "null"}");
            return Conversions.ToInteger(converter.Result);
        }
        host.Queue().Enqueue(converter);
        return 1;
    }

    public string? Input(string? title, string? text = null, string? defaultInput = null,
        Collection<TValidation>? validateRules = null, string? hintText = "", string? button1 = "确定", string? button2 = "取消", bool isWarn = false) {
        TMessage converter = host.Create();
        converter.Text = text ?? ""; converter.HintText = hintText ?? ""; converter.Type = host.Input;
        converter.ValidateRules = validateRules ?? new Collection<TValidation>();
        converter.Button1 = button1; converter.Button2 = button2; converter.Content = defaultInput;
        converter.IsWarn = isWarn; converter.Title = title;
        host.Queue().Enqueue(converter);
        Wait(converter);
        Logger.Info($"输入弹框返回：{converter.Result ?? "null"}");
        return Conversions.ToString(converter.Result);
    }

    // The actual IEnumerable<IMyRadio> reference is stored as Content without enumerating/copying it.
    public int? Select(object? selections, string? title = "提示", string? button1 = "确定", string? button2 = "", bool isWarn = false) {
        TMessage converter = host.Create();
        converter.Type = host.Select; converter.Button1 = button1; converter.Button2 = button2;
        converter.Content = selections; converter.IsWarn = isWarn; converter.Title = title;
        host.Queue().Enqueue(converter);
        Wait(converter);
        Logger.Info($"选择弹框返回：{converter.Result ?? "null"}");
        return converter.Result is null ? null : Conversions.ToInteger(converter.Result);
    }

    private void Wait(TMessage converter) {
        try {
            if (host.Window() is not null) host.DragStop();
            if (host.IsUiThread()) Tick();
            ComponentDispatcher.PushModal();
            Dispatcher.PushFrame(converter.WaitFrame);
        } finally { ComponentDispatcher.PopModal(); }
    }

    public void Content(string? caption, string? title, Func<FrameworkElement>? factory) {
        TMessage converter = host.Create();
        converter.Type = host.Text; converter.Text = caption ?? ""; converter.Title = title ?? "提示";
        converter.ContentFactory = factory; converter.HighLight = true; converter.ForceWait = true;
        host.Queue().Enqueue(converter);
    }

    public int ConfirmContent(string? caption, string? title, string? confirmText, Func<FrameworkElement>? factory,
        string? cancelText = "取消", string? thirdText = "", int warningButton = 1, bool isWarn = true) {
        if (host.Panel() is null) return Show(caption, title, confirmText, cancelText, thirdText, isWarn);
        TMessage converter = host.Create();
        converter.Type = host.Text; converter.Text = caption; converter.Title = title;
        converter.Button1 = confirmText; converter.Button2 = cancelText; converter.Button3 = thirdText;
        converter.IsWarn = isWarn; converter.WarningButton = warningButton; converter.ForceWait = true; converter.ContentFactory = factory;
        host.Queue().Enqueue(converter);
        try {
            host.DragStop();
            if (host.IsUiThread()) Tick();
            ComponentDispatcher.PushModal();
            Dispatcher.PushFrame(converter.WaitFrame);
        } finally { ComponentDispatcher.PopModal(); }
        return Conversions.ToInteger(converter.Result ?? 2);
    }

    public void Tick() {
        try {
            if (host.Panel() is null || host.Window()!.WindowState == WindowState.Minimized) return;
            if (host.Panel()!.Children.Count > 0) {
                host.Panel()!.Visibility = Visibility.Visible;
            } else if (host.Queue().TryDequeue(out TMessage? target)) {
                host.Panel()!.Visibility = Visibility.Visible;
                if (Same(target.Type, host.Input)) host.Panel()!.Children.Add(host.CreateInput(target));
                else if (Same(target.Type, host.Select)) host.Panel()!.Children.Add(host.CreateSelect(target));
                else if (Same(target.Type, host.Text)) host.Panel()!.Children.Add(host.CreateText(target));
                else if (Same(target.Type, host.Login)) host.Panel()!.Children.Add(host.CreateLogin(target));
            } else if (host.Panel()!.Visibility != Visibility.Collapsed) {
                host.Panel()!.Visibility = Visibility.Collapsed;
            }
        } catch (Exception ex) { Logger.Error(ex, "处理等待中的弹窗失败"); }
    }
    private static bool Same(TKind first, TKind second) => EqualityComparer<TKind>.Default.Equals(first, second);
}
