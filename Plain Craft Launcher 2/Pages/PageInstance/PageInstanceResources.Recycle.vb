Imports System.IO

Partial Public Class PageInstanceResources
    Friend Property IsRecycleMode As Boolean

    Friend Sub SetRecycleMode(Value As Boolean)
        If Busy OrElse CurrentRoot Is Nothing Then Return
        ResourceLoader.Cancel()
        CancelOnline()
        ClearSelection()
        IsRecycleMode = Value
        Filter = 0
        Entries.Clear()
        Rows.Clear() : PanList.Children.Clear()
        For Each Tile In ScreenshotRows.Values
            Tile.ReleaseThumbnail()
        Next
        ScreenshotRows.Clear() : PanScreenshots.Children.Clear()
        WorldBackupPoints.Clear() : ResetWorldGroups()
        Online.Clear()
        SearchBox.Text = ""
        CurrentFolder = CurrentRoot
        SetStatus(Nothing)
        ApplyRecycleMode()
        PanBack.ScrollToHome()
        RefreshResources()
    End Sub

    Private Sub ApplyRecycleMode()
        BtnRecycleBack.Visibility = If(IsRecycleMode, Visibility.Visible, Visibility.Collapsed)
        BtnRecycleBack.IsEnabled = Not Busy AndAlso CurrentRoot IsNot Nothing
        BtnRestore.Visibility = If(IsRecycleMode, Visibility.Collapsed, Visibility.Visible)
        BtnRestore.ToolTip = "查看删除的资源，可还原或彻底删除"
        BtnOpenFolder.Text = If(IsRecycleMode, "打开回收站文件夹", "打开文件夹")
        BtnOpenFolder.ToolTip = If(IsRecycleMode, RecycleRoot, CurrentFolder)
        BtnOpenFolder.MinWidth = If(IsRecycleMode, 150, 110)
        BtnImportFile.Text = If(IsRecycleMode, "还原所选", "从文件安装")
        BtnImportFile.ToolTip = If(IsRecycleMode, "还原到删除前的位置，同名资源不覆盖", "选择本地资源文件安装，也可拖入列表")
        BtnBackupAll.Text = If(IsRecycleMode, "清空回收站", "立即备份")
        BtnExport.Text = If(IsRecycleMode, "还原", "导出")
        BtnExport.Logo = If(IsRecycleMode, Logo.IconButtonRefresh, Logo.IconButtonSave)
        BtnDelete.Text = If(IsRecycleMode, "彻底删除", "删除")
        CardList.Title = If(IsRecycleMode, "回收站", " ")
        PanFilter.Margin = New Thickness(15, If(IsRecycleMode, 40, 13), 0, 0)
        PanResourceList.Margin = New Thickness(20, If(IsRecycleMode, 75, 48), 18, 22)
        If Not IsRecycleMode Then
            RefreshResourceToolbar()
            Return
        End If
        CardList.Title = "回收站"
        SearchBox.HintText = "搜索已删除的资源"
        BackupSettings.Visibility = Visibility.Collapsed
        LabBackupStatus.Visibility = Visibility.Collapsed
        BtnBackupAll.Visibility = Visibility.Visible
        BtnBackupAll.IsEnabled = Not Busy AndAlso Entries.Count > 0
        BtnImportFile.IsEnabled = Not Busy AndAlso SelectedPaths.Count > 0
        PanManageButtons.ColumnDefinitions(4).SharedSizeGroup = "Button"
        BtnDownload.Visibility = Visibility.Collapsed
        PanManageButtons.ColumnDefinitions(2).SharedSizeGroup = Nothing
        BtnUpdate.Visibility = Visibility.Collapsed
        BtnUp.Visibility = Visibility.Collapsed
        BtnBackupRestore.Visibility = Visibility.Collapsed
        LabEmpty.Text = If(SearchBox.Text = "", "回收站为空。", "没有找到匹配的删除记录。")
        BtnFilterUpdates.Visibility = Visibility.Collapsed
        RefreshResourceToolbar()
    End Sub

    Private ToolbarLayoutActive As Boolean
    Private Sub RefreshResourceToolbar() Handles PanManageButtons.SizeChanged
        If ToolbarLayoutActive OrElse PanManageButtons.ActualWidth <= 0 Then Return
        ToolbarLayoutActive = True
        Try
            Dim Buttons = PanManageButtons.Children.OfType(Of MyButton)().Where(Function(Button) Button.Visibility = Visibility.Visible).ToList()
            Dim Columns = 1
            For Candidate = Math.Min(Buttons.Count, PanManageButtons.ColumnDefinitions.Count) To 1 Step -1
                Dim Widths(Candidate - 1) As Double
                For Index = 0 To Buttons.Count - 1
                    Dim Button = Buttons(Index)
                    Widths(Index Mod Candidate) = Math.Max(Widths(Index Mod Candidate), Button.MinWidth + Button.Margin.Left + Button.Margin.Right)
                Next
                If Widths.Sum() <= PanManageButtons.ActualWidth Then
                    Columns = Candidate
                    Exit For
                End If
            Next
            For Index = 0 To PanManageButtons.ColumnDefinitions.Count - 1
                PanManageButtons.ColumnDefinitions(Index).SharedSizeGroup = Nothing
                PanManageButtons.ColumnDefinitions(Index).Width = If(Index < Columns, GridLength.Auto, New GridLength(0))
            Next
            PanManageButtons.RowDefinitions.Clear()
            Dim Rows = CInt(Math.Ceiling(Buttons.Count / CDbl(Columns)))
            For Index = 1 To Rows
                PanManageButtons.RowDefinitions.Add(New RowDefinition With {.Height = New GridLength(45)})
            Next
            For Index = 0 To Buttons.Count - 1
                Grid.SetColumn(Buttons(Index), Index Mod Columns) : Grid.SetRow(Buttons(Index), Index \ Columns)
                Buttons(Index).Height = 35 : Buttons(Index).VerticalAlignment = VerticalAlignment.Top
            Next
            PanManageButtons.Height = Math.Max(35, Rows * 45 - 10)
        Finally
            ToolbarLayoutActive = False
        End Try
    End Sub

    Private Sub ReturnToResources() Handles BtnRecycleBack.Click
        SetRecycleMode(False)
    End Sub

    Private Function RecycleButtons(Row As MyLocalModItem) As List(Of MyIconButton)
        Dim Restore As New MyIconButton With {.Logo = Logo.IconButtonRefresh, .ToolTip = "还原"}
        AddHandler Restore.Click, Sub() RestoreRecycleSelection({Row.ResourceEntry})
        Dim Delete As New MyIconButton With {.Logo = Logo.IconButtonDelete, .ToolTip = "彻底删除"}
        AddHandler Delete.Click, Sub() PurgeRecycleSelection({Row.ResourceEntry})
        Return New List(Of MyIconButton) From {Restore, Delete}
    End Function

    Private Sub RestoreRecycleSelection(Optional Items As IEnumerable(Of InstanceResourceEntry) = Nothing)
        If Not CanChange() Then Return
        Dim Ids = If(Items, Selection()).Select(Function(Item) Item.RecycleId).ToArray()
        If Ids.Length = 0 Then Return
        Dim Root = CurrentRoot, Recycle = RecycleRoot
        RunOperation("还原资源", Sub(Token) ResourceRecycleStore.Restore(Root, Recycle, Ids, Token))
    End Sub

    Private Sub PurgeRecycleSelection(Items As IEnumerable(Of InstanceResourceEntry))
        If Not CanChange() Then Return
        Dim SelectedItems = Items.ToList(), Root = CurrentRoot, Recycle = RecycleRoot
        If SelectedItems.Count = 0 Then Return
        If MyMsgBox("将永久删除选中的 " & SelectedItems.Count & " 个资源及其所有内容。此操作无法从回收站还原。", "彻底删除资源", "彻底删除", "取消", IsWarn:=True) <> 1 Then Return
        Dim Ids = SelectedItems.Select(Function(Item) Item.RecycleId).ToArray()
        RunOperation("彻底删除资源", Sub(Token) ResourceRecycleStore.Purge(Root, Recycle, Ids, Token))
    End Sub
End Class
