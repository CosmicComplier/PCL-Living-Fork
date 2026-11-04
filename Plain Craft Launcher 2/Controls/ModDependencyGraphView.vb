Imports System.Windows.Media
Imports System.Windows.Shapes
Imports System.Threading
Imports System.Threading.Tasks
Imports Inspector = PCL.ModDependencyInspector

'只读现有报告：只展示必需依赖、必需反向依赖和当前命中的冲突。
Friend NotInheritable Class ModDependencyGraphView
    Inherits Grid

    Friend Class GraphNode
        Public Identity As String
        Public Title As String
        Public Condition As String
        Public Details As String
        Public IsConflict As Boolean
        Public IsCandidate As Boolean
        Public IsPlatformRequirement As Boolean
        Public NeedsAttention As Boolean
        Public NeedsVersionReview As Boolean
        Public FileName As String
        Public IconPath As String
        Public Height As Double = 50
    End Class

    Friend ReadOnly GraphCanvas As New Canvas With {.Width = 708, .Background = Brushes.Transparent}
    Private Const NodeWidth As Double = 184
    Private Const RowGap As Double = 10
    Private Shared ReadOnly IconSlots As New SemaphoreSlim(3)
    Private ReadOnly GraphScroll As New ScrollViewer
    Private ReadOnly IconImages As New Dictionary(Of Border, Image)
    Private ReadOnly PendingIcons As New HashSet(Of Border)
    Private ReadOnly IconCache As New Dictionary(Of String, ImageSource)(StringComparer.Ordinal)
    Private IconGeneration As Integer
    Friend ReadOnly Property PreferredWidth As Double
        Get
            Return GraphCanvas.Width + 84 '弹窗内边距与纵向滚动条，避免默认宽度出现横向滚动。
        End Get
    End Property

    Friend Sub New(Report As Inspector.InspectionReport, FullPath As String)
        RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(1, GridUnitType.Star)})
        RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
        GraphScroll.Content = GraphCanvas
        GraphScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        GraphScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        GraphScroll.HorizontalContentAlignment = HorizontalAlignment.Center
        Children.Add(GraphScroll)
        AddHandler GraphScroll.ScrollChanged, Sub() RefreshVisibleIcons()
        AddHandler Loaded, Sub() RefreshVisibleIcons()
        AddHandler Unloaded, Sub() IconGeneration += 1
        Dim Focus = Report.Mods.Where(Function(Item) Inspector.SameFilePath(Item.RootPath, FullPath)).ToList()
        Dim Primary = If(Focus.FirstOrDefault(Function(Item) Not Item.IsNested), Focus.FirstOrDefault())
        Dim PlatformFiles = Report.OnlineFiles.Where(Function(Item) Inspector.SameFilePath(Item.FullPath, FullPath)).ToList()
        Dim HasPlatform = PlatformFiles.Any(Function(Item) Item.Matches.Any(Function(Match) Match.IsExact))
        Dim UseLocal = Not Report.OnlineAttempted OrElse Report.OnlineTimedOut OrElse Not HasPlatform
        Dim ExactFiles As New HashSet(Of String)(Report.OnlineFiles.Where(Function(Item) Item.Matches.Any(Function(Match) Match.IsExact)).Select(Function(Item) Inspector.FilePathKey(Item.FullPath)), StringComparer.OrdinalIgnoreCase)
        Dim PlatformRequirements = PlatformFiles.SelectMany(Function(Item) Item.Matches).Where(Function(Item) Item.IsExact).
            SelectMany(Function(Item) Item.Dependencies).Where(Function(Item) Item.Kind = "required").ToList()
        Dim PlatformPaths As New HashSet(Of String)(PlatformRequirements.SelectMany(Function(Item) Item.EnabledPaths).Select(AddressOf Inspector.FilePathKey), StringComparer.OrdinalIgnoreCase)
        Dim Incoming = Report.Edges.Where(Function(Edge) UseLocal AndAlso Edge.Requirement.Kind = Inspector.DependencyKind.Required AndAlso Not ExactFiles.Contains(Inspector.FilePathKey(Edge.Owner.RootPath))).
            Select(Function(Edge) New With {.Edge = Edge, .Relation = Inspector.FileProviderRelation(Report, Edge, FullPath)}).
            Where(Function(Item) Item.Relation <> Inspector.FileProviderKind.None).
            GroupBy(Function(Item) Inspector.FilePathKey(Item.Edge.Owner.RootPath), StringComparer.OrdinalIgnoreCase).ToList()
        '依赖归属依据已求解的提供者，不能凭一个 JAR 恰好打包了相同 ID 的备用组件推断。
        Dim Needed = Focus.SelectMany(Function(Item) Item.Dependencies).Where(Function(Item) Item.Kind = Inspector.DependencyKind.Required).ToList()
        Dim LeftNodes = Incoming.Select(Function(Group)
            Dim Container = ContainerMod(Report, Group.First().Edge.Owner)
            Dim Possible = Not Group.Any(Function(Item) Item.Relation = Inspector.FileProviderKind.Required)
            Return New GraphNode With {.Title = Container.Name, .Condition = If(Possible, "候选 · ", "本地参考 · ") & Container.Version,
                .IsCandidate = Possible, .FileName = Container.RootPath, .IconPath = Container.IconPath,
                .Details = String.Join(vbCrLf, Group.Select(Function(Item) Item.Edge.Requirement.Display).Distinct())}
        End Function).ToList()
        For Each File In Report.OnlineFiles.Where(Function(Item) Item.Enabled AndAlso Not Inspector.SameFilePath(Item.FullPath, FullPath))
            Dim Required = File.Matches.Where(Function(Item) Item.IsExact).SelectMany(Function(Item) Item.Dependencies).
                Where(Function(Item) Item.Kind = "required" AndAlso Item.EnabledPaths.Concat(Item.ProjectPaths).Any(Function(Path) Inspector.SameFilePath(Path, FullPath))).ToList()
            If Required.Count = 0 Then Continue For
            Dim Owner = Report.Mods.FirstOrDefault(Function(Item) Not Item.IsNested AndAlso Inspector.SameFilePath(Item.RootPath, File.FullPath))
            Dim VersionReview = Required.Any(Function(Item) Item.ProjectPaths.Count > 0 AndAlso Item.MatchedPaths.Count = 0 AndAlso Not String.IsNullOrEmpty(Item.VersionId))
            LeftNodes.Add(New GraphNode With {.Title = If(Owner?.Name, IO.Path.GetFileName(File.FullPath)), .Condition = "平台声明 · " & If(Owner?.Version, "") & If(VersionReview, " · 指定版本待核对", ""), .IsCandidate = VersionReview,
                .FileName = File.FullPath, .IconPath = Owner?.IconPath, .IsPlatformRequirement = True,
                .Details = String.Join(vbCrLf, Required.Select(Function(Item) Item.Platform & " 必需前置：" & If(Item.ProjectName, Item.ProjectId) & vbCrLf & Item.Status).Distinct())})
        Next
        Dim RightNodes = Needed.Select(Function(Requirement)
            Dim Owner = Focus.First(Function(Item) Item.Dependencies.Contains(Requirement))
            Dim Edge = Report.Edges.FirstOrDefault(Function(Item) Item.Owner Is Owner AndAlso Item.Requirement Is Requirement)
            If Edge IsNot Nothing AndAlso (Not Edge.Evaluated OrElse Edge.ConditionBypassed) Then Return Nothing
            If Inspector.FileSatisfiesDependency(Report, FullPath, Requirement) Then Return Nothing
            Dim Providers = If(Edge Is Nothing, Report.Mods.Where(Function(Item) Item.Enabled AndAlso Requirement.Id IsNot Nothing AndAlso Inspector.Identities(Item).ContainsKey(Requirement.Id)).ToList(), Edge.Providers)
            If Edge Is Nothing AndAlso Providers.Any(Function(Item) Not Item.IsNested) Then Providers = Providers.Where(Function(Item) Not Item.IsNested).ToList()
            Dim Preferred = Providers.Where(Function(Item) Not Item.IsBuiltin AndAlso PlatformPaths.Contains(Inspector.FilePathKey(Item.RootPath))).ToList()
            If Requirement.Children.Count = 0 AndAlso Preferred.Count > 0 AndAlso Preferred.All(Function(Item) Inspector.SameFilePath(Item.RootPath, Preferred(0).RootPath)) Then Providers = Preferred
            Dim Provider = Providers.FirstOrDefault()
            '同一 JAR 已打包并满足的内嵌依赖，不作为需要另装的外部模组展示。
            If Edge IsNot Nothing AndAlso Edge.Providers.Count > 0 AndAlso
                Edge.Providers.All(Function(Item) Not Item.IsBuiltin AndAlso Inspector.SameFilePath(Item.RootPath, FullPath)) Then Return Nothing
            Dim Title = If(Provider?.Name, If(Requirement.Id, Requirement.Display))
            Dim Identity = If(Requirement.Children.Count = 0, "mod:" & Requirement.Id, Requirement.Syntax & ":" & Requirement.Display)
            Dim Condition = RangeText(Requirement)
            Dim IconFile As String = Nothing, IconPath As String = Nothing
            If Provider IsNot Nothing AndAlso Not Provider.IsBuiltin AndAlso Providers.All(Function(Item) Not Item.IsBuiltin AndAlso Inspector.SameFilePath(Item.RootPath, Provider.RootPath)) Then
                Dim Container = ContainerMod(Report, Provider)
                Title = Container.Name
                Identity = "jar:" & Inspector.FilePathKey(Container.RootPath).ToUpperInvariant()
                Condition = Container.Version
                IconFile = Container.RootPath : IconPath = Container.IconPath
            ElseIf Provider IsNot Nothing AndAlso Not Provider.IsBuiltin Then
                '多个外部容器都能提供时，报告尚未确定加载器选择，不能冒认第一个候选。
                Title = String.Join(" / ", Providers.Select(Function(Item) If(Item.IsBuiltin, Item.Name, ContainerMod(Report, Item).Name)).Distinct(StringComparer.Ordinal))
                Identity = "choice:" & Requirement.Syntax & ":" & Requirement.Display
                Condition = "提供方待确认"
            End If
            Dim RuntimeOwner = Inspector.RuntimeOwnerId(Requirement.Id, Report.Context)
            If RuntimeOwner <> "" Then
                Title = Inspector.RuntimeDisplayName(RuntimeOwner)
                Identity = "builtin:" & RuntimeOwner
                If Inspector.IsInternalLoaderId(Requirement.Id) Then Condition = "加载器内置支持"
            End If
            Select Case Requirement.Id
                Case "fabricloader" : Title = "Fabric Loader"
                Case "minecraft" : Title = "Minecraft"
                Case "java" : Title = "Java"
            End Select
            Return New GraphNode With {.Identity = Identity, .Title = Title, .Condition = Condition, .Details = "本地声明：" & Inspector.RequirementDisplay(Requirement, Report.Context),
                .IsCandidate = Identity.StartsWith("choice:", StringComparison.Ordinal), .FileName = IconFile, .IconPath = IconPath}
        End Function).Where(Function(Item) Item IsNot Nothing).GroupBy(Function(Item) Item.Identity, StringComparer.Ordinal).
            Select(Function(Group) New GraphNode With {.Identity = Group.Key, .Title = Group.First().Title,
                .Condition = String.Join(" 且 ", Group.Select(Function(Item) Item.Condition).Where(Function(Value) Value <> "").Distinct(StringComparer.Ordinal)),
                .Details = String.Join(vbCrLf, Group.Select(Function(Item) Item.Details).Distinct(StringComparer.Ordinal)),
                .IsCandidate = Group.First().IsCandidate, .FileName = Group.First().FileName, .IconPath = Group.First().IconPath}).ToList()
        If Not UseLocal Then
            RightNodes.Clear()
        End If
        For Each Requirement In PlatformRequirements
            Dim Local = Report.Mods.FirstOrDefault(Function(Item) Not Item.IsNested AndAlso Requirement.EnabledPaths.Any(Function(Path) Inspector.SameFilePath(Path, Item.RootPath)))
            If Local Is Nothing Then Local = Report.Mods.FirstOrDefault(Function(Item) Not Item.IsNested AndAlso Requirement.MatchedPaths.Any(Function(Path) Inspector.SameFilePath(Path, Item.RootPath)))
            If Local Is Nothing Then Local = Report.Mods.FirstOrDefault(Function(Item) Not Item.IsNested AndAlso Requirement.ProjectPaths.Any(Function(Path) Inspector.SameFilePath(Path, Item.RootPath)))
            Dim VersionReview = Requirement.ProjectPaths.Count > 0 AndAlso Requirement.MatchedPaths.Count = 0 AndAlso Not String.IsNullOrEmpty(Requirement.VersionId)
            If Local IsNot Nothing AndAlso Inspector.SameFilePath(Local.RootPath, FullPath) Then Continue For
            Dim PlatformKey = If(Not String.IsNullOrEmpty(Requirement.ProjectId), Requirement.ProjectId,
                If(Not String.IsNullOrEmpty(Requirement.VersionId), "version:" & Requirement.VersionId, "file:" & Requirement.DisplayName))
            Dim Key = If(Local Is Nothing, "platform:" & Requirement.Platform & ":" & PlatformKey,
                "jar:" & Inspector.FilePathKey(Local.RootPath).ToUpperInvariant())
            '未确认的跨平台同名项目不能当作同一模组；已精确识别的同一 JAR 才合并。
            Dim Existing = RightNodes.FirstOrDefault(Function(Item) Item.Identity = Key)
            Dim Detail = Requirement.Platform & " 精确版本：必需前置 " & Requirement.DisplayName & " [" & Requirement.ProjectId & "]" &
                If(String.IsNullOrEmpty(Requirement.VersionId), "", "；指定平台版本 " & If(String.IsNullOrEmpty(Requirement.VersionName), "", Requirement.VersionName & " ") & "[" & Requirement.VersionId & "]")
            If Existing IsNot Nothing Then
                Existing.IsPlatformRequirement = True
                Existing.NeedsAttention = Existing.NeedsAttention OrElse (Local IsNot Nothing AndAlso Not Local.Enabled)
                Existing.NeedsVersionReview = Existing.NeedsVersionReview OrElse VersionReview
                Existing.IsCandidate = Existing.IsCandidate OrElse Local Is Nothing OrElse VersionReview
                Existing.Details &= vbCrLf & Detail & vbCrLf & Requirement.Status
            Else
                RightNodes.Add(New GraphNode With {.Identity = Key, .Title = If(Local?.Name, Requirement.DisplayName),
                    .Condition = If(Local Is Nothing, "安装状态待确认", If(Local.Enabled, Local.Version, "已禁用 · " & Local.Version)), .Details = Detail & vbCrLf & Requirement.Status,
                    .NeedsAttention = Local IsNot Nothing AndAlso Not Local.Enabled, .NeedsVersionReview = VersionReview, .IsCandidate = Local Is Nothing OrElse VersionReview,
                    .IsPlatformRequirement = True, .FileName = Local?.RootPath, .IconPath = Local?.IconPath})
            End If
        Next
        For Each Node In RightNodes.Where(Function(Item) Item.IsPlatformRequirement)
            Node.Condition = "平台前置 · " & Node.Condition & If(Node.NeedsVersionReview, " · 指定版本待核对", "")
        Next
        For Each Node In RightNodes.Where(Function(Item) Not Item.IsPlatformRequirement)
            Node.Condition = "本地参考 · " & Node.Condition
        Next
        Dim NeededCount = RightNodes.Count
        '冲突双向可见；未命中的版本限制、可选和建议项不放进图中。
        Dim Conflicts = Report.Edges.Where(Function(Edge) UseLocal AndAlso Edge.Evaluated AndAlso Not Edge.ConditionBypassed AndAlso Edge.State = Inspector.MatchState.Satisfied AndAlso
            (Edge.Requirement.Kind = Inspector.DependencyKind.Incompatible OrElse Edge.Requirement.Kind = Inspector.DependencyKind.Conflict) AndAlso
            (Focus.Contains(Edge.Owner) OrElse Edge.Providers.Any(Function(Provider) Not Provider.IsBuiltin AndAlso Inspector.SameFilePath(Provider.RootPath, FullPath)))).ToList()
        Dim ConflictNodes As New List(Of GraphNode)
        For Each Edge In Conflicts
            If Focus.Contains(Edge.Owner) Then
                For Each Provider In Edge.Providers.Where(Function(Item) Item.IsBuiltin OrElse Not Inspector.SameFilePath(Item.RootPath, FullPath))
                    Dim Container = If(Provider.IsBuiltin, Provider, ContainerMod(Report, Provider))
                    ConflictNodes.Add(New GraphNode With {.Identity = If(Provider.IsBuiltin, "builtin:" & If(Inspector.RuntimeOwnerId(Provider.Id, Report.Context) = "", Provider.Id, Inspector.RuntimeOwnerId(Provider.Id, Report.Context)), "jar:" & Inspector.FilePathKey(Provider.RootPath).ToUpperInvariant()),
                        .Title = If(Provider.IsBuiltin AndAlso Inspector.RuntimeOwnerId(Provider.Id, Report.Context) <> "", Inspector.RuntimeDisplayName(Inspector.RuntimeOwnerId(Provider.Id, Report.Context)), Container.Name),
                        .Condition = If(Provider.IsBuiltin AndAlso Inspector.IsInternalLoaderId(Provider.Id), "加载器内置支持", Container.Version),
                        .Details = Inspector.RequirementDisplay(Edge.Requirement, Report.Context), .IsConflict = True,
                        .FileName = If(Provider.IsBuiltin, Nothing, Container.RootPath), .IconPath = Container.IconPath})
                Next
            Else
                Dim Container = ContainerMod(Report, Edge.Owner)
                ConflictNodes.Add(New GraphNode With {.Identity = "jar:" & Inspector.FilePathKey(Container.RootPath).ToUpperInvariant(),
                    .Title = Container.Name, .Condition = Container.Version, .Details = Edge.Requirement.Display, .IsConflict = True,
                    .FileName = Container.RootPath, .IconPath = Container.IconPath})
            End If
        Next
        For Each Node In ConflictNodes
            Node.Condition = "本地参考 · " & Node.Condition
        Next
        For Each File In Report.OnlineFiles.Where(Function(Item) Item.Enabled)
            Dim IsOwner = Inspector.SameFilePath(File.FullPath, FullPath)
            For Each Requirement In File.Matches.Where(Function(Item) Item.IsExact).SelectMany(Function(Item) Item.Dependencies).Where(Function(Item) Item.Kind = "incompatible")
                Dim Targets = If(IsOwner, Requirement.EnabledPaths.AsEnumerable(),
                    If(Requirement.EnabledPaths.Any(Function(Path) Inspector.SameFilePath(Path, FullPath)), {File.FullPath}.AsEnumerable(), Enumerable.Empty(Of String)()))
                For Each TargetPath In Targets.Where(Function(Path) Not Inspector.SameFilePath(Path, FullPath))
                    Dim Local = Report.Mods.FirstOrDefault(Function(Item) Not Item.IsNested AndAlso Inspector.SameFilePath(Item.RootPath, TargetPath))
                    ConflictNodes.Add(New GraphNode With {.Identity = "jar:" & Inspector.FilePathKey(TargetPath).ToUpperInvariant(), .Title = If(Local?.Name, IO.Path.GetFileName(TargetPath)),
                        .Condition = "平台冲突 · " & If(Local?.Version, ""), .Details = Requirement.Platform & " 精确版本冲突声明", .IsConflict = True, .IsPlatformRequirement = True,
                        .FileName = TargetPath, .IconPath = Local?.IconPath})
                Next
            Next
        Next
        ConflictNodes = ConflictNodes.GroupBy(Function(Item) Item.Identity, StringComparer.Ordinal).Select(Function(Group) New GraphNode With {
            .Identity = Group.Key, .Title = Group.First().Title, .IsConflict = True,
            .Condition = String.Join("；", Group.Where(Function(Item) Item.IsPlatformRequirement = Group.Any(Function(Value) Value.IsPlatformRequirement)).Select(Function(Item) Item.Condition).Distinct(StringComparer.Ordinal)),
            .Details = String.Join(vbCrLf, Group.Select(Function(Item) Item.Details).Distinct(StringComparer.Ordinal)),
            .FileName = Group.First().FileName, .IconPath = Group.First().IconPath}).ToList()
        Dim CenterX = If(LeftNodes.Count = 0, 4.0, 232.0)
        Dim RightX = CenterX + 228
        GraphCanvas.Width = RightX + NodeWidth + 4
        Dim Center As New GraphNode With {.Title = If(Primary?.Name, IO.Path.GetFileName(FullPath)),
            .Condition = If(Primary Is Nothing, "未识别本地声明", Primary.Version),
            .FileName = Primary?.RootPath, .IconPath = Primary?.IconPath}
        For Each Node In LeftNodes.Concat(RightNodes).Concat(ConflictNodes).Concat({Center})
            Dim Content = NodeContent(Node)
            Content.Measure(New Size(NodeWidth - 20, Double.PositiveInfinity))
            Node.Height = Math.Max(50, Math.Ceiling(Content.DesiredSize.Height) + 16)
        Next
        Dim LeftHeight = StackHeight(LeftNodes), RightHeight = StackHeight(RightNodes)
        Dim Middle = 34 + Math.Max(0, Math.Min(100, (Math.Max(LeftHeight, RightHeight) - Center.Height) / 2))
        Dim LeftTop = Math.Max(34, Middle + Center.Height / 2 - LeftHeight / 2)
        If LeftNodes.Count > 0 Then Header(If(LeftNodes.Any(Function(Item) Item.IsCandidate), "被这些模组依赖（含候选）", "被这些模组依赖"), 4, LeftTop - 25)
        Header("当前模组", CenterX, Middle - 25)
        Header(If(UseLocal, "需要的依赖（本地参考）", "需要的依赖（平台声明）"), RightX, 9)
        Dim Y = LeftTop
        For Each Node In LeftNodes
            Connector(New Point(4 + NodeWidth, Y + Node.Height / 2), New Point(CenterX, Middle + Center.Height / 2), False, Node.IsCandidate)
            AddNode(Node, 4, Y)
            Y += Node.Height + RowGap
        Next
        Y = 34
        For Each Node In RightNodes
            Connector(New Point(CenterX + NodeWidth, Middle + Center.Height / 2), New Point(RightX, Y + Node.Height / 2), False, Node.IsCandidate)
            AddNode(Node, RightX, Y)
            Y += Node.Height + RowGap
        Next
        Dim ConflictTop = Middle + Center.Height + If(LeftNodes.Count = 0, 44, 22)
        Header("存在冲突", CenterX, ConflictTop)
        Y = ConflictTop + 25
        For Each Node In ConflictNodes
            Connector(New Point(CenterX + NodeWidth, Middle + Center.Height / 2), New Point(CenterX + NodeWidth, Y + Node.Height / 2), True)
            AddNode(Node, CenterX, Y)
            Y += Node.Height + RowGap
        Next
        AddNode(Center, CenterX, Middle, True)
        If LeftNodes.Count = 0 Then EmptyLabel("被依赖：暂无", CenterX, Middle + Center.Height + 10)
        If RightNodes.Count = 0 Then EmptyLabel("暂无", RightX, 34)
        If ConflictNodes.Count = 0 Then EmptyLabel("未发现", CenterX, ConflictTop + 25)
        GraphCanvas.Height = Math.Max(Math.Max(LeftTop + LeftHeight, 34 + RightHeight),
            ConflictTop + 25 + Math.Max(20, StackHeight(ConflictNodes))) + 12
        Height = Math.Min(480, GraphCanvas.Height + 55)
        Dim CandidateCount = LeftNodes.Where(Function(Item) Item.IsCandidate).Count()
        Dim Summary As New TextBlock With {.FontSize = 12, .Foreground = Brushes.Gray, .Margin = New Thickness(4, 8, 4, 0), .TextWrapping = TextWrapping.Wrap,
            .Text = "需要 " & NeededCount & " 项 · 被依赖 " & (LeftNodes.Count - CandidateCount) & " 项 · 冲突 " & ConflictNodes.Count & " 项"}
        If CandidateCount > 0 Then Summary.Text &= " · 候选关联 " & CandidateCount & " 项"
        If CandidateCount > 0 OrElse RightNodes.Any(Function(Item) Item.IsCandidate) Then Summary.Text &= vbCrLf & "灰色虚线表示提供方待确认。"
        If RightNodes.Any(Function(Item) Item.IsPlatformRequirement) Then Summary.Text &= vbCrLf & "平台前置来自精确版本声明，不用于启动拦截。"
        If Report.OnlineAttempted AndAlso Not HasPlatform Then Summary.Text &= vbCrLf & "未查到精确的平台声明，当前以本地声明作为参考。"
        If Report.OnlineTimedOut Then Summary.Text &= vbCrLf & "网络查询超时，当前以本地声明作为主要参考。"
        If Report.OnlineTimedOut OrElse Report.OnlineCancelled Then Summary.Text &= vbCrLf & "平台查询未完成，已显示的结果可能不全。"
        Grid.SetRow(Summary, 1) : Children.Add(Summary)
    End Sub

    Private Shared Function StackHeight(Nodes As List(Of GraphNode)) As Double
        Return If(Nodes.Count = 0, 0, Nodes.Sum(Function(Item) Item.Height) + (Nodes.Count - 1) * RowGap)
    End Function

    Private Shared Function ContainerMod(Report As Inspector.InspectionReport, Item As Inspector.ModInfo) As Inspector.ModInfo
        Return If(Report.Mods.FirstOrDefault(Function(Candidate) Not Candidate.IsNested AndAlso Inspector.SameFilePath(Candidate.RootPath, Item.RootPath)), Item)
    End Function

    Private Shared Function RangeText(Requirement As Inspector.Dependency) As String
        If Requirement.Children.Count > 0 Then Return Requirement.Display
        Dim Range = Requirement.Versions?.ToString()
        Return If(String.IsNullOrWhiteSpace(Range) OrElse Range = "*", "", Range)
    End Function
    Private Sub Header(Text As String, X As Double, Y As Double)
        Dim Label As New TextBlock With {.Text = Text, .FontSize = 12, .FontWeight = FontWeights.SemiBold, .Foreground = Brushes.Gray}
        Canvas.SetLeft(Label, X) : Canvas.SetTop(Label, Y) : GraphCanvas.Children.Add(Label)
    End Sub
    Private Sub EmptyLabel(Text As String, X As Double, Y As Double)
        Dim Label As New TextBlock With {.Text = Text, .FontSize = 12, .Foreground = Brushes.Gray}
        Canvas.SetLeft(Label, X) : Canvas.SetTop(Label, Y) : GraphCanvas.Children.Add(Label)
    End Sub
    Private Shared Function Accent(Conflict As Boolean, Optional Possible As Boolean = False) As Brush
        If Conflict Then Return New SolidColorBrush(Color.FromRgb(207, 71, 71))
        If Possible Then Return New SolidColorBrush(Color.FromRgb(139, 147, 161))
        Return If(TryCast(Application.Current.TryFindResource("ColorBrush3"), Brush), Brushes.RoyalBlue)
    End Function

    Private Shared Function DefaultIcon(Node As GraphNode) As ImageSource
        Dim Name = "Icons/NoIcon.png"
        Select Case Node.Identity
            Case "mod:minecraft" : Name = "Blocks/Grass.png"
            Case "mod:fabricloader" : Name = "Blocks/Fabric.png"
            Case "mod:forge" : Name = "Blocks/Anvil.png"
            Case "mod:neoforge" : Name = "Blocks/NeoForge.png"
        End Select
        Return CType(New MyBitmap(PathImage & Name), ImageSource)
    End Function

    Private Shared Function NodeContent(Node As GraphNode) As Grid
        Dim Content As New Grid
        Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(28)})
        Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(8)})
        Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
        Dim Icon As New Image With {.Source = DefaultIcon(Node), .Width = 28, .Height = 28, .Stretch = Stretch.Uniform, .VerticalAlignment = VerticalAlignment.Center,
            .Clip = New RectangleGeometry(New Rect(0, 0, 28, 28), 5, 5)}
        RenderOptions.SetBitmapScalingMode(Icon, BitmapScalingMode.HighQuality)
        Content.Children.Add(Icon)
        Dim Text As New StackPanel With {.VerticalAlignment = VerticalAlignment.Center}
        Text.Children.Add(New TextBlock With {.Text = Node.Title, .FontSize = 13, .FontWeight = FontWeights.SemiBold, .TextWrapping = TextWrapping.Wrap, .Foreground = Brushes.DimGray})
        Text.Children.Add(New TextBlock With {.Text = Node.Condition, .FontSize = 11, .Foreground = Accent(Node.IsConflict OrElse Node.NeedsAttention, Node.IsCandidate),
            .TextWrapping = TextWrapping.Wrap, .Margin = New Thickness(0, 3, 0, 0), .Visibility = If(Node.Condition = "", Visibility.Collapsed, Visibility.Visible)})
        Grid.SetColumn(Text, 2) : Content.Children.Add(Text)
        Return Content
    End Function

    Private Sub AddNode(Node As GraphNode, X As Double, Y As Double, Optional Focus As Boolean = False)
        Dim Content = NodeContent(Node)
        Dim Box As New Border With {.Width = NodeWidth, .Height = Node.Height, .CornerRadius = New CornerRadius(7), .BorderThickness = New Thickness(If(Focus, 2, 1)),
            .BorderBrush = Accent(Node.IsConflict OrElse Node.NeedsAttention, Node.IsCandidate), .Background = Brushes.White, .Padding = New Thickness(8, 5, 8, 5), .Tag = Node,
            .ToolTip = Node.Title & If(Node.Condition = "", "", vbCrLf & Node.Condition) & If(String.IsNullOrEmpty(Node.Details), "", vbCrLf & Node.Details),
            .Child = Content}
        If Focus Then Box.SetResourceReference(Border.BackgroundProperty, "ColorBrush8")
        Canvas.SetLeft(Box, X) : Canvas.SetTop(Box, Y) : GraphCanvas.Children.Add(Box)
        If Not String.IsNullOrEmpty(Node.FileName) AndAlso Not String.IsNullOrEmpty(Node.IconPath) Then IconImages.Add(Box, CType(Content.Children(0), Image))
    End Sub

    Private Shared Function IconKey(Node As GraphNode) As String
        Return Inspector.FilePathKey(Node.FileName).ToUpperInvariant() & "|" & Node.IconPath
    End Function

    Private Sub RefreshVisibleIcons()
        If Not IsLoaded Then Return
        For Each Pair In IconImages
            Dim Box = Pair.Key, Node = CType(Box.Tag, GraphNode)
            Dim Top = Canvas.GetTop(Box)
            If Top + Box.Height < GraphScroll.VerticalOffset OrElse Top > GraphScroll.VerticalOffset + GraphScroll.ViewportHeight Then Continue For
            Dim Picture As ImageSource = Nothing
            If IconCache.TryGetValue(IconKey(Node), Picture) Then
                If Picture IsNot Nothing Then Pair.Value.Source = Picture
            ElseIf PendingIcons.Add(Box) Then
                LoadIconAsync(Box, Node, IconGeneration)
            End If
        Next
    End Sub

    Private Async Sub LoadIconAsync(Box As Border, Node As GraphNode, Generation As Integer)
        Dim Picture As ImageSource = Nothing
        Try
            Await IconSlots.WaitAsync()
            Try
                Picture = Await Task.Run(Function() ModDependencyIcon.Read(Node.FileName, Node.IconPath))
            Finally
                IconSlots.Release()
            End Try
            If Dispatcher.HasShutdownStarted Then Return
            Await Dispatcher.InvokeAsync(Sub()
                                             PendingIcons.Remove(Box)
                                             If Generation = IconGeneration AndAlso IsLoaded Then
                                                 IconCache(IconKey(Node)) = Picture
                                                 If Picture IsNot Nothing Then IconImages(Box).Source = Picture
                                             End If
                                             RefreshVisibleIcons()
                                         End Sub)
        Catch
            '图标失败仅使用默认图标，不改变依赖关系。
        End Try
    End Sub

    Friend Sub LoadIconsForPreview()
        For Each Pair In IconImages
            Dim Node = CType(Pair.Key.Tag, GraphNode)
            Dim Picture = ModDependencyIcon.Read(Node.FileName, Node.IconPath)
            If Picture IsNot Nothing Then Pair.Value.Source = Picture
        Next
    End Sub

    Private Sub Connector(From As Point, Target As Point, Conflict As Boolean, Optional Possible As Boolean = False)
        Dim Figure As New PathFigure With {.StartPoint = From}
        Dim Half = (Target.X - From.X) / 2
        Figure.Segments.Add(New BezierSegment(New Point(From.X + If(Conflict, 26, Half), From.Y), New Point(Target.X + If(Conflict, 26, -Half), Target.Y), Target, True))
        Dim Line As New Shapes.Path With {.Data = New PathGeometry({Figure}), .Stroke = Accent(Conflict, Possible), .StrokeThickness = 1.5, .IsHitTestVisible = False}
        If Conflict OrElse Possible Then Line.StrokeDashArray = New DoubleCollection({4.0, 3.0})
        GraphCanvas.Children.Add(Line)
        GraphCanvas.Children.Add(New Polygon With {.Points = New PointCollection({Target, New Point(Target.X + If(Conflict, 6, -6), Target.Y - 4), New Point(Target.X + If(Conflict, 6, -6), Target.Y + 4)}),
            .Fill = Accent(Conflict, Possible), .IsHitTestVisible = False})
    End Sub
End Class
