Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyScrollViewer
    Inherits MyScrollViewerView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Shadows ScrollBar As MyScrollBar
    Public Shadows Sub PerformVerticalOffsetDelta(Delta As Double)
        MyBase.PerformVerticalOffsetDelta(Delta)
    End Sub
    Protected Overrides ReadOnly Property VerticalBar As MyScrollBarView
        Get
            Return ScrollBar
        End Get
    End Property
    Protected Overrides Sub AssignBar(Value As MyScrollBarView)
        ScrollBar = DirectCast(Value, MyScrollBar)
    End Sub
    Protected Overrides ReadOnly Property HasMainWindow As Boolean
        Get
            Return FrmMain IsNot Nothing
        End Get
    End Property
    Protected Overrides Sub RefreshBack()
        FrmMain.BtnExtraBack.ShowRefresh()
    End Sub
End Class
