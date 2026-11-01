Imports System.IO
Imports System.Threading

Friend NotInheritable Class MapDownloadInstaller
    Friend Const DownloadFolderName As String = "PCLLFWorldDownload"

    Friend Shared Function GetDownloadFolder(SavesFolder As String) As String
        Dim Root = Path.GetFullPath(SavesFolder), Folder = Path.Combine(Root, DownloadFolderName)
        InstanceResourceStore.EnsureWithin(Root, Folder)
        If File.Exists(Path.Combine(Folder, "level.dat")) Then Throw New IOException("地图压缩包目录与现有存档同名，请先重命名该存档。")
        Directory.CreateDirectory(Folder)
        Return Folder
    End Function

    Friend Shared Function Install(ArchivePath As String, SavesFolder As String, Token As CancellationToken) As String
        Token.ThrowIfCancellationRequested()
        Dim Full = Path.GetFullPath(ArchivePath), Root = Path.GetFullPath(SavesFolder)
        InstanceResourceStore.EnsureWithin(Path.GetDirectoryName(Full), Full)
        Dim Name = Path.GetFileNameWithoutExtension(Full).TrimEnd(" "c, "."c)
        If Name.Length > 110 Then Name = Name.Substring(0, 110).TrimEnd(" "c, "."c)
        Name = InstanceResourceStore.SafeName(Name)
        Dim Parent = If(Path.GetDirectoryName(Root.TrimEnd("\"c)), Root)
        If String.IsNullOrEmpty(Parent) Then Parent = Root
        Dim Stage = Path.Combine(Parent, ".pcl-import-" & Guid.NewGuid().ToString("N"))
        InstanceResourceStore.EnsureWithin(Parent, Root, True)
        InstanceResourceStore.EnsureWithin(Parent, Stage)
        Directory.CreateDirectory(Root)
        Try
            Dim World = InstanceResourceStore.ImportWorld(Stage, Full, "world", Token)
            For Index = 1 To 10000
                Token.ThrowIfCancellationRequested()
                Dim Destination = Path.Combine(Root, Name & If(Index = 1, "", " (" & Index & ")"))
                InstanceResourceStore.EnsureWithin(Root, Destination)
                If Directory.Exists(Destination) OrElse File.Exists(Destination) Then Continue For
                Try
                    Directory.Move(World, Destination)
                    Return Destination
                Catch ex As IOException When Directory.Exists(Destination) OrElse File.Exists(Destination)
                End Try
            Next
            Throw New IOException("没有可用的地图存档名称，原压缩包已保留。")
        Finally
            InstanceResourceStore.DeleteStage(Parent, Stage)
        End Try
    End Function
End Class
