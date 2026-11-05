using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCLLF.Presentation.Dialogs;

namespace PCL;

public partial class MyMsgSelect : Grid {
    private readonly ModMain.MyMsgBoxConverter MyConverter;
    private readonly int Uuid = ModBase.GetUuid();
    private int SelectedIndex = -1;
    private readonly IMessageDialogController Native;
    public MyMsgSelect(ModMain.MyMsgBoxConverter Converter) {
        Native = new MessageSelectController<ModMain.MyMsgBoxType, Validate>(new MessageSelectParts {
            Root = this, Button1 = () => Btn1, Button2 = () => Btn2, Title = () => LabTitle, Line = () => ShapeLine,
            Position = () => TransformPos, Rotation = () => TransformRotate, Selection = () => PanSelection,
            SelectedIndex = () => SelectedIndex, SetSelectedIndex = value => SelectedIndex = value
        }, () => MyConverter, MessageDialogPlatform.CreateHost(), () => Uuid);
        Loaded += OnViewLoaded;
        try {
            InitializeComponent(); Native.RenameButtons(); MyConverter = Converter; Native.Initialize();
        } catch (Exception error) { Native.InitializationFailed(error); }
    }
    private void OnViewLoaded(object sender, RoutedEventArgs args) => Native.Load();
    public void Btn1_Click() => Native.Button1Click();
    public void Btn2_Click() => Native.Button2Click();
    private void OnButton1Click(object sender, MouseButtonEventArgs args) => Btn1_Click();
    private void OnButton2Click(object sender, MouseButtonEventArgs args) => Btn2_Click();
    private void OnTitleDrag(object sender, MouseButtonEventArgs args) => Native.Drag(args);
}
