Imports System
Imports System.Collections.Generic
Imports PCLCS
Friend NotInheritable Class ToolboxDownloadSession
    Inherits LegacyToolboxDownloadSession
    Private Sub New(Target As String)
        MyBase.New(Target)
    End Sub
    Friend Shared Shadows Function ParseUrl(Value As String) As Uri
        Return LegacyToolboxDownloadSession.ParseUrl(Value)
    End Function
    Friend Shared Shadows Function ValidateName(Value As String) As String
        Return LegacyToolboxDownloadSession.ValidateName(Value)
    End Function
    Friend Shared Shadows Function SuggestName(Value As String) As String
        Return LegacyToolboxDownloadSession.SuggestName(Value)
    End Function
    Friend Shared Shadows Function ResolveTarget(Folder As String, Name As String) As String
        Return LegacyToolboxDownloadSession.ResolveTarget(Folder,Name)
    End Function
    Friend Shared Shadows Function Create(Folder As String, Name As String) As ToolboxDownloadSession
        Return LegacyToolboxDownloadSession.Create(Folder,Name,Function(Target) New ToolboxDownloadSession(Target))
    End Function
    Friend Shadows Sub Commit(AllowOverwrite As Boolean, ShouldCancel As Func(Of Boolean))
        MyBase.Commit(AllowOverwrite,ShouldCancel)
    End Sub
    Friend Shadows Sub OnCanceledMergeFinished()
        MyBase.OnCanceledMergeFinished()
    End Sub
    Friend Shadows Sub Complete(Success As Boolean)
        MyBase.Complete(Success)
    End Sub
    Friend Shadows ReadOnly Property RecoveryPath As String
        Get
            Return MyBase.RecoveryPath
        End Get
    End Property
    Friend Shadows ReadOnly Property UnrevertedPath As String
        Get
            Return MyBase.UnrevertedPath
        End Get
    End Property
End Class
