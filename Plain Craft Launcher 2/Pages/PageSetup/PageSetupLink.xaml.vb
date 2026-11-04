Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageSetupLink
    Private _controller As LegacyAuxiliaryBasicController
    Friend ReadOnly Property Controller As LegacyAuxiliaryBasicController
        Get
            If _controller Is Nothing Then _controller=New LegacyAuxiliaryBasicController(AuxiliaryPageRuntime.Basic,New LegacyAuxiliaryBasicView With {.Page=Function() Me,.Scroll=Function() PanBack})
            Return _controller
        End Get
    End Property

    Private Sub PageSetupLink_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.LinkLoaded()
    End Sub

    Public Sub Reload()
        Controller.LinkReload()
    End Sub

    Public Sub Reset()
        Controller.LinkReset()
    End Sub

End Class
