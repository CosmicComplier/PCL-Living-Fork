Imports System.IO
Imports System.Threading

Friend NotInheritable Class MapDownloadInstaller
    Private Shared ReadOnly Files As New PCLCS.MapDownloadFiles(AddressOf PathUtils.RemoveExtendedPrefix)
    Friend Const DownloadFolderName As String = PCLCS.MapDownloadFiles.DownloadFolderName

    Friend Shared Function GetDownloadFolder(SavesFolder As String) As String
        Return Files.GetDownloadFolder(SavesFolder)
    End Function

    Friend Shared Function Install(ArchivePath As String, SavesFolder As String, Token As CancellationToken) As String
        Return Files.Install(ArchivePath, SavesFolder, Token)
    End Function
End Class
