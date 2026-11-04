Imports FolderCore = PCLCS.FolderSelectionPageController(Of PCL.ModMinecraft.McFolder, PCL.ModMinecraft.McFolder.Types, PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType)

Public Class PageSelectLeft
    Implements IRefreshable
    Private Controller As FolderCore
    Private ReadOnly Property Service As FolderCore
        Get
            If Controller Is Nothing Then Controller = New FolderCore(CreateHost(Me))
            Return Controller
        End Get
    End Property
    Private Shared Function CreateHost(Owner As PageSelectLeft) As PCLCS.FolderSelectionPageHost(Of McFolder, McInstance)
        Return New PCLCS.FolderSelectionPageHost(Of McFolder, McInstance) With {
            .Panel = Function() FrmSelectLeft.PanList, .OwnerPanel = Function() Owner.PanList, .Folders = Function() McFolderList,
            .ModpackEnabled = Function() ModModpackMode.Enabled, .CurrentPageExists = Function() FrmSelectLeft IsNot Nothing,
            .AnyLocked = Function() McInstanceLock.IsAnyLocked, .Downloading = Function(IncludeHidden) HasDownloadingTask(IncludeHidden),
            .PackFolder = Function() ModModpackMode.CreatePackFolderList()(0), .BaseFolder = Function() Paths.Base,
            .SelectedFolder = Function() McFolderSelected, .SetSelectedFolder = Sub(Value) McFolderSelected = Value,
            .ReadFolders = Function() Settings.Get(Of String)("LaunchFolders"), .WriteFolders = Sub(Value) Settings.Set("LaunchFolders", Value),
            .SubscribePreviewFinish = Sub(Finish) AddHandler McFolderListLoader.PreviewFinish, Sub() Finish(),
            .UiWait = Sub(Work) RunInUiWait(Sub() Work()), .RunThread = Sub(Work) RunInThread(Sub() Work()),
            .RunNewThread = Sub(Work, Name, Priority) RunInNewThread(Sub() Work(), Name, Priority),
            .RefreshFolders = Sub() McFolderListLoader.Start(IsForceRestart:=True), .RefreshInstances = AddressOf RefreshNativeInstances,
            .Permission = Function(Path) CheckPermission(Path), .SelectFolder = Function(Title, Multi) Dialogs.SelectFolder(Title, Multi),
            .Input = AddressOf NativeInput, .Message = Function(Request) MyMsgBox(Request.Text, Request.Title, Request.Button1, Request.Button2, Request.Button3, Request.IsWarn),
            .Hint = AddressOf NativeHint, .ParseMenu = Function(Markup) DirectCast(GetObjectFromXML(Markup), ContextMenu), .BindMenu = AddressOf BindNativeMenu,
            .FolderRow = Function(Folder, Menu) New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.RadioBox,
                .MinPaddingRight = 30, .Title = Folder.Name, .Info = Folder.Location, .Height = 40, .ContextMenu = Menu, .Tag = Folder},
            .ActionRow = AddressOf CreateActionRow, .ConfigureButton = AddressOf CreateConfigureButton,
            .SetButtons = Sub(Row, Buttons) DirectCast(Row, MyListItem).Buttons = Buttons.Cast(Of MyIconButton).ToArray(),
            .BindChanged = Sub(Row, Changed) AddHandler DirectCast(Row, MyListItem).Changed, Sub(Sender, Args) Changed(Sender, Args.RaiseByMouse, Sub() Args.Handled = True),
            .BindClick = Sub(Row, Click) AddHandler DirectCast(Row, MyListItem).Click, Sub() Click(),
            .CheckRow = AddressOf CheckNativeRow, .RowChecked = Function(Row) DirectCast(Row, MyListItem).Checked,
            .MenuRow = AddressOf NativeMenuRow, .RowInfo = Function(Row) DirectCast(Row, MyListItem).Info,
            .InstallPack = Sub() ModpackInstall(), .NewInstance = Function(Path) New McInstance(Path),
            .WriteInstanceInteger = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .WriteInstanceBoolean = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .CreateProfiles = AddressOf McFolderLauncherProfilesJsonCreate, .OpenExplorer = Sub(Path) OpenExplorer(Path),
            .WriteIni = Sub(File, Key, Value) WriteIni(File, Key, Value), .NextUuid = AddressOf GetUuid}
    End Function
    Private Shared Sub RefreshNativeInstances(Force As Boolean)
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, If(Force, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated), MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    Private Shared Sub NativeHint(Text As String, Kind As PCLCS.SelectionPageHint)
        Select Case Kind
            Case PCLCS.SelectionPageHint.Red : Hint(Text, HintType.Red)
            Case PCLCS.SelectionPageHint.Blue : Hint(Text, HintType.Blue)
            Case Else : Hint(Text, HintType.Green)
        End Select
    End Sub
    Private Shared Function NativeInput(Request As PCLCS.SelectionPageInput) As String
        Dim Rules As New ObjectModel.Collection(Of Validate)
        If Request.DeleteConfirmation Then
            Rules.Add(New ValidateSame("Potato", $"请输入 {vbLQ}Potato{vbRQ}", IgnoreCase:=True))
        Else
            Rules.Add(New ValidateNullOrWhiteSpace)
            Rules.Add(New ValidateLength(1, 30))
            Rules.Add(New ValidateExcept({">", "|"}))
        End If
        Return MyMsgBoxInput(Request.Title, Request.Caption, Request.Default, Rules, Button1:=Request.Button1, Button2:=Request.Button2, IsWarn:=Request.IsWarn)
    End Function
    Private Shared Sub BindNativeMenu(Menu As ContextMenu, Name As String)
        Select Case Name
            Case "Remove" : DirectCast(Menu.FindName(Name), MyMenuItem).AddHandler(MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmSelectLeft.Remove_Click))
            Case "Open" : DirectCast(Menu.FindName(Name), MyMenuItem).AddHandler(MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmSelectLeft.Open_Click))
            Case "Delete" : DirectCast(Menu.FindName(Name), MyMenuItem).AddHandler(MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmSelectLeft.Delete_Click))
            Case "Rename" : DirectCast(Menu.FindName(Name), MyMenuItem).AddHandler(MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmSelectLeft.Rename_Click))
            Case "Refresh" : DirectCast(Menu.FindName(Name), MyMenuItem).AddHandler(MyMenuItem.ClickEvent, New RoutedEventHandler(AddressOf FrmSelectLeft.Refresh_Click))
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Name))
        End Select
    End Sub
    Private Shared Function NativeMenuRow(Sender As Object) As FrameworkElement
        Return CType(CType(CType(Sender.Parent, ContextMenu).Parent, Primitives.Popup).PlacementTarget, MyListItem)
    End Function
    Private Shared Sub CheckNativeRow(Row As FrameworkElement, Checked As Boolean, Silent As Boolean)
        If Silent Then
            DirectCast(Row, MyListItem).SetChecked(Checked, False, False)
        Else
            DirectCast(Row, MyListItem).Checked = Checked
        End If
    End Sub
    Private Shared Function CreateConfigureButton(Click As Action) As FrameworkElement
        Dim Button As New MyIconButton With {.Logo = Logo.IconButtonSetup, .LogoScale = 1.1}
        AddHandler Button.Click, Sub() Click()
        Return Button
    End Function
    Private Shared Function CreateActionRow(Kind As Integer) As FrameworkElement
        Select Case Kind
            Case 0
                Return New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Title = "新建 .minecraft 文件夹", .Height = 34,
                    .ToolTip = "在 PCL 当前所在文件夹下创建新的 .minecraft 文件夹",
                    .LogoScale = 0.9,
                    .Logo = "M103.331925 384.978025l25.805736 0L129.137661 161.847132c0-18.313088 14.905478-33.718963 33.718963-33.718963l0.969071 0 253.006318 0c10.82044 0 20.218484 4.797259 26.500561 12.257162l117.579929 126.753869 297.819966 0c18.297738 0 33.736359 15.179724 33.736359 33.977859l0 0.952698 0 82.909292 25.547863 0c18.538215 0 34.187637 15.179724 34.187637 33.977859 0 2.163269-0.469698 3.617387-0.469698 5.539156l-54.437843 432.971086c-1.210571 10.382465-7.007601 19.056008-14.968923 24.352641-6.249331 5.765307-14.680351 9.624195-23.595394 9.624195l-0.969071 0-694.906773 0c-9.155521 0-17.344017-3.858888-23.626094-9.155521-8.67252-5.765307-14.453177-14.939247-15.389502-25.758664L69.597613 423.040922c-2.165316-18.313088 10.868535-35.414581 29.665647-38.062897L103.331925 384.978025 103.331925 384.978025zM196.576609 384.978025 196.576609 384.978025l627.938546 0 0-49.625234L546.461371 335.352791l0 0c-9.400091 0-18.329461-4.117784-25.048489-11.110035L402.363486 196.067514 196.576609 196.067514 196.576609 384.978025 196.576609 384.978025zM879.469767 452.916347 879.469767 452.916347l-20.267603 0-0.469698 0-0.969071 0-694.906773 0-0.984421 0-20.218484 0 45.781696 366.728382 646.218888 0L879.469767 452.916347 879.469767 452.916347z"}
            Case 1
                Return New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Title = "添加已有文件夹", .Height = 34,
                .ToolTip = "将一个已有的 Minecraft 文件夹添加到列表",
                .Logo = "M512.277 954.412c-118.89 0-230.659-46.078-314.73-129.73S67.12 629.666 67.12 511.222s46.327-229.744 130.398-313.427 195.82-129.73 314.73-129.73 230.659 46.078 314.72 129.73S957.397 392.81 957.397 511.183 911.078 740.96 826.97 824.642s-195.8 129.77-314.692 129.77z m0-822.784c-101.972 0-197.809 39.494-269.865 111.222s-111.7 166.997-111.7 268.373 39.653 196.695 111.67 268.335S410.246 890.78 512.248 890.78s197.809-39.484 269.865-111.222 111.7-166.998 111.67-268.374c-0.03-101.375-39.654-196.665-111.67-268.303S614.22 131.628 512.277 131.628z m222.585 347.8H544.073V288.64c-0.76-17.561-15.613-31.18-33.173-30.419-16.495 0.714-29.704 13.924-30.419 30.419v190.787H289.703c-17.56 0.761-31.179 15.614-30.419 33.174 0.715 16.494 13.924 29.703 30.42 30.418H480.48v190.788c0.761 17.56 15.614 31.179 33.174 30.419 16.494-0.715 29.703-13.925 30.418-30.42V543.02h190.788c17.56 0.762 32.413-12.857 33.173-30.418 0.762-17.561-12.858-32.414-30.419-33.174a31.683 31.683 0 0 0-2.753 0z"}
            Case 2
                Return New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Title = "导入整合包", .Height = 34,
                .ToolTip = "在当前选择的 Minecraft 文件夹下安装整合包",
                .Logo = "M512 40.96C249.344 40.96 35.84 252.416 35.84 512s213.504 471.04 476.16 471.04c103.424 0 202.752-33.28 286.72-96.256l1.536-1.536c5.12-5.632 7.68-12.8 7.68-19.968 0-16.896-13.824-30.208-30.72-30.208-7.68 0-15.36 2.56-20.992 7.68h-0.512c-71.68 52.224-155.648 79.36-243.712 79.36-227.328 0-412.16-182.784-412.16-407.552 0-224.768 184.832-407.552 412.16-407.552s412.16 182.784 412.16 407.552c0 68.608-15.872 132.608-46.592 190.464-0.512 1.024-1.024 2.048-1.024 3.072-0.512 2.048-1.536 4.608-1.536 8.192 0 16.896 13.824 30.208 30.72 30.208 12.288 0 23.04-7.168 28.16-18.432 35.84-68.608 53.76-141.312 53.76-216.064 0.512-259.584-212.992-471.04-475.648-471.04z M812.032 483.328c-31.744-20.992-71.68 1.536-78.848 6.144-1.024 0.512-104.448 61.44-128 74.752-8.192 4.608-22.528-0.512-27.136-4.096-31.232-36.352-54.272-70.656-68.608-102.4-13.312-29.184 0.512-41.472 3.072-43.52 7.168-4.608 114.688-68.608 143.36-83.456 24.064-12.288 40.96-25.088 46.08-45.056 3.072-13.312 0-27.136-9.216-39.936-22.016-31.744-172.544-84.992-311.296-3.584-157.184 91.648-152.064 242.688-150.528 292.352v9.216c0 18.944-12.8 37.376-14.848 40.448l-20.992 21.504c-6.144 6.144-9.216 13.824-9.216 22.528 0 8.704 3.584 16.384 9.728 22.528 12.8 12.288 32.768 11.776 45.056-0.512l22.528-23.552 0.512-0.512c3.072-3.584 30.208-38.4 30.208-81.92l-0.512-11.264c-1.536-44.544-5.632-162.816 119.296-235.52 88.064-51.2 173.056-32.256 208.896-19.968-36.864 19.456-143.36 83.456-144.896 84.48-22.016 14.336-55.808 58.88-26.112 122.88 17.408 37.376 43.52 76.8 80.896 120.32 14.336 17.408 62.976 37.376 103.424 15.36 24.576-13.312 125.44-73.216 130.048-75.776 2.048-1.024 4.608-2.56 7.68-3.584 0 2.56-0.512 6.144-1.024 10.752-5.632 35.84-35.328 155.136-191.488 181.76-49.664 8.704-89.6 3.584-121.856-0.512h-0.512c-37.888-4.608-73.216-9.216-101.888 14.336-31.232 26.112-40.96 34.304-35.84 54.272 3.584 14.336 16.384 24.064 30.72 24.064 2.56 0 5.12-0.512 7.68-1.024 6.656-1.536 12.8-5.632 16.896-10.752 2.048-2.048 7.68-6.656 20.992-18.432 6.656-5.632 25.088-3.584 52.736 0 34.816 4.608 81.92 10.24 141.312 0.512 157.184-26.624 228.864-138.752 243.2-234.496 7.68-38.912 0-64.512-21.504-78.336z"}
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Sub PageSelectLeft_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        Service.Initialize()
    End Sub
    Private Sub PageSelectLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Service.Loaded()
    End Sub
    Private Sub McFolderListUI()
        Service.Render()
    End Sub
    Friend Sub RefreshForModpackMode()
        Service.RefreshForModpackMode()
    End Sub
    Friend Sub RenderPackFolderList()
        Service.RenderPackFolderList()
    End Sub
    Public Sub Add_Click()
        FolderCore.Add(CreateHost(Nothing))
    End Sub
    Public Shared Sub AddFolder(FolderPath As String, DisplayName As String, ShowHint As Boolean)
        FolderCore.AddFolder(FolderPath, DisplayName, ShowHint, CreateHost(Nothing))
    End Sub
    Public Sub Create_Click()
        FolderCore.Create(CreateHost(Nothing))
    End Sub
    Public Shared Sub CreateMcFolderInCurrentPath()
        FolderCore.CreateInCurrentPath(CreateHost(Nothing))
    End Sub
    Public Sub Remove_Click(sender As Object, e As RoutedEventArgs)
        FolderCore.Remove(sender, CreateHost(Nothing))
    End Sub
    Public Sub Delete_Click(sender As Object, e As RoutedEventArgs)
        FolderCore.Delete(sender, CreateHost(Nothing))
    End Sub
    Private Shared Function RemoveFolderFromSetup(Target As McFolder) As String
        Return FolderCore.RemoveFromSetup(Target, CreateHost(Nothing))
    End Function
    Public Sub Open_Click(sender As Object, e As RoutedEventArgs)
        FolderCore.Open(sender, CreateHost(Nothing))
    End Sub
    Public Sub Refresh_Click(sender As Object, e As RoutedEventArgs)
        FolderCore.RefreshClick(sender, CreateHost(Nothing))
    End Sub
    Public Sub RefreshCurrent() Implements IRefreshable.Refresh
        RefreshCurrent(McFolderSelected)
    End Sub
    Public Shared Sub RefreshCurrent(Folder As String)
        FolderCore.Refresh(Folder, CreateHost(Nothing))
    End Sub
    Public Sub Rename_Click(sender As Object, e As RoutedEventArgs)
        FolderCore.Rename(sender, CreateHost(Nothing))
    End Sub
    Public Sub Folder_Change(sender As MyListItem, e As RouteEventArgs)
        FolderCore.ChangeFolder(sender, e.RaiseByMouse, Sub() e.Handled = True, CreateHost(Nothing))
    End Sub
End Class
