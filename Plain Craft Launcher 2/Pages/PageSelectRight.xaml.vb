Imports CloneSelection = PCLCS.InstanceCloneSelectionPresenter(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode, PCL.InstanceCloneGroupSet)
Imports CloneSelectionHost = PCLCS.InstanceCloneSelectionHost(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode)

Public Class PageSelectRight
    Private Shared ReadOnly ClonePresenter As New CloneSelection(InstanceCloneGroups.Service, CreateCloneHost())

    '窗口基础
    Private Sub PageSelectRight_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
        PanBack.ScrollToHome()
    End Sub
    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, McInstanceListLoader, AddressOf McInstanceListUI, AutoRun:=False)
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If McInstanceListLoader.State = LoadState.Failed Then
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        End If
    End Sub

    '窗口属性
    ''' <summary>
    ''' 是否显示隐藏的 Minecraft 版本。
    ''' </summary>
    Public ShowHidden As Boolean = False

#Region "结果 UI 化"

    Private Shared Function CreateCloneHost() As CloneSelectionHost
        Return New CloneSelectionHost With {
            .HiddenCategory = McInstanceCardType.Hidden, .StarCategory = McInstanceCardType.Star,
            .IsCollapsedCategory = Function(Kind) Kind = McInstanceCardType.Rubbish OrElse Kind = McInstanceCardType.Error OrElse Kind = McInstanceCardType.Fool,
            .IsLocked = Function(Instance) McInstanceLock.IsLocked(Instance),
            .AllowsInstance = Function(Instance) ModModpackMode.AllowsInstance(Instance.PathVersion),
            .VersionPath = Function(Instance) Instance.PathVersion, .Name = Function(Instance) Instance.Name,
            .Info = Function(Instance) Instance.Info, .SelectedPath = Function() McInstanceSelected?.PathVersion,
            .CreateRow = Function(Instance, Initialized) Instance.ToListItem(Sub(Item, Args) Initialized(Item, Args)),
            .InitializeRow = Sub(Item, Args) McInstanceListContent(DirectCast(Item, MyListItem), Args),
            .SetRowInfo = Sub(Item, Info) DirectCast(Item, MyListItem).Info = Info,
            .CreateCard = Function(Title, Margin) New CloneCard(New MyCard With {.Title = Title, .Margin = Margin}),
            .AdaptCard = Function(Element) New CloneCard(DirectCast(Element, MyCard)), .SwappedHeight = MyCard.SwapedHeight}
    End Function
    Private NotInheritable Class CloneCard
        Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode)
        Private ReadOnly Card As MyCard
        Public Sub New(Card As MyCard)
            Me.Card = Card
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).Element
            Get
                Return Card
            End Get
        End Property
        Public Property IsSwapped As Boolean Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).IsSwapped
            Get
                Return Card.IsSwapped
            End Get
            Set(Value As Boolean)
                Card.IsSwapped = Value
            End Set
        End Property
        Public Sub SetContent(Content As Panel) Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).SetContent
            Card.Children.Add(Content)
            Card.SwapControl = Content
        End Sub
        Public Sub SetContentBuilder(Items As IEnumerable(Of InstanceCloneGroupNode), Builder As Func(Of InstanceCloneGroupNode, FrameworkElement)) Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).SetContentBuilder
            Card.SetContentBuilder(Items, Function(Node) DirectCast(Builder(Node), UIElement))
        End Sub
        Public Sub BuildContent() Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).BuildContent
            Card.BuildContent()
        End Sub
    End Class

    Private Sub McInstanceListUI(Loader As LoaderTask(Of String, Integer))
        Try
            '加载 UI
            PanMain.Children.Clear()
            Dim Grouping = ClonePresenter.Prepare(McInstanceList, Function() ShowHidden)
            For Each Card In Grouping.VisibleCards
                Dim Roots = Grouping.Roots(Card.Key, Card.Value)
                If Roots.Count = 0 Then Continue For
                Dim Instances = Grouping.Instances(Roots)
#Region "确认卡片名称"
                Dim CardName As String = ""
                Select Case Card.Key
                    Case McInstanceCardType.OriginalLike
                        CardName = "常规版本"
                    Case McInstanceCardType.API
                        Dim IsForgeExists As Boolean = False
                        Dim IsNeoForgeExists As Boolean = False
                        Dim IsFabricExists As Boolean = False
                        Dim IsLiteExists As Boolean = False
                        For Each Instance As McInstance In Instances
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Fabric) Then IsFabricExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.LiteLoader) Then IsLiteExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Forge) Then IsForgeExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge) Then IsNeoForgeExists = True
                        Next
                        If If(IsLiteExists, 1, 0) + If(IsForgeExists, 1, 0) + If(IsFabricExists, 1, 0) + If(IsNeoForgeExists, 1, 0) > 1 Then
                            CardName = "可安装 Mod"
                        ElseIf IsForgeExists Then
                            CardName = "Forge 版本"
                        ElseIf IsNeoForgeExists Then
                            CardName = "NeoForge 版本"
                        ElseIf IsLiteExists Then
                            CardName = "LiteLoader 版本"
                        Else
                            CardName = "Fabric 版本"
                        End If
                    Case McInstanceCardType.Error
                        CardName = "错误的版本"
                    Case McInstanceCardType.Hidden
                        CardName = "隐藏的版本"
                    Case McInstanceCardType.Rubbish
                        CardName = "不常用版本"
                    Case McInstanceCardType.Star
                        CardName = "收藏夹"
                    Case McInstanceCardType.Fool
                        CardName = "愚人节版本"
                    Case Else
                        Throw New ArgumentException("未知的卡片种类（" & Card.Key & "）")
                End Select
#End Region
                '建立控件
                ClonePresenter.RenderCategory(PanMain, Card.Key, CardName, Roots, Instances.Count, Grouping)
            Next

            '若只有一个卡片，则强制展开
            ClonePresenter.ExpandSingle(PanMain)

            '判断应该显示哪一个页面
            If PanMain.Children.Count = 0 Then
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                If ShowHidden Then
                    LabEmptyTitle.Text = "无隐藏版本"
                    LabEmptyContent.Text = "没有版本被隐藏，你可以在版本设置的版本分类选项中隐藏版本。" & vbCrLf & "再次按下 F11 即可退出隐藏版本查看模式。"
                    BtnEmptyDownload.Visibility = Visibility.Collapsed
                Else
                    LabEmptyTitle.Text = "无可用版本"
                    LabEmptyContent.Text = "未找到任何版本的游戏，请先下载任意版本的游戏。" & vbCrLf & "若有已存在的游戏，请在左边的列表中选择添加文件夹，选择 .minecraft 文件夹将其导入。"
                    If ModModpackMode.Enabled Then LabEmptyContent.Text = "当前整合包目录中没有可用的游戏版本。请确认整合包已完整解压，或重新运行附带安装文件的启动器完成安装。"
                    BtnEmptyDownload.Visibility = If(ModModpackMode.Enabled OrElse (Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Not PageSetupUI.HiddenForceShow), Visibility.Collapsed, Visibility.Visible)
                End If
            Else
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
            End If

        Catch ex As Exception
            Logger.Error(ex, "将版本列表转换显示时失败")
        End Try
    End Sub
    '来源行始终可选；只有下方测试副本使用原生折叠卡片和惰性列表项。
    Friend Shared Function BuildCloneChoice(Node As InstanceCloneGroupNode, InstancesByPath As Dictionary(Of String, McInstance), SelectedPath As String) As FrameworkElement
        Return ClonePresenter.BuildCloneChoice(Node, InstancesByPath, SelectedPath)
    End Function
    Public Shared Sub McInstanceListContent(sender As MyListItem, e As EventArgs)
        Dim Instance As McInstance = sender.Tag
        '注册点击事件
        AddHandler sender.Click, AddressOf Item_Click
        '图标按钮
        Dim BtnStar As New MyIconButton
        BtnStar.Visibility = If(ModModpackMode.Enabled, Visibility.Collapsed, Visibility.Visible)
        If Instance.IsStar Then
            BtnStar.ToolTip = "取消收藏"
            BtnStar.LogoScale = 1.1
            BtnStar.Logo = Logo.IconButtonLikeFill
        Else
            BtnStar.ToolTip = "收藏"
            BtnStar.LogoScale = 1.1
            BtnStar.Logo = Logo.IconButtonLikeLine
        End If
        AddHandler BtnStar.Click,
        Sub()
            WriteIni(Instance.PathVersion & "PCL\Setup.ini", "IsStar", Not Instance.IsStar)
            McInstanceListForceRefresh = True
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        End Sub
        Dim BtnDel As New MyIconButton With {.LogoScale = 1.1, .Logo = Logo.IconButtonDelete}
        BtnDel.Visibility = If(ModModpackMode.Enabled, Visibility.Collapsed, Visibility.Visible)
        BtnDel.ToolTip = "删除"
        AddHandler BtnDel.Click, Sub() DeleteInstance(sender, Instance)
        If Instance.State <> McInstance.McInstanceState.Error Then
            Dim BtnCont As New MyIconButton With {.LogoScale = 1.1, .Logo = Logo.IconButtonSetup}
            BtnCont.ToolTip = "设置"
            AddHandler BtnCont.Click,
            Sub()
                PageInstanceLeft.Instance = Instance
                FrmMain.PageChange(FormMain.PageType.InstanceSetup, 0)
            End Sub
            AddHandler sender.MouseRightButtonUp,
            Sub()
                PageInstanceLeft.Instance = Instance
                FrmMain.PageChange(FormMain.PageType.InstanceSetup, 0)
            End Sub
            sender.Buttons = {BtnStar, BtnDel, BtnCont}
        Else
            Dim BtnCont As New MyIconButton With {.LogoScale = 1.15, .Logo = Logo.IconButtonOpen}
            BtnCont.ToolTip = "打开文件夹"
            AddHandler BtnCont.Click, Sub() PageInstanceOverall.OpenInstanceFolder(Instance)
            AddHandler sender.MouseRightButtonUp, Sub() PageInstanceOverall.OpenInstanceFolder(Instance)
            sender.Buttons = {BtnStar, BtnDel, BtnCont}
        End If
    End Sub

#End Region

#Region "页面事件"

    '点击选项
    Public Shared Sub Item_Click(sender As MyListItem, e As EventArgs)
        Dim Instance As McInstance = sender.Tag
        If Not ModModpackMode.AllowsInstance(Instance.PathVersion) Then Return
        If McInstanceLock.IsLocked(Instance) Then
            Hint("该版本正在被修改，请稍后再选择！", HintType.Red)
            Return
        End If
        If New McInstance(Instance.PathVersion).Check Then
            '正常版本
            McInstanceSelected = Instance
            FrmMain.PageBack()
        Else
            '错误版本
            PageInstanceOverall.OpenInstanceFolder(Instance)
        End If
    End Sub

    Private Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles BtnEmptyDownload.Click
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
    End Sub

    '修改此代码时，同时修改 PageInstanceOverall 中的代码
    Public Shared Sub DeleteInstance(Item As MyListItem, Instance As McInstance)
        If ModModpackMode.Enabled Then Return
        Try
            Instance = ResolveDeleteTarget(Item, Instance)
            If McInstanceLock.IsLocked(Instance) Then
                Hint("该版本正在被修改，请稍后再删除！", HintType.Red)
                Return
            End If
            Dim IsShiftPressed As Boolean = My.Computer.Keyboard.ShiftKeyDown
            Dim IsHintIndie As Boolean = Instance.State <> McInstance.McInstanceState.Error AndAlso Instance.PathIndie <> McFolderSelected
            Dim SavesFolder = DirectoryUtils.GetInfo(Instance.PathIndie & "saves\")
            Dim SaveEntries = If(IsHintIndie AndAlso SavesFolder.Exists, SavesFolder.EnumerateDirectories.OrderByDescending(Function(Save) Save.LastWriteTime).Select(Function(Save) $"{Save.Name}（上次修改：{StringUtils.FormatTimeSpan(Save.LastWriteTime - Date.Now, False)}）").ToList, New List(Of String))
            Select Case MyMsgBox($"你确定要{If(IsShiftPressed, "永久", "")}删除版本 {Instance.Name} 吗？" &
                        If(IsHintIndie, vbCrLf & "该版本对应的存档、资源包、Mod 等文件也将被一并删除！", "") &
                        If(SaveEntries.Any, vbCrLf & vbCrLf & "这会删除以下存档：" & vbCrLf & "· " & SaveEntries.Join(vbCrLf & "· "), ""),
                        "版本删除确认", , "取消",, True)
                Case 1
                    Instance.ResetSettingsCache()
                    DirectoryUtils.Delete(Instance.PathVersion, Not IsShiftPressed)
                    Hint("版本 " & Instance.Name & " 已删除！", HintType.Green)
                Case 2
                    Return
                Case Else
                    Return
            End Select
            '嵌套卡片、惰性项和收藏夹都从模型刷新；不从控件父级推断版本或删除范围。
            McInstanceList = WithoutDeletedInstance(McInstanceList, Instance.PathVersion)
            Dim NextSelection = SelectionAfterDelete(McInstanceSelected, Instance.PathVersion)
            If NextSelection IsNot McInstanceSelected Then McInstanceSelected = NextSelection
            If PageInstanceLeft.Instance IsNot Nothing AndAlso PathUtils.IsSame(PageInstanceLeft.Instance.PathVersion, Instance.PathVersion) Then PageInstanceLeft.Instance = Nothing
            McInstanceListForceRefresh = True
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        Catch ex As OperationCanceledException
            Logger.Warn(ex, $"删除版本 {If(Instance?.Name, "未知版本")} 被主动取消")
        Catch ex As Exception
            Logger.Error(ex, $"删除版本 {If(Instance?.Name, "未知版本")} 失败", LogBehavior.Alert)
        End Try
    End Sub
    Friend Shared Function ResolveDeleteTarget(Item As MyListItem, Expected As McInstance) As McInstance
        Dim Actual = If(Item Is Nothing, Nothing, TryCast(Item.Tag, McInstance))
        If Actual Is Nothing OrElse Expected Is Nothing OrElse Not PathUtils.IsSame(Actual.PathVersion, Expected.PathVersion) Then Throw New IOException("版本列表已变化，请刷新后重新选择要删除的版本。")
        Return Actual
    End Function
    Friend Shared Function WithoutDeletedInstance(Cards As IDictionary(Of McInstanceCardType, List(Of McInstance)), DeletedFolder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Dim Result As New Dictionary(Of McInstanceCardType, List(Of McInstance))
        For Each Card In Cards
            Dim Remaining = Card.Value.Where(Function(Instance) Not PathUtils.IsSame(Instance.PathVersion, DeletedFolder)).ToList()
            If Remaining.Count > 0 Then Result.Add(Card.Key, Remaining)
        Next
        Return Result
    End Function
    Friend Shared Function SelectionAfterDelete(Current As McInstance, DeletedFolder As String) As McInstance
        Return If(Current IsNot Nothing AndAlso PathUtils.IsSame(Current.PathVersion, DeletedFolder), Nothing, Current)
    End Function

    Public Sub BtnEmptyDownload_Loaded() Handles BtnEmptyDownload.Loaded
        Dim NewVisibility = If((Settings.Get(Of Boolean)("UiHiddenPageDownload") AndAlso Not PageSetupUI.HiddenForceShow) OrElse ShowHidden, Visibility.Collapsed, Visibility.Visible)
        If BtnEmptyDownload.Visibility <> NewVisibility Then
            BtnEmptyDownload.Visibility = NewVisibility
            PanLoad.TriggerForceResize()
        End If
    End Sub

#End Region

End Class
