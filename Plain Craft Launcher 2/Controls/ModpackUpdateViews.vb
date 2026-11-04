Imports System.IO

'整合包弹窗共用原生按钮、列表选择与滚动动效，文件操作仍由更新事务处理。
Friend MustInherit Class ModpackDialogView
    Inherits Grid

    Friend ReadOnly Property PreferredWidth As Double = 740
    Friend ReadOnly Header As New StackPanel
    Friend ReadOnly Filters As New Primitives.UniformGrid
    Friend ReadOnly Rows As New StackPanel
    Private ReadOnly Scroll As MyScrollViewer
    Private ReadOnly AnimationName As String = "Modpack Dialog " & GetUuid()

    Protected Sub New(Title As String, Subtitle As String, FilterCount As Integer)
        Height = 420
        RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
        RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
        RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(1, GridUnitType.Star)})
        Header.Margin = New Thickness(3, 0, 7, 0)
        Header.Children.Add(Label(Title, 15, True))
        Header.Children.Add(Label(Subtitle, 12, False, "ColorBrushGray2", New Thickness(0, 5, 0, 0)))
        Children.Add(Header)
        Filters.Columns = FilterCount : Filters.Margin = New Thickness(0, 13, 4, 7)
        AddHandler Filters.SizeChanged, Sub()
                                            Filters.Columns = If(FilterCount = 4 AndAlso Filters.ActualWidth < 580, 2, If(FilterCount = 3 AndAlso Filters.ActualWidth < 430, 1, FilterCount))
                                        End Sub
        Grid.SetRow(Filters, 1) : Children.Add(Filters)
        Scroll = New MyScrollViewer With {.Content = Rows, .VerticalScrollBarVisibility = ScrollBarVisibility.Auto, .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled}
        Grid.SetRow(Scroll, 2) : Children.Add(Scroll)
        AddHandler Unloaded, Sub() AniStop(AnimationName)
    End Sub

    Protected Sub AddFilter(Text As String, Key As Integer, Enabled As Boolean, SelectFilter As Action(Of Integer))
        Dim Button As New MyButton With {.Text = Text, .Tag = Key, .Height = 34, .Margin = New Thickness(3, 0, 3, 6), .IsEnabled = Enabled}
        AddHandler Button.Click, Sub() SelectFilter(Key)
        Filters.Children.Add(Button)
    End Sub

    Protected Sub BeginFilter(Key As Integer)
        For Each Button As MyButton In Filters.Children
            Button.ColorType = If(CInt(Button.Tag) = Key, MyButton.ColorState.Highlight, MyButton.ColorState.Normal)
        Next
        Rows.Children.Clear()
        Scroll.ScrollToTop()
        AniStop(AnimationName)
        If IsLoaded AndAlso AniControlEnabled = 0 Then
            Rows.Opacity = 0
            AniStart(AaOpacity(Rows, 1, 120), AnimationName)
        Else
            Rows.Opacity = 1
        End If
    End Sub

    Protected Shared Function Label(Text As String, Size As Double, Optional Bold As Boolean = False, Optional Color As String = "ColorBrush1", Optional Margin As Thickness = Nothing) As TextBlock
        Dim Value As New TextBlock With {.Text = Text, .FontSize = Size, .FontWeight = If(Bold, FontWeights.SemiBold, FontWeights.Normal), .TextWrapping = TextWrapping.Wrap, .Margin = Margin}
        Value.SetResourceReference(TextBlock.ForegroundProperty, Color)
        Return Value
    End Function

    Protected Shared Function Frame(Child As UIElement) As Border
        Dim Box As New Border With {.Child = Child, .CornerRadius = New CornerRadius(5), .BorderThickness = New Thickness(1),
            .Padding = New Thickness(12, 10, 12, 10), .Margin = New Thickness(3, 0, 7, 8), .Background = ThemeBrush("ColorBrushWhite")}
        Box.SetResourceReference(Border.BorderBrushProperty, "ColorBrushGray5")
        Return Box
    End Function
End Class

Friend NotInheritable Class ModpackVersionSelectView
    Inherits ModpackDialogView

    Private ReadOnly Versions As List(Of ResourceVersion)
    Private SelectedVersion As ResourceVersion
    Friend ReadOnly Selections As New List(Of MyListItem)
    Friend Event SelectionChanged As EventHandler
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
    Private Channel As ResourceVersion.ReleaseTypes

    Friend Sub New(Items As IEnumerable(Of ResourceVersion), ProjectName As String, CurrentVersion As String)
        MyBase.New(ProjectName, "当前版本：" & If(String.IsNullOrWhiteSpace(CurrentVersion), "未记录", CurrentVersion), 3)
        Versions = Items.OrderByDescending(Function(Item) Item.ReleaseDate).ToList()
        Height = Math.Min(420, Math.Max(200, 100 + 72 * Versions.GroupBy(Function(Item) ChannelOf(Item)).Select(Function(Group) Group.Count()).DefaultIfEmpty().Max()))
        For Each Kind In {ResourceVersion.ReleaseTypes.Release, ResourceVersion.ReleaseTypes.Beta, ResourceVersion.ReleaseTypes.Alpha}
            Dim Count = Versions.Where(Function(Item) ChannelOf(Item) = Kind).Count()
            AddFilter(ChannelName(Kind) & "（" & Count & "）", CInt(Kind), Count > 0, Sub(Key) ShowChannel(CType(Key, ResourceVersion.ReleaseTypes)))
        Next
        ShowChannel({ResourceVersion.ReleaseTypes.Release, ResourceVersion.ReleaseTypes.Beta, ResourceVersion.ReleaseTypes.Alpha}.FirstOrDefault(Function(Kind) Versions.Any(Function(Item) ChannelOf(Item) = Kind)))
    End Sub

    Private Shared Function ChannelOf(Version As ResourceVersion) As ResourceVersion.ReleaseTypes
        Return If(Version.ReleaseType = ResourceVersion.ReleaseTypes.Release OrElse Version.ReleaseType = ResourceVersion.ReleaseTypes.Beta, Version.ReleaseType, ResourceVersion.ReleaseTypes.Alpha)
    End Function

    Private Shared Function ChannelName(Kind As ResourceVersion.ReleaseTypes) As String
        Return If(Kind = ResourceVersion.ReleaseTypes.Release, "正式版", If(Kind = ResourceVersion.ReleaseTypes.Beta, "测试版", "早期测试版"))
    End Function

    Friend Sub ShowChannel(Kind As ResourceVersion.ReleaseTypes)
        ActiveChannel = Kind
        SelectedVersion = Nothing : Selections.Clear()
        RaiseEvent SelectionChanged(Me, EventArgs.Empty)
        BeginFilter(CInt(Kind))
        For Each Version In Versions.Where(Function(Item) ChannelOf(Item) = Kind)
            Dim Games = If(Version.GameVersions, New List(Of String))
            Dim GameText = If(Games.Count = 0, "游戏版本未知", String.Join("、", Games.Take(4)) & If(Games.Count > 4, " 等", ""))
            Dim LoaderText = If(Version.ModLoaders = ModLoaders.None, "", " · " & Version.ModLoaders.ToString())
            Dim Item As New MyListItem With {.Title = Version.Display, .Info = "Minecraft " & GameText & LoaderText & " · " & Version.ReleaseDate.ToString("yyyy-MM-dd"),
                .Type = MyListItem.CheckType.RadioBox, .Height = 64, .PaddingLeft = 14, .MinPaddingRight = 12, .Margin = New Thickness(3, 0, 7, 8), .Tag = Version,
                .Background = ThemeBrush("ColorBrushWhite"), .ToolTip = Version.Display & vbCrLf & "Minecraft " & String.Join("、", Games) & LoaderText}
            Dim Outline As New Border With {.CornerRadius = New CornerRadius(5), .BorderThickness = New Thickness(1), .IsHitTestVisible = False}
            Outline.SetResourceReference(Border.BorderBrushProperty, "ColorBrushGray5")
            Grid.SetColumnSpan(Outline, 999) : Grid.SetRowSpan(Outline, 999) : Item.Children.Insert(0, Outline)
            AddHandler Item.Check, Sub()
                                       SelectedVersion = Version
                                       RaiseEvent SelectionChanged(Me, EventArgs.Empty)
                                   End Sub
            AddHandler Item.Changed, Sub() Outline.SetResourceReference(Border.BorderBrushProperty, If(Item.Checked, "ColorBrush3", "ColorBrushGray5"))
            Selections.Add(Item) : Rows.Children.Add(Item)
        Next
        If Selections.Count = 0 Then Rows.Children.Add(Label("此分类没有其他可下载的版本。", 13, Margin:=New Thickness(8)))
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
    Friend Property ActiveKind As ChangeKind
        Get
            Return Kind
        End Get
        Private Set(Value As ChangeKind)
            Kind = Value
        End Set
    End Property
    Private Kind As ChangeKind

    Friend Sub New(Plan As PackUpdatePlan, CurrentMinecraft As String, CurrentLoaders As Dictionary(Of ModLoaders, String))
        MyBase.New(Plan.Package.Name & " " & Plan.Package.Version, RuntimeSummary(Plan.Package, CurrentMinecraft, CurrentLoaders), 4)
        Changes = Plan.Changes.ToList()
        Dim RuntimeChanged = CurrentMinecraft <> Plan.Package.Minecraft OrElse Not CurrentLoaders.OrderBy(Function(Pair) Pair.Key).SequenceEqual(Plan.Package.Loaders.OrderBy(Function(Pair) Pair.Key))
        If RuntimeChanged Then Header.Children.Add(New MyHint With {.Text = "游戏版本或加载器将更新，请确认兼容性。存档会保留，更新前自动备份。", .Theme = MyHint.Themes.Yellow, .Margin = New Thickness(0, 10, 0, 0)})
        For Each Value In {ChangeKind.Added, ChangeKind.Updated, ChangeKind.Removed, ChangeKind.Preserved}
            Counts(Value) = Changes.Where(Function(Item) Classify(Item) = Value).Count()
            AddFilter(KindName(Value) & "（" & Counts(Value) & "）", CInt(Value), Counts(Value) > 0, Sub(Key) ShowKind(CType(Key, ChangeKind)))
        Next
        ShowKind({ChangeKind.Preserved, ChangeKind.Updated, ChangeKind.Added, ChangeKind.Removed}.FirstOrDefault(Function(Value) Counts(Value) > 0))
    End Sub

    Friend Shared Function Classify(Change As PackUpdateChange) As ChangeKind
        If Change.Conflict Then Return ChangeKind.Preserved
        If Change.Incoming Is Nothing Then Return ChangeKind.Removed
        Return If(Change.Before Is Nothing, ChangeKind.Added, ChangeKind.Updated)
    End Function

    Private Shared Function KindName(Value As ChangeKind) As String
        Select Case Value
            Case ChangeKind.Added : Return "新增"
            Case ChangeKind.Updated : Return "更新"
            Case ChangeKind.Removed : Return "移除"
            Case Else : Return "保留个人修改"
        End Select
    End Function

    Private Shared Function RuntimeSummary(Package As PackUpdatePackage, Minecraft As String, Loaders As Dictionary(Of ModLoaders, String)) As String
        Dim Values As New List(Of String) From {"Minecraft " & If(Minecraft = Package.Minecraft, Minecraft, Minecraft & " → " & Package.Minecraft)}
        For Each Loader In Loaders.Keys.Union(Package.Loaders.Keys).OrderBy(Function(Value) Value)
            Dim Before As String = Nothing, After As String = Nothing
            Loaders.TryGetValue(Loader, Before) : Package.Loaders.TryGetValue(Loader, After)
            Values.Add(Loader.ToString() & " " & If(Before = After, After, If(Before, "无") & " → " & If(After, "移除")))
        Next
        Return String.Join(" · ", Values)
    End Function

    Private Shared Function FolderName(Relative As String) As String
        Dim Parts = Relative.Split("/"c)
        If Parts.Length = 1 Then Return "根目录文件"
        Select Case Parts(0).ToLowerInvariant()
            Case "mods" : Return "模组"
            Case "config", "defaultconfigs" : Return "配置"
            Case "resourcepacks" : Return "资源包"
            Case "shaderpacks" : Return "光影包"
            Case "datapacks" : Return "数据包"
            Case Else : Return Parts(0)
        End Select
    End Function

    Friend Sub ShowKind(Value As ChangeKind)
        ActiveKind = Value : BeginFilter(CInt(Value))
        Dim Description = If(Value = ChangeKind.Preserved, "以下文件保留当前状态，本次更新不会覆盖。", If(Value = ChangeKind.Removed, "作者在新版中移除了以下文件，更新前会备份。", If(Value = ChangeKind.Added, "新版将添加以下文件。", "以下文件将替换为作者提供的新版，更新前会备份。")))
        Rows.Children.Add(Label(Description, 12, Color:="ColorBrushGray2", Margin:=New Thickness(4, 0, 7, 10)))
        For Each Group In Changes.Where(Function(Item) Classify(Item) = Value).GroupBy(Function(Item) FolderName(Item.Relative)).OrderBy(Function(Item) Item.Key)
            Rows.Children.Add(Label(Group.Key & "（" & Group.Count() & "）", 13, True, Margin:=New Thickness(4, 3, 7, 9)))
            For Each Change In Group.OrderBy(Function(Item) Item.Relative, StringComparer.OrdinalIgnoreCase)
                Dim Text As New StackPanel
                Dim Name = Label(Path.GetFileName(Change.Relative), 13, True)
                Name.TextWrapping = TextWrapping.NoWrap : Name.TextTrimming = TextTrimming.CharacterEllipsis
                Text.Children.Add(Name)
                Dim Folder = Path.GetDirectoryName(Change.Relative.Replace("/"c, Path.DirectorySeparatorChar))
                Text.Children.Add(Label(If(String.IsNullOrEmpty(Folder), "根目录", Folder) & If(Value = ChangeKind.Preserved, " · " & Change.Reason, ""), 12, False, "ColorBrushGray2", New Thickness(0, 4, 0, 0)))
                Dim Box = Frame(Text)
                Box.ToolTip = Change.Relative & vbCrLf & Change.Reason
                Rows.Children.Add(Box)
            Next
        Next
        If Counts(Value) = 0 Then Rows.Children.Add(Label("此分类没有文件。", 13, Margin:=New Thickness(8)))
    End Sub
End Class
