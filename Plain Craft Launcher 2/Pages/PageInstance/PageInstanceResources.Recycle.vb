Imports ResourceRow = PCLCS.IResourcePageRow(Of PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo)
Imports ResourceTile = PCLCS.IResourcePageThumbnail(Of PCL.InstanceResourceEntry)
Imports ResourceRowActions = PCLCS.ResourcePageRowActions(Of PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo)

Partial Public Class PageInstanceResources
    Friend Property IsRecycleMode As Boolean
        Get
            Return Controller.IsRecycleMode
        End Get
        Set(Value As Boolean)
            Controller.IsRecycleMode = Value
        End Set
    End Property
    Friend Sub SetRecycleMode(Value As Boolean)
        Controller.SetRecycleMode(Value)
    End Sub
    Private Sub ReturnToResources() Handles BtnRecycleBack.Click
        Controller.ReturnToResources()
    End Sub
    Private Sub RestoreDeleted() Handles BtnRestore.Click, BtnHintRestore.Click
        Controller.SetRecycleMode(Not Controller.IsRecycleMode)
    End Sub
    Private Sub RestoreRecycleSelection(Optional Items As IEnumerable(Of InstanceResourceEntry) = Nothing)
        Controller.RestoreRecycleSelection(Items)
    End Sub
    Private Sub RefreshResourceToolbar() Handles PanManageButtons.SizeChanged
        Controller?.RefreshResourceToolbar()
    End Sub

    Private NotInheritable Class RowAdapter
        Implements ResourceRow
        Private ReadOnly Row As MyLocalModItem
        Public Sub New(Page As PageInstanceResources, Actions As ResourceRowActions)
            Row = New MyLocalModItem With {.SelectionBar = Page.CardSelect}
            AddHandler Row.Changed, Sub(Sender, e)
                                        If Actions.SelectionChanged(Me) Then e.Handled = True
                                    End Sub
            AddHandler Row.Click, Sub() Actions.Click(Me)
            AddHandler Row.MouseRightButtonUp, Sub(Sender, e) Actions.Details(Row.ResourceEntry)
            Row.ResourceUpdateRequested = Sub() Actions.Update(Row.ResourceEntry)
            Row.ResourceDetailsRequested = Sub() Actions.Details(Row.ResourceEntry)
            Row.Buttons = NativeRowButtons(Page.Kind, Row, Actions)
            AddHandler Row.MouseLeftButtonDown, Sub(Sender, e)
                                                   If e.ClickCount = 2 Then Actions.EnterFolder(Row.ResourceEntry)
                                               End Sub
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements ResourceRow.Element
            Get
                Return Row
            End Get
        End Property
        Public ReadOnly Property Entry As InstanceResourceEntry Implements ResourceRow.Entry
            Get
                Return Row.ResourceEntry
            End Get
        End Property
        Public Property Checked As Boolean Implements ResourceRow.Checked
            Get
                Return Row.Checked
            End Get
            Set(Value As Boolean)
                Row.Checked = Value
            End Set
        End Property
        Public Sub Bind(Value As InstanceResourceEntry, Info As InstanceResourceOnlineInfo) Implements ResourceRow.Bind
            Row.BindResource(Value, Info)
        End Sub
    End Class

    Private Shared Function NativeRowButtons(Kind As InstanceResourceKind, Row As MyLocalModItem, Actions As ResourceRowActions) As List(Of MyIconButton)
        If Actions.IsRecycle() Then
            Dim Restore As New MyIconButton With {.Logo = Logo.IconButtonRefresh, .ToolTip = "还原"}
            AddHandler Restore.Click, Sub() Actions.Restore(Row.ResourceEntry)
            Dim Purge As New MyIconButton With {.Logo = Logo.IconButtonDelete, .ToolTip = "彻底删除"}
            AddHandler Purge.Click, Sub() Actions.Purge(Row.ResourceEntry)
            Return New List(Of MyIconButton) From {Restore, Purge}
        End If
        Dim Buttons As New List(Of MyIconButton)
        If Actions.CanPlay() Then
            Dim Play As New MyIconButton With {.Logo = LocalResourceUi.PlayLogo, .LogoScale = 0.9, .ToolTip = "启动此版本并直接进入存档"}
            AddHandler Play.Click, Sub() Actions.Play(Row.ResourceEntry)
            Buttons.Add(Play)
        End If
        Dim Details As New MyIconButton With {.Logo = Logo.IconButtonInfo, .ToolTip = If(Kind = InstanceResourceKind.Screenshots, "预览截图", "详情")}
        AddHandler Details.Click, Sub() Actions.Details(Row.ResourceEntry)
        Buttons.Add(Details)
        Dim Open As New MyIconButton With {.Logo = Logo.IconButtonOpen, .LogoScale = 1.05, .ToolTip = "打开文件位置"}
        AddHandler Open.Click, Sub() Actions.Open(Row.ResourceEntry)
        Buttons.Add(Open)
        Dim Rename As New MyIconButton With {.Logo = Logo.IconButtonEdit, .ToolTip = "重命名"}
        AddHandler Rename.Click, Sub() Actions.Rename(Row.ResourceEntry)
        Buttons.Add(Rename)
        Dim Delete As New MyIconButton With {.Logo = Logo.IconButtonDelete, .ToolTip = "删除"}
        AddHandler Delete.Click, Sub() Actions.Delete(Row.ResourceEntry)
        Buttons.Add(Delete)
        Return Buttons
    End Function

    Private NotInheritable Class ThumbnailAdapter
        Implements ResourceTile
        Private ReadOnly Tile As MyScreenshotItem
        Public Sub New(Changed As Action(Of ResourceTile), Clicked As Action(Of InstanceResourceEntry, Integer), Viewport As FrameworkElement)
            Tile = New MyScreenshotItem()
            AddHandler Tile.Changed, Sub() Changed(Me)
            AddHandler Tile.ClickRequested, Sub(Sender, Count) Clicked(Sender.Entry, Count)
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements ResourceTile.Element
            Get
                Return Tile
            End Get
        End Property
        Public ReadOnly Property Entry As InstanceResourceEntry Implements ResourceTile.Entry
            Get
                Return Tile.Entry
            End Get
        End Property
        Public Property Checked As Boolean Implements ResourceTile.Checked
            Get
                Return Tile.Checked
            End Get
            Set(Value As Boolean)
                Tile.Checked = Value
            End Set
        End Property
        Public Sub Bind(Value As InstanceResourceEntry) Implements ResourceTile.Bind
            Tile.Bind(Value)
        End Sub
        Public Sub UpdateViewport(Viewport As FrameworkElement) Implements ResourceTile.UpdateViewport
            Tile.UpdateViewport(Viewport)
        End Sub
        Public Sub ReleaseThumbnail() Implements ResourceTile.ReleaseThumbnail
            Tile.ReleaseThumbnail()
        End Sub
    End Class
End Class
