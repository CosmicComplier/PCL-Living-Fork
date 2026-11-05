using System;
using System.Windows.Controls;
using PCLCS.Validation;
using PCLLF.Presentation.Controls;
using PCLLF.Presentation.Shell;

namespace PCLLF.Presentation.Dialogs;

public partial class MyMsgInputView : Grid {
    public MyMsgInputView() { }
    private IMessageInputController? controller;
    public static MyMsgInputView Create<TKind>(MainMessageData<TKind, LegacyValidationRule> converter, MessageDialogHost host) {
        MyMsgInputView view = new(); int uuid = host.NewUuid();
        MainMessageData<TKind, LegacyValidationRule>? current = null;
        var controller = new MessageInputController<TKind, LegacyValidationRule>(new MessageInputParts {
            Root = view, Button1 = () => view.Btn1, Button2 = () => view.Btn2, Title = () => view.LabTitle, Line = () => view.ShapeLine,
            Position = () => view.TransformPos, Rotation = () => view.TransformRotate, Input = () => view.TextArea,
            CaptionText = () => view.LabText, TextPanel = () => view.PanText, ApplyRules = () => view.TextArea.ValidateRules = current!.ValidateRules!
        }, () => current, host, () => uuid);
        view.controller = controller; view.Loaded += (_, _) => controller.Load();
        try {
            view.InitializeComponent();
            // LF ValidateChanged is shared. Keep the original subscription lifetime and sender-independent refresh.
            MyTextBoxView.ValidateChanged += (_, _) => controller.ValidateChanged();
            view.Btn1.Click += (_, _) => controller.Button1Click(); view.Btn2.Click += (_, _) => controller.Button2Click();
            view.PanBorder.MouseLeftButtonDown += (_, args) => controller.Drag(args); view.LabTitle.MouseLeftButtonDown += (_, args) => controller.Drag(args);
            controller.RenameButtons(); current = converter; controller.Initialize();
        } catch (Exception error) { controller.InitializationFailed(error); }
        return view;
    }
    public void Accept() => controller!.Button1Click();
    public void Cancel() => controller!.Button2Click();
}
