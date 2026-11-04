using System;
using System.Windows.Controls;
using PCLLF.Presentation.Shell;

namespace PCLLF.Presentation.Dialogs;

public partial class MyMsgSelectView : Grid {
    public MyMsgSelectView() { }
    private IMessageDialogController? controller;
    public static MyMsgSelectView Create<TKind, TValidation>(MainMessageData<TKind, TValidation> converter, MessageDialogHost host) {
        MyMsgSelectView view = new(); int uuid = host.NewUuid(); int selectedIndex = -1;
        MainMessageData<TKind, TValidation>? current = null;
        var controller = new MessageSelectController<TKind, TValidation>(new MessageSelectParts {
            Root = view, Button1 = () => view.Btn1, Button2 = () => view.Btn2, Title = () => view.LabTitle, Line = () => view.ShapeLine,
            Position = () => view.TransformPos, Rotation = () => view.TransformRotate, Selection = () => view.PanSelection,
            SelectedIndex = () => selectedIndex, SetSelectedIndex = value => selectedIndex = value
        }, () => current, host, () => uuid);
        view.controller = controller; view.Loaded += (_, _) => controller.Load();
        try {
            view.InitializeComponent();
            view.Btn1.Click += (_, _) => controller.Button1Click(); view.Btn2.Click += (_, _) => controller.Button2Click();
            view.PanBorder.MouseLeftButtonDown += (_, args) => controller.Drag(args); view.LabTitle.MouseLeftButtonDown += (_, args) => controller.Drag(args);
            controller.RenameButtons(); current = converter; controller.Initialize();
        } catch (Exception error) { controller.InitializationFailed(error); }
        return view;
    }
    public void Accept() => controller!.Button1Click();
    public void Cancel() => controller!.Button2Click();
}
