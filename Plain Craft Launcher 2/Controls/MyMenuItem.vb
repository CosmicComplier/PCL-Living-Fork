Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyMenuItem
    Inherits MyMenuItemView
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Protected Overrides ReadOnly Property LauncherTransparency As Integer
        Get
            Return Settings.Get(Of Integer)("UiLauncherTransparent")
        End Get
    End Property
End Class
