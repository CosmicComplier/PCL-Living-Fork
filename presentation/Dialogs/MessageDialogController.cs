using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using MeloongCore;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;
using PCLLF.Presentation.Shell;

namespace PCLLF.Presentation.Dialogs;

// LF Controls/MyMsg/MyMsg{Text,Select,Input}.xaml.vb, fixed LF initial/current sources.
// Original converter fields and validation collections remain the actual closed C031b model, not copied dialog DTOs.
public sealed class ModalSelectionPort {
    public readonly Func<bool> HasSelection;
    public readonly Action<Action> ConnectChanged;
    public ModalSelectionPort(Func<bool> hasSelection, Action<Action> connectChanged) { HasSelection = hasSelection; ConnectChanged = connectChanged; }
}
public sealed class ModalRadioPort {
    public readonly FrameworkElement Element;
    public readonly Action<Action<object?>> ConnectCheck;
    public ModalRadioPort(FrameworkElement element, Action<Action<object?>> connectCheck) { Element = element; ConnectCheck = connectCheck; }
    public static ModalRadioPort ForNative(FrameworkElement element) {
        if (element is MyListItemView list) return new(element, handler => list.Check += (sender, _) => handler(sender));
        if (element is MyRadioBoxView radio) return new(element, handler => radio.Check += (sender, _) => handler(sender));
        throw new InvalidCastException("选择控件必须实现实际单选接口");
    }
}
public sealed class MessageDialogHost {
    public Func<int> NewUuid = null!;
    public Func<Grid?> MessagePanel = null!;
    public Func<bool> HasWaiting = null!;
    public Action DragMove = null!;
    public Func<double, double> PixelToWpf = null!;
    public Func<ControlAnimations> Animations = null!;
    // These project the actual LF body classes; precedence, bounds and enablement policy remain below.
    public Func<FrameworkElement?, double?>? UpdateWidth;
    public Func<FrameworkElement?, double?> ModpackWidth = null!, DependenciesWidth = null!, GraphWidth = null!, ImpactsWidth = null!;
    public Func<object?, ModalSelectionPort?> VersionSelection = null!;
    public Func<FrameworkElement, ModalRadioPort> Radio = null!;
}
public class MessageDialogParts {
    public Grid Root = null!;
    public Func<MyButtonView> Button1 = null!, Button2 = null!;
    public Func<TextBlock> Title = null!;
    public Func<Rectangle> Line = null!;
    public Func<TranslateTransform> Position = null!;
    public Func<RotateTransform> Rotation = null!;
}
public sealed class MessageTextParts : MessageDialogParts {
    public Func<MyButtonView> Button3 = null!;
    public Func<MyScrollViewerView> Caption = null!;
    public Func<TextBlock> CaptionText = null!;
}
public sealed class MessageSelectParts : MessageDialogParts {
    public Func<StackPanel> Selection = null!;
    public Func<int> SelectedIndex = null!;
    public Action<int> SetSelectedIndex = null!;
}
public sealed class MessageInputParts : MessageDialogParts {
    public Func<MyTextBoxView> Input = null!;
    public Func<MyScrollViewerView> TextPanel = null!;
    public Func<TextBlock> CaptionText = null!;
    // Assigns the original collection to the actual input, preserving collection identity and typed legacy rules.
    public Action ApplyRules = null!;
}
public interface IMessageDialogController {
    void RenameButtons();
    void Initialize();
    void InitializationFailed(Exception error);
    void Load();
    void Button1Click();
    void Button2Click();
    void Drag(MouseButtonEventArgs args);
}
public interface IMessageTextController : IMessageDialogController {
    void Button3Click();
    void RefreshContentBounds();
    void ContentHostLoaded();
    void ContentHostUnloaded();
}
public interface IMessageInputController : IMessageDialogController { void ValidateChanged(); }

public abstract class MessageDialogController<TKind, TValidation> : IMessageDialogController {
    protected readonly MessageDialogParts Parts;
    protected readonly MessageDialogHost Host;
    private readonly Func<MainMessageData<TKind, TValidation>?> readConverter;
    private readonly Func<int> uuid;
    protected MainMessageData<TKind, TValidation> Converter => readConverter()!;
    protected bool HasConverter => readConverter() is not null;
    protected abstract string Kind { get; }
    protected ControlAnimations Animations => Host.Animations();
    protected MessageDialogController(MessageDialogParts parts, Func<MainMessageData<TKind, TValidation>?> readConverter, MessageDialogHost host, Func<int> uuid) {
        Parts = parts; this.readConverter = readConverter; Host = host; this.uuid = uuid;
    }
    protected static bool Same(string? first, string? second) => string.Equals(first ?? "", second ?? "", StringComparison.Ordinal);
    public virtual void RenameButtons() {
        Parts.Button1().Name += Conversions.ToString(Host.NewUuid());
        Parts.Button2().Name += Conversions.ToString(Host.NewUuid());
    }
    public abstract void Initialize();
    public void InitializationFailed(Exception error) => Logger.Error(error, Kind + "弹窗初始化失败", LogBehavior.Toast);
    protected void InitializeCommon() {
        Parts.Title().Text = Converter.Title;
        Parts.Button1().Text = Converter.Button1;
        if (Converter.IsWarn) {
            Parts.Button1().ColorType = MyButtonView.ColorState.Red;
            Parts.Title().SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight");
        }
        Parts.Button2().Text = Converter.Button2;
        Parts.Button2().Visibility = Same(Converter.Button2, "") ? Visibility.Collapsed : Visibility.Visible;
        Parts.Line().StrokeThickness = Host.PixelToWpf(1);
    }
    protected virtual void FocusInput() { }
    protected abstract string LogText();
    public void Load() {
        try {
            if (Parts.Button2().IsVisible && Parts.Button1().ColorType != MyButtonView.ColorState.Red) Parts.Button1().ColorType = MyButtonView.ColorState.Highlight;
            FocusInput();
            Parts.Root.Opacity = 0;
            Animations.Start(Animations.ColorTo(Host.MessagePanel()!, Grid.BackgroundProperty,
                Converter.IsWarn ? new ControlColor(140, 80, 0, 0) : new ControlColor(90, 0, 0, 0), new(200)), "PanMsg Background");
            Animations.Start(new[] {
                Animations.Opacity(Parts.Root, 1, new(120, 60)),
                Animations.Double(delta => Parts.Position().Y += delta, -Parts.Position().Y, new(300, 60, ControlEase.OutBack, ControlEasePower.Weak)),
                Animations.Double(delta => Parts.Rotation().Angle += delta, -Parts.Rotation().Angle, new(300, 60, ControlEase.OutFluent, ControlEasePower.Weak))
            }, "MyMsgBox " + Conversions.ToString(uuid()));
            Logger.Info(LogText());
        } catch (Exception error) { Logger.Error(error, Kind + "弹窗加载失败", LogBehavior.Toast); }
    }
    protected void Close(bool waitOnlyWhenRequested = false) {
        if (!waitOnlyWhenRequested || Converter.ForceWait || !Same(Converter.Button2, "")) Converter.WaitFrame.Continue = false;
        // The LF dialog and outer wait each pop at their original points; do not silently redesign modal ownership.
        ComponentDispatcher.PopModal();
        Animations.Start(new[] {
            Animations.Code(() => {
                if (!Host.HasWaiting()) Animations.Start(Animations.ColorTo(Host.MessagePanel()!, Grid.BackgroundProperty,
                    new ControlColor(0, 0, 0, 0), new(200, ease: ControlEase.OutFluent, power: ControlEasePower.Weak)));
            }, 30),
            Animations.Opacity(Parts.Root, -Parts.Root.Opacity, new(80, 20)),
            Animations.Double(delta => Parts.Position().Y += delta, 20 - Parts.Position().Y, new(150, ease: ControlEase.OutFluent)),
            Animations.Double(delta => Parts.Rotation().Angle += delta, 6 - Parts.Rotation().Angle, new(150, ease: ControlEase.InFluent, power: ControlEasePower.Weak)),
            Animations.Code(() => ((Grid)Parts.Root.Parent).Children.Remove(Parts.Root), after: true)
        }, "MyMsgBox " + Conversions.ToString(uuid()));
    }
    public abstract void Button1Click();
    public abstract void Button2Click();
    public void Drag(MouseButtonEventArgs args) {
        ProjectData.ClearProjectError(); // Original On Error Resume Next declaration.
        bool drag;
        try { drag = args.GetPosition(Parts.Line()).Y <= 2; }
        catch (Exception error) { ProjectData.SetProjectError(error); drag = true; } // Original VB Resume Next continues the If body after a condition failure.
        if (drag) { try { Host.DragMove(); } catch (Exception error) { ProjectData.SetProjectError(error); } }
    }
}

public sealed class MessageTextController<TKind, TValidation> : MessageDialogController<TKind, TValidation>, IMessageTextController {
    private readonly MessageTextParts text;
    protected override string Kind => "普通";
    public MessageTextController(MessageTextParts parts, Func<MainMessageData<TKind, TValidation>?> converter, MessageDialogHost host, Func<int> uuid)
        : base(parts, converter, host, uuid) { text = parts; }
    public override void RenameButtons() { base.RenameButtons(); text.Button3().Name += Conversions.ToString(Host.NewUuid()); }
    public override void Initialize() {
        Parts.Title().Text = Converter.Title;
        text.CaptionText().Text = Converter.Text;
        if (Converter.ContentFactory is not null) {
            text.Caption().Content = Converter.ContentFactory();
            text.Caption().VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            text.Caption().Padding = new Thickness(0); Parts.Root.MinWidth = 0;
            RefreshContentBounds();
            ModalSelectionPort? versions = Host.VersionSelection(text.Caption().Content);
            if (versions is not null) {
                Parts.Button1().IsEnabled = versions.HasSelection();
                versions.ConnectChanged(() => Parts.Button1().IsEnabled = versions.HasSelection());
            }
        }
        Parts.Button1().Text = Converter.Button1;
        if (Converter.IsWarn) {
            MyButtonView warning = Converter.WarningButton == 3 ? text.Button3() : Converter.WarningButton == 2 ? Parts.Button2() : Parts.Button1();
            warning.ColorType = MyButtonView.ColorState.Red;
            Parts.Title().SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight");
        }
        Parts.Button2().Text = Converter.Button2; text.Button3().Text = Converter.Button3;
        Parts.Button2().Visibility = Same(Converter.Button2, "") ? Visibility.Collapsed : Visibility.Visible;
        text.Button3().Visibility = Same(Converter.Button3, "") ? Visibility.Collapsed : Visibility.Visible;
        Parts.Line().StrokeThickness = Host.PixelToWpf(1);
    }
    public void RefreshContentBounds() {
        if (!HasConverter || Converter.ContentFactory is null) return;
        Grid? panel = Host.MessagePanel();
        FrameworkElement? body = text.Caption().Content as FrameworkElement;
        double preferred = Host.UpdateWidth?.Invoke(body) ?? Host.ModpackWidth(body) ?? Host.DependenciesWidth(body) ?? Host.GraphWidth(body) ?? Host.ImpactsWidth(body) ?? 980;
        Parts.Root.Width = Math.Min(preferred, Math.Max(0, (panel is null ? 1000 : panel.ActualWidth) - 50));
        Parts.Root.MaxHeight = Math.Max(0, (panel is null ? 740 : panel.ActualHeight) - 50);
        if (body is not null) body.MaxHeight = Math.Max(0, Parts.Root.MaxHeight - 156);
    }
    private void ContentHostResized(object? sender, SizeChangedEventArgs args) => RefreshContentBounds();
    public void ContentHostLoaded() {
        if (!HasConverter || Converter.ContentFactory is null) return;
        if (Host.MessagePanel() is not null) Host.MessagePanel()!.SizeChanged += ContentHostResized;
        RefreshContentBounds();
    }
    public void ContentHostUnloaded() {
        if (HasConverter && Converter.ContentFactory is not null && Host.MessagePanel() is not null) Host.MessagePanel()!.SizeChanged -= ContentHostResized;
    }
    protected override void FocusInput() => Parts.Button1().Focus();
    protected override string LogText() => $"普通弹窗：{Parts.Title().Text}\r\n{text.CaptionText().Text}";
    public override void Button1Click() {
        if (Converter.IsExited) return;
        if (Converter.Button1Action is not null) Converter.Button1Action();
        else { Converter.IsExited = true; Converter.Result = 1; Close(true); }
    }
    public override void Button2Click() {
        if (Converter.IsExited) return;
        if (Converter.Button2Action is not null) Converter.Button2Action();
        else { Converter.IsExited = true; Converter.Result = 2; Close(true); }
    }
    public void Button3Click() {
        if (Converter.IsExited) return;
        if (Converter.Button3Action is not null) Converter.Button3Action();
        else { Converter.IsExited = true; Converter.Result = 3; Close(true); }
    }
}

public sealed class MessageSelectController<TKind, TValidation> : MessageDialogController<TKind, TValidation> {
    private readonly MessageSelectParts select;
    protected override string Kind => "选择";
    public MessageSelectController(MessageSelectParts parts, Func<MainMessageData<TKind, TValidation>?> converter, MessageDialogHost host, Func<int> uuid)
        : base(parts, converter, host, uuid) { select = parts; }
    public override void Initialize() {
        InitializeCommon(); Parts.Button1().IsEnabled = false;
        foreach (object item in (IEnumerable)Converter.Content!) {
            FrameworkElement content = MyVirtualizingElementView.TryInit((FrameworkElement)item);
            ModalRadioPort selection = Host.Radio(content);
            select.Selection().Children.Add(selection.Element);
            selection.ConnectCheck(sender => { Parts.Button1().IsEnabled = true; select.SetSelectedIndex(select.Selection().Children.IndexOf((UIElement)sender!)); });
            if (selection.Element is MyListItemView list) { list.Type = MyListItemView.CheckType.RadioBox; list.MinHeight = 24; }
            else ((MyRadioBoxView)selection.Element).MinHeight = 24;
        }
    }
    protected override string LogText() => $"选择弹窗：{Parts.Title().Text}";
    public override void Button1Click() {
        if (Converter.IsExited || select.SelectedIndex() == -1) return;
        Converter.IsExited = true; Converter.Result = select.SelectedIndex(); Close();
    }
    public override void Button2Click() {
        if (Converter.IsExited) return;
        Converter.IsExited = true; Converter.Result = null; Close();
    }
}

public sealed class MessageInputController<TKind, TValidation> : MessageDialogController<TKind, TValidation>, IMessageInputController {
    private readonly MessageInputParts input;
    protected override string Kind => "输入";
    public MessageInputController(MessageInputParts parts, Func<MainMessageData<TKind, TValidation>?> converter, MessageDialogHost host, Func<int> uuid)
        : base(parts, converter, host, uuid) { input = parts; }
    public override void Initialize() {
        Parts.Title().Text = Converter.Title; input.CaptionText().Text = Converter.Text;
        input.TextPanel().Visibility = Same(Converter.Text, "") ? Visibility.Collapsed : Visibility.Visible;
        input.Input().Text = Conversions.ToString(Converter.Content);
        input.Input().HintText = Converter.HintText; input.ApplyRules();
        Parts.Button1().Text = Converter.Button1;
        if (Converter.IsWarn) {
            Parts.Button1().ColorType = MyButtonView.ColorState.Red;
            Parts.Title().SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight");
        }
        Parts.Button2().Text = Converter.Button2;
        Parts.Button2().Visibility = Same(Converter.Button2, "") ? Visibility.Collapsed : Visibility.Visible;
        Parts.Line().StrokeThickness = Host.PixelToWpf(1);
    }
    protected override void FocusInput() { input.Input().Focus(); input.Input().SelectionStart = input.Input().Text.Length; }
    protected override string LogText() => $"输入弹窗：{Parts.Title().Text}";
    public override void Button1Click() {
        input.Input().Validate(); // LF #5773: deliberately before even the IsExited guard.
        if (Converter.IsExited || !input.Input().IsValidated) return;
        Converter.IsExited = true; Converter.Result = input.Input().Text; Close();
    }
    public override void Button2Click() {
        if (Converter.IsExited) return;
        Converter.IsExited = true; Converter.Result = null; Close();
    }
    public void ValidateChanged() => Parts.Button1().IsEnabled = input.Input().IsValidated;
}
