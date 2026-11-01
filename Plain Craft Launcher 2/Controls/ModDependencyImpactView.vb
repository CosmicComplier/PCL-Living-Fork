Imports Inspector = PCL.ModDependencyInspector
Imports System.Windows.Media

Friend NotInheritable Class ModDependencyImpactView
    Inherits Grid
    Friend Class Item
        Public Name As String
        Public FileName As String
        Public Icon As ImageSource
        Public Reasons As New List(Of String)
    End Class
    Friend ReadOnly Cards As New Grid
    Friend ReadOnly Property PreferredWidth As Double = 760

    Friend Shared Function Collect(Report As Inspector.InspectionReport, Paths As IEnumerable(Of String)) As List(Of Item)
        Dim Removed As New HashSet(Of String)(Paths.Select(AddressOf Inspector.FilePathKey), StringComparer.OrdinalIgnoreCase)
        Dim Result As New Dictionary(Of String, Item)(StringComparer.OrdinalIgnoreCase)
        Dim Add As Action(Of String, String) = Sub(FileName, Reason)
            Dim Key = Inspector.FilePathKey(FileName), Entry As Item = Nothing
            If Not Result.TryGetValue(Key, Entry) Then
                Dim Owner = Report.Mods.FirstOrDefault(Function(ModInfo) Not ModInfo.IsNested AndAlso Inspector.SameFilePath(ModInfo.RootPath, FileName))
                Entry = New Item With {.Name = If(Owner?.Name, IO.Path.GetFileName(FileName)), .FileName = FileName,
                    .Icon = ModDependencyIcon.Read(FileName, Owner?.IconPath)}
                Result.Add(Key, Entry)
            End If
            If Not Entry.Reasons.Contains(Reason) Then Entry.Reasons.Add(Reason)
        End Sub
        For Each Impact In Inspector.GetImpacts(Report, Paths)
            Add(Impact.Mod.RootPath, If(Impact.IsDefinite, "", "待确认 · ") & Impact.Reason)
        Next
        For Each File In Report.OnlineFiles.Where(Function(Value) Value.Enabled AndAlso Value.IsSupplementTarget AndAlso Not Removed.Contains(Inspector.FilePathKey(Value.FullPath)))
            For Each Requirement In File.Matches.Where(Function(Match) Match.IsExact).SelectMany(Function(Match) Match.Dependencies).
                Where(Function(Value) Value.Kind = "required" AndAlso Value.EnabledPaths.Count > 0 AndAlso Value.EnabledPaths.All(Function(Path) Removed.Contains(Inspector.FilePathKey(Path))))
                Add(File.FullPath, Requirement.Platform & " 平台前置：" & If(Requirement.ProjectName, Requirement.ProjectId))
            Next
        Next
        Return Result.Values.ToList()
    End Function

    Friend Sub New(Items As List(Of Item), ActionName As String)
        Height = Math.Min(440, 55 + Math.Ceiling(Items.Count / 2.0) * 100)
        RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
        RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(1, GridUnitType.Star)})
        Children.Add(New TextBlock With {.Text = "以下 " & Items.Count & " 个模组可能受影响。仍然" & ActionName & "所选模组吗？", .TextWrapping = TextWrapping.Wrap, .Margin = New Thickness(4, 0, 4, 10), .FontSize = 13, .Foreground = Brushes.DimGray})
        Cards.ColumnDefinitions.Add(New ColumnDefinition()) : Cards.ColumnDefinitions.Add(New ColumnDefinition())
        Dim Scroll As New ScrollViewer With {.Content = Cards, .VerticalScrollBarVisibility = ScrollBarVisibility.Auto, .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled}
        Grid.SetRow(Scroll, 1) : Children.Add(Scroll)
        For Index = 0 To Items.Count - 1
            If Index Mod 2 = 0 Then Cards.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            Dim Entry = Items(Index), Content As New Grid
            Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(28)})
            Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(8)})
            Content.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
            Content.Children.Add(New Image With {.Source = If(Entry.Icon, CType(New MyBitmap(PathImage & "Icons/NoIcon.png"), ImageSource)),
                .Width = 28, .Height = 28, .Stretch = Stretch.Uniform, .VerticalAlignment = VerticalAlignment.Top, .Clip = New RectangleGeometry(New Rect(0, 0, 28, 28), 5, 5)})
            Dim Text As New StackPanel
            Text.Children.Add(New TextBlock With {.Text = Entry.Name, .TextWrapping = TextWrapping.Wrap, .FontSize = 13, .FontWeight = FontWeights.SemiBold, .Foreground = Brushes.DimGray})
            Text.Children.Add(New TextBlock With {.Text = String.Join(vbCrLf, Entry.Reasons), .TextWrapping = TextWrapping.Wrap, .FontSize = 11, .Foreground = Brushes.Firebrick, .Margin = New Thickness(0, 5, 0, 0)})
            Grid.SetColumn(Text, 2) : Content.Children.Add(Text)
            Dim Box As New Border With {.Child = Content, .BorderBrush = Brushes.LightCoral, .BorderThickness = New Thickness(1), .CornerRadius = New CornerRadius(6),
                .Background = Brushes.White, .Padding = New Thickness(9), .Margin = New Thickness(4), .MinHeight = 64, .Tag = Entry, .ToolTip = Entry.FileName}
            Grid.SetColumn(Box, Index Mod 2) : Grid.SetRow(Box, Index \ 2) : Cards.Children.Add(Box)
        Next
    End Sub
End Class
