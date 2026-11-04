Imports DialogBase = PCLCS.ModpackDialogController(Of PCL.MyButton, PCL.MyScrollViewer)
Imports VersionController = PCLCS.ModpackVersionSelectController(Of PCL.ResourceVersion, PCL.MyListItem, PCL.MyButton, PCL.MyScrollViewer)
Imports PreviewController = PCLCS.ModpackUpdatePreviewController(Of PCL.PackUpdateFile, PCL.PackUpdatePackage, PCL.PackUpdateChange, PCL.PackUpdatePlan, PCL.ModpackUpdatePreviewView.ChangeKind, PCL.MyButton, PCL.MyScrollViewer)

Friend MustInherit Class ModpackDialogView
    Inherits DialogBase
    Friend Shadows ReadOnly Property PreferredWidth As Double = 740
    Friend Shadows ReadOnly Header As StackPanel
    Friend Shadows ReadOnly Filters As Primitives.UniformGrid
    Friend Shadows ReadOnly Rows As StackPanel
    Private ReadOnly Scroll As MyScrollViewer
    Private ReadOnly AnimationName As String
    Protected Sub New(Title As String, Subtitle As String, FilterCount As Integer)
        MyBase.New(Title, Subtitle, FilterCount, New PCLCS.ModpackDialogHost(Of MyButton, MyScrollViewer) With {
            .AnimationName = Function() "Modpack Dialog " & GetUuid(),
            .CreateButton = Function() New MyButton,
            .SetButtonText = Sub(Button, Text) Button.Text = Text,
            .HighlightButton = Sub(Button, Highlight) Button.ColorType = If(Highlight, MyButton.ColorState.Highlight, MyButton.ColorState.Normal),
            .OnClick = Sub(Button, Work) AddHandler Button.Click, Sub() Work(),
            .CreateScroll = Function() New MyScrollViewer,
            .AnimationControl = Function() AniControlEnabled,
            .StopAnimation = Sub(Name) AniStop(Name),
            .AnimateOpacity = Sub(Element, Value, Duration, Name) AniStart(AaOpacity(Element, Value, Duration), Name)})
        Header = MyBase.Header : Filters = MyBase.Filters : Rows = MyBase.Rows
        Scroll = Scroller
        AnimationName = MyBase.AnimationKey
    End Sub
End Class

Friend NotInheritable Class ModpackVersionSelectView
    Inherits ModpackDialogView
    Private ReadOnly Versions As List(Of ResourceVersion)
    Private SelectedVersion As ResourceVersion
    Friend ReadOnly Selections As New List(Of MyListItem)
    Friend Event SelectionChanged As EventHandler
    Private Channel As ResourceVersion.ReleaseTypes
    Private ReadOnly Controller As VersionController
    Friend ReadOnly Property Selected As ResourceVersion
        Get
            Return SelectedVersion
        End Get
    End Property
    Friend Property ActiveChannel As ResourceVersion.ReleaseTypes
        Get
            Return Channel
        End Get
        Private Set(Value As ResourceVersion.ReleaseTypes)
            Channel = Value
        End Set
    End Property
    Friend Sub New(Items As IEnumerable(Of ResourceVersion), ProjectName As String, CurrentVersion As String)
        MyBase.New(ProjectName, "当前版本：" & If(String.IsNullOrWhiteSpace(CurrentVersion), "未记录", CurrentVersion), 3)
        Controller = New VersionController(Me, Items, Selections, New PCLCS.ModpackVersionSelectHost(Of ResourceVersion, MyListItem) With {
            .CreateItem = Function() New MyListItem,
            .Configure = AddressOf ConfigureItem,
            .Checked = Function(Item) Item.Checked,
            .OnCheck = Sub(Item, Work) AddHandler Item.Check, Sub() Work(),
            .OnChanged = Sub(Item, Work) AddHandler Item.Changed, Sub() Work(),
            .Select = Sub(Value) SelectedVersion = Value,
            .SetChannel = Sub(Value) Channel = CType(Value, ResourceVersion.ReleaseTypes),
            .SelectionChanged = Sub() RaiseEvent SelectionChanged(Me, EventArgs.Empty)})
        Versions = Controller.Versions
    End Sub
    Private Shared Sub ConfigureItem(Item As MyListItem, Version As ResourceVersion, Info As String, ToolTip As String)
        Item.Title = Version.Display : Item.Info = Info : Item.Type = MyListItem.CheckType.RadioBox
        Item.Height = 64 : Item.PaddingLeft = 14 : Item.MinPaddingRight = 12 : Item.Margin = New Thickness(3, 0, 7, 8)
        Item.Tag = Version : Item.Background = PCLLF.Presentation.Foundation.LegacyThemePalette.Brush("ColorBrushWhite") : Item.ToolTip = ToolTip
    End Sub
    Private Shared Function ChannelOf(Version As ResourceVersion) As ResourceVersion.ReleaseTypes
        Return CType(VersionController.ChannelOf(Version), ResourceVersion.ReleaseTypes)
    End Function
    Private Shared Function ChannelName(Kind As ResourceVersion.ReleaseTypes) As String
        Return VersionController.ChannelName(CInt(Kind))
    End Function
    Friend Sub ShowChannel(Kind As ResourceVersion.ReleaseTypes)
        Controller.ShowChannel(CInt(Kind))
    End Sub
End Class

Friend NotInheritable Class ModpackUpdatePreviewView
    Inherits ModpackDialogView
    Friend Enum ChangeKind
        Added
        Updated
        Removed
        Preserved
    End Enum
    Private ReadOnly Changes As List(Of PackUpdateChange)
    Friend ReadOnly Counts As New Dictionary(Of ChangeKind, Integer)
    Private Kind As ChangeKind
    Private ReadOnly Controller As PreviewController
    Friend Property ActiveKind As ChangeKind
        Get
            Return Kind
        End Get
        Private Set(Value As ChangeKind)
            Kind = Value
        End Set
    End Property
    Friend Sub New(Plan As PackUpdatePlan, CurrentMinecraft As String, CurrentLoaders As Dictionary(Of ModLoaders, String))
        MyBase.New(Plan.Package.Name & " " & Plan.Package.Version, RuntimeSummary(Plan.Package, CurrentMinecraft, CurrentLoaders), 4)
        Controller = New PreviewController(Me, Plan, CurrentMinecraft, CurrentLoaders, Counts,
            Sub(Value) Kind = CType(Value, ChangeKind),
            Function(Text) New MyHint With {.Text = Text, .Theme = MyHint.Themes.Yellow, .Margin = New Thickness(0, 10, 0, 0)})
        Changes = Controller.Changes
    End Sub
    Friend Shared Function Classify(Change As PackUpdateChange) As ChangeKind
        Return CType(PreviewController.Classify(Change), ChangeKind)
    End Function
    Private Shared Function KindName(Value As ChangeKind) As String
        Return PreviewController.KindName(CInt(Value))
    End Function
    Private Shared Function RuntimeSummary(Package As PackUpdatePackage, Minecraft As String, Loaders As Dictionary(Of ModLoaders, String)) As String
        Return PreviewController.RuntimeSummary(Package, Minecraft, Loaders)
    End Function
    Private Shared Function FolderName(Relative As String) As String
        Return PreviewController.FolderName(Relative)
    End Function
    Friend Sub ShowKind(Value As ChangeKind)
        Controller.ShowKind(CInt(Value))
    End Sub
End Class
