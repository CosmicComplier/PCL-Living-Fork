using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCLLF.Presentation.Dialogs;

namespace PCL;

public partial class MyMsgInput : Grid {
    private readonly ModMain.MyMsgBoxConverter MyConverter;
    private readonly int Uuid = ModBase.GetUuid();
    private readonly IMessageInputController Native;
    public MyMsgInput(ModMain.MyMsgBoxConverter Converter) {
        Native = new MessageInputController<ModMain.MyMsgBoxType, Validate>(new MessageInputParts {
            Root = this, Button1 = () => Btn1, Button2 = () => Btn2, Title = () => LabTitle, Line = () => ShapeLine,
            Position = () => TransformPos, Rotation = () => TransformRotate, Input = () => TextArea,
            TextPanel = () => PanText, CaptionText = () => LabText, ApplyRules = () => TextArea.ValidateRules = MyConverter.ValidateRules
        }, () => MyConverter, MessageDialogPlatform.CreateHost(), () => Uuid);
        Loaded += OnViewLoaded;
        try {
            InitializeComponent();
            // The projected original TextArea WithEvents setter attaches the real shared event during Connect.
            Native.RenameButtons(); MyConverter = Converter; Native.Initialize();
        } catch (Exception error) { Native.InitializationFailed(error); }
    }
    private void OnViewLoaded(object sender, RoutedEventArgs args) => Native.Load();
    public void Btn1_Click() => Native.Button1Click();
    public void Btn2_Click() => Native.Button2Click();
    private void OnButton1Click(object sender, MouseButtonEventArgs args) => Btn1_Click();
    private void OnButton2Click(object sender, MouseButtonEventArgs args) => Btn2_Click();
    private void OnTextValidationChanged(object sender, EventArgs args) => Native.ValidateChanged();
    private void OnTitleDrag(object sender, MouseButtonEventArgs args) => Native.Drag(args);
}
