Public Class MyScreenshotItem
    Friend Entry As InstanceResourceEntry
    Private ReadOnly AnimationId As Integer = GetUuid()
    Private ReadOnly Controller As PCLCS.ResourceThumbnailController
    Friend Event Changed(Sender As MyScreenshotItem)
    Friend Event ClickRequested(Sender As MyScreenshotItem, Count As Integer)

    Public Sub New()
        InitializeComponent()
        Dim View As New PCLCS.ResourceThumbnailController.View(Me, Thumbnail, LabName, LabInfo, SelectionBorder, Check,
            Function() Check.Checked, Sub(Value) Check.Checked = Value,
            Function() Entry, Sub(Value) Entry = DirectCast(Value, InstanceResourceEntry))
        Dim Host As New PCLCS.ResourceThumbnailController.Host(
            Function(FileName, Width) InstanceResourceStore.LoadImage(FileName, Width),
            Sub(Work) RunInUi(Work),
            Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            Sub(Back, Hovered, Selected, Pressed)
                Dim Ani As New List(Of AniData)
                LocalResourceUi.AppendItemSelection(Ani, Back, Hovered, Selected, Pressed, False)
                AniStart(Ani, "Screenshot Selection " & AnimationId)
            End Sub,
            Function() AniControlEnabled = 0,
            Sub() AniStop("Screenshot Selection " & AnimationId))
        Controller = New PCLCS.ResourceThumbnailController(View, Host)
        AddHandler Controller.Changed,
            Sub()
                RaiseEvent Changed(Me)
            End Sub
        AddHandler Controller.ClickRequested,
            Sub(Count)
                RaiseEvent ClickRequested(Me, Count)
            End Sub
    End Sub

    Public Property Checked As Boolean
        Get
            Return Controller.Checked
        End Get
        Set(Value As Boolean)
            Controller.Checked = Value
        End Set
    End Property
    Friend Sub Bind(Value As InstanceResourceEntry)
        Controller.Bind(Value)
    End Sub
    Private Sub CheckChanged() Handles Check.Change
        Controller?.CheckChanged()
    End Sub
    Friend Sub UpdateViewport(Viewport As FrameworkElement)
        Controller.UpdateViewport(Viewport)
    End Sub
    Friend Sub ReleaseThumbnail()
        Controller.ReleaseThumbnail()
    End Sub
End Class
