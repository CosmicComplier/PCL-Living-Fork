using System;
using System.Windows;
using System.Windows.Controls;
using PCLLF.Presentation.Shell;

namespace PCLLF.Presentation.Dialogs;

public partial class MyMsgTextView : Grid {
    // The legacy subclass compiles its original PCL-typed child tree; this constructor builds no second tree.
    public MyMsgTextView() { }
    private IMessageTextController? controller;
    public static MyMsgTextView Create<TKind, TValidation>(MainMessageData<TKind, TValidation> converter, MessageDialogHost host) {
        MyMsgTextView view = new(); int uuid = host.NewUuid();
        MainMessageData<TKind, TValidation>? current = null;
        var controller = new MessageTextController<TKind, TValidation>(new MessageTextParts {
            Root = view, Button1 = () => view.Btn1, Button2 = () => view.Btn2, Button3 = () => view.Btn3,
            Title = () => view.LabTitle, Line = () => view.ShapeLine, Position = () => view.TransformPos, Rotation = () => view.TransformRotate,
            Caption = () => view.PanCaption, CaptionText = () => view.LabCaption
        }, () => current, host, () => uuid);
        view.controller = controller;
        view.SizeChanged += (_, _) => controller.RefreshContentBounds();
        view.Loaded += (_, _) => controller.ContentHostLoaded(); view.Unloaded += (_, _) => controller.ContentHostUnloaded();
        view.Loaded += (_, _) => controller.Load();
        try {
            view.InitializeComponent();
            view.Btn1.Click += (_, _) => controller.Button1Click(); view.Btn2.Click += (_, _) => controller.Button2Click(); view.Btn3.Click += (_, _) => controller.Button3Click();
            view.PanBorder.MouseLeftButtonDown += (_, args) => controller.Drag(args); view.LabTitle.MouseLeftButtonDown += (_, args) => controller.Drag(args);
            controller.RenameButtons(); current = converter; controller.Initialize();
        } catch (Exception error) { controller.InitializationFailed(error); }
        return view;
    }
    public void Accept() => controller!.Button1Click();
    public void Cancel() => controller!.Button2Click();
    public void Third() => controller!.Button3Click();
}
