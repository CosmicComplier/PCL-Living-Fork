Imports PCLCS
Imports PCLLF.Presentation.Controls

' Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
Public Class PageOtherTest
    Public Shared Sub Jrrp()
        AuxiliaryPageRuntime.Tools.Jrrp()
    End Sub

    Public Shared Function GetRandomCave() As String
        Return AuxiliaryPageRuntime.Tools.GetRandomCave()
    End Function

    Public Shared Function GetRandomHint() As String
        Return AuxiliaryPageRuntime.Tools.GetRandomHint()
    End Function

    Public Shared Function GetRandomPresetHint() As String
        Return AuxiliaryPageRuntime.Tools.GetRandomPresetHint()
    End Function

End Class
