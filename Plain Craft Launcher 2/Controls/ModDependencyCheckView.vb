Imports Inspector = PCL.ModDependencyInspector

Friend NotInheritable Class ModDependencyCheckView
    Inherits Grid

    Friend ReadOnly Property PreferredWidth As Double = 740
    Friend ReadOnly Groups As New StackPanel
    Friend ReadOnly Details As MyCard

    Friend Sub New(Report As Inspector.InspectionReport, Optional Plan As ModDependencyAutoFill.Plan = Nothing)
        Height = 430
        RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
        RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(1, GridUnitType.Star)})
        Dim Files = Report.Mods.Where(Function(Item) Item.Enabled AndAlso Not Item.IsNested).Select(Function(Item) Item.RootPath).Distinct(StringComparer.OrdinalIgnoreCase).Count()
        If Plan Is Nothing Then Plan = New ModDependencyAutoFill.Plan With {.Report = Report}
        Dim Covered As New HashSet(Of String)(Plan.Enables.SelectMany(Function(Item) Item.ResolvedIssues), StringComparer.Ordinal)
        Dim Actions As New List(Of String)
        If Plan.Enables.Count > 0 Then Actions.Add(Plan.Enables.Count & " 个前置已安装但禁用")
        If Plan.Items.Count > 0 Then Actions.Add(Plan.Items.Count & " 个必需前置待补全")
        If Actions.Count = 0 Then Actions.Add(Report.Issues.Where(Function(Item) Item.IsError).Count() & " 项需要处理")
        Dim Summary As New TextBlock With {.Text = "已检查 " & Files & " 个启用文件 · " & String.Join(" · ", Actions),
            .Margin = New Thickness(3, 0, 3, 12), .TextWrapping = TextWrapping.Wrap, .FontSize = 13}
        Summary.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1") : Children.Add(Summary)
        Dim Scroll As New MyScrollViewer With {.Content = Groups, .VerticalScrollBarVisibility = ScrollBarVisibility.Auto, .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled}
        Grid.SetRow(Scroll, 1) : Children.Add(Scroll)
        If Plan.Items.Count = 0 AndAlso Plan.Enables.Count = 0 AndAlso Report.OnlineAttempted Then
            Dim Reason = If(Plan.Notes.LastOrDefault(), If(Report.OnlineNotes.LastOrDefault(), "尚未匹配到可用的前置下载信息，请检查下载源连接后重试。"))
            Groups.Children.Add(New MyHint With {.Text = "暂不能自动补全：" & Reason, .Theme = MyHint.Themes.Yellow, .Margin = New Thickness(3, 3, 7, 10)})
        End If
        If Plan.Enables.Count > 0 Then
            Dim Entries = Plan.Enables.Select(Function(Item) Tuple.Create(Item.Name & "（已禁用）", "将启用：" & IO.Path.GetFileName(Item.Destination) & vbCrLf &
                "需要它：" & Referrers(Item.RequiredBy) & vbCrLf & "提供 " & Item.Requirements.Count & " 项依赖（含内嵌组件）", Item.Owner)).ToList()
            AddGroup("可启用的必需前置（" & Entries.Count & " 个文件）", Entries, False)
        End If
        If Plan IsNot Nothing AndAlso Plan.Items.Count > 0 Then
            Dim Entries = Plan.Items.Select(Function(Item) Tuple.Create(Item.Name, "候选：" & Item.Version.Display & vbCrLf & "需要它：" & Referrers(Item.RequiredBy) &
                If(Item.Alternatives.Count > 0, vbCrLf & "将校验实际兼容性，必要时尝试较早版本", ""), DirectCast(Nothing, Inspector.ModInfo))).ToList()
            AddGroup("可补全的必需前置（" & Entries.Count & "）", Entries, False)
        End If
        For Each Kind In {Inspector.IssueKind.MissingRequired, Inspector.IssueKind.VersionMismatch, Inspector.IssueKind.DuplicateId, Inspector.IssueKind.Incompatible}
            Dim Issues = Report.Issues.Where(Function(Item) Item.Kind = Kind AndAlso Not Covered.Contains(ModDependencyActivation.IssueKey(Item))).ToList()
            If Issues.Count = 0 Then Continue For
            Dim Title = If(Kind = Inspector.IssueKind.MissingRequired, "缺少依赖", If(Kind = Inspector.IssueKind.VersionMismatch, "版本不匹配", If(Kind = Inspector.IssueKind.DuplicateId, "重复模组", "模组冲突")))
            Dim Entries = Issues.GroupBy(Function(Item) If(Item.Owner?.RootPath, "") & "|" & If(Item.Owner?.Id, "")).Select(Function(Group)
                Dim Owner = Group.First().Owner
                Return Tuple.Create(If(Owner?.Name, If(Owner?.Id, "模组问题")), String.Join(vbCrLf, Group.Select(Function(Item) Describe(Item))), Owner)
            End Function).ToList()
            AddGroup(Title & "（" & Issues.Count & "）", Entries, True)
        Next
        Dim Notes = Report.Notices.Concat(If(Plan?.Notes, New List(Of String))).Distinct().ToList()
        If Notes.Count > 0 OrElse Report.MetadataIncomplete OrElse Report.OnlineNotes.Count > 0 Then
            Notes.AddRange(Report.OnlineNotes)
            If Report.MetadataIncomplete Then Notes.Insert(0, "部分模组声明未读完整，无法确认的项目不会自动安装。")
            Details = New MyCard With {.Title = "读取说明（" & Notes.Count & "）", .CanSwap = True, .Margin = New Thickness(3, 3, 7, 8)}
            Dim Text As New TextBlock With {.Text = String.Join(vbCrLf & vbCrLf, Notes), .Margin = New Thickness(15, 40, 15, 15), .TextWrapping = TextWrapping.Wrap, .FontSize = 12}
            Text.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3")
            Details.Children.Add(Text) : Details.SwapControl = Text : Details.IsSwapped = True
            Dim Caption = Details.MainTextBlock
            Groups.Children.Add(Details)
        End If
        Groups.Measure(New Size(PreferredWidth - 70, Double.PositiveInfinity))
        Height = Math.Min(430, Math.Max(110, Groups.DesiredSize.Height + 36))
    End Sub

    Private Shared Function Referrers(Names As List(Of String)) As String
        Return String.Join("、", Names.Take(3)) & If(Names.Count > 3, " 等 " & Names.Count & " 个模组", "")
    End Function

    Private Shared Function Describe(Issue As Inspector.InspectionIssue) As String
        Dim Requirement = Issue.Requirement
        If Requirement Is Nothing Then Return Issue.Message
        Dim Prefix = If(Issue.Kind = Inspector.IssueKind.MissingRequired, "需要：", If(Issue.Kind = Inspector.IssueKind.VersionMismatch, "要求：", "冲突："))
        Dim Expected = Requirement.Display
        If TypeOf Requirement.Versions Is JArray AndAlso DirectCast(Requirement.Versions, JArray).Count > 0 AndAlso DirectCast(Requirement.Versions, JArray).All(Function(Value) Value.Type = JTokenType.String) Then
            Expected = Requirement.Id & " " & String.Join(" 或 ", DirectCast(Requirement.Versions, JArray).Select(Function(Value) Value.ToString()))
        End If
        Dim Found = String.Join("、", Issue.RelatedMods.Select(Function(Item) If(Item.IsBuiltin, If(Item.Name, Item.Id), Item.Display) & " " & Item.Version))
        Dim DisabledAt = If(Issue.Message, "").IndexOf("已禁用：", StringComparison.Ordinal)
        Return If(Issue.IsError, "", "待确认 · ") & Prefix & Expected & If(Found = "", "", vbCrLf & "当前：" & Found) &
            If(DisabledAt < 0, "", vbCrLf & Issue.Message.Substring(DisabledAt))
    End Function

    Private Sub AddGroup(Title As String, Entries As List(Of Tuple(Of String, String, Inspector.ModInfo)), Warning As Boolean)
        Dim Card As New MyCard With {.Title = Title, .Margin = New Thickness(3, 3, 7, 10)}
        Dim Rows As New Primitives.UniformGrid With {.Columns = 2, .Margin = New Thickness(9, 38, 9, 10)}
        AddHandler Rows.SizeChanged, Sub() Rows.Columns = If(Rows.ActualWidth < 510, 1, 2)
        For Each Entry In Entries
            Dim Content As New Grid
            Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(28)})
            Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
            Dim Picture = If(Entry.Item3 Is Nothing, Nothing, ModDependencyIcon.Read(Entry.Item3.RootPath, Entry.Item3.IconPath))
            Content.Children.Add(New Image With {.Source = If(Picture, CType(New MyBitmap(PathImage & "Icons/NoIcon.png"), ImageSource)), .Width = 28, .Height = 28,
                .VerticalAlignment = VerticalAlignment.Top, .Clip = New RectangleGeometry(New Rect(0, 0, 28, 28), 5, 5)})
            Dim Text As New StackPanel With {.Margin = New Thickness(9, 0, 0, 0)}
            Dim Name As New TextBlock With {.Text = Entry.Item1, .TextWrapping = TextWrapping.Wrap, .FontWeight = FontWeights.SemiBold, .FontSize = 13}
            Name.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1") : Text.Children.Add(Name)
            Dim Reason As New TextBlock With {.Text = Entry.Item2, .TextWrapping = TextWrapping.Wrap, .Margin = New Thickness(0, 5, 0, 0), .FontSize = 12}
            Reason.SetResourceReference(TextBlock.ForegroundProperty, If(Warning, "ColorBrushRedLight", "ColorBrush3")) : Text.Children.Add(Reason)
            Grid.SetColumn(Text, 1) : Content.Children.Add(Text)
            Dim Box As New Border With {.Child = Content, .BorderThickness = New Thickness(1), .CornerRadius = New CornerRadius(5), .Padding = New Thickness(10), .Margin = New Thickness(4), .Background = Brushes.White, .ToolTip = Entry.Item3?.RootPath}
            Box.SetResourceReference(Border.BorderBrushProperty, If(Warning, "ColorBrushRedLight", "ColorBrush5"))
            Rows.Children.Add(Box)
        Next
        Card.Children.Add(Rows)
        Dim Caption = Card.MainTextBlock
        Groups.Children.Add(Card)
    End Sub
End Class
