Option Strict On
Imports PCLLF.Presentation.Controls

Public Class MyResizer
    Inherits WindowResizer
    Public Sub New(Target As Window)
        MyBase.New(Target, AddressOf GetWPFSize, Sub(Work, Name) RunInNewThread(Work, Name))
    End Sub
End Class
