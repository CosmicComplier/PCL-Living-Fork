Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageOtherAbout
    Private _controller As LegacyAuxiliaryBasicController
    Friend ReadOnly Property Controller As LegacyAuxiliaryBasicController
        Get
            If _controller Is Nothing Then _controller=New LegacyAuxiliaryBasicController(AuxiliaryPageRuntime.Basic,New LegacyAuxiliaryBasicView With {.Page=Function() Me,.Scroll=Function() PanBack,.About=Function() ItemAboutPcl})
            Return _controller
        End Get
    End Property

    Private Sub PageOtherAbout_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.AboutLoaded()
    End Sub

    Private Sub BtnAboutUpdate_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAboutUpdate.Click
        Controller.CheckUpdate()
    End Sub

End Class
