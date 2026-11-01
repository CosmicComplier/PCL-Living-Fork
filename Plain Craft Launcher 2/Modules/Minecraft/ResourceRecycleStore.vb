Imports System.IO
Imports System.Threading
Imports System.Text.RegularExpressions

Friend NotInheritable Class ResourceRecycleStore
    Friend Class Record
        Public Id As String
        Public Box As String
        Public Destination As String
        Public Entry As InstanceResourceEntry
    End Class

    Private Shared Function ReadRecord(Root As String, Recycle As String, Id As String) As Record
        If Not Regex.IsMatch(If(Id, ""), "\A[0-9a-f]{32}\z") Then Throw New IOException("回收站记录编号无效。")
        If (File.GetAttributes(Recycle) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("不操作链接形式的回收站。")
        Dim Box = Path.Combine(Recycle, Id), Meta = Path.Combine(Box, "resource.json"), Payload = Path.Combine(Box, "content")
        InstanceResourceStore.EnsureWithin(Recycle, Meta)
        InstanceResourceStore.EnsureWithin(Recycle, Payload)
        If New FileInfo(Meta).Length > 65536 Then Throw New IOException("回收站记录过大。")
        Dim Json = JObject.Parse(File.ReadAllText(Meta))
        If {"root", "name", "folder"}.Any(Function(Key) Json(Key) Is Nothing OrElse Json(Key).Type <> JTokenType.String) OrElse
            Json("directory") Is Nothing OrElse Json("directory").Type <> JTokenType.Boolean Then Throw New IOException("回收站记录格式无效。")
        If Not PathUtils.IsSame(Root, Json("root")?.ToString()) Then Throw New IOException("此记录不属于当前资源目录。")
        Dim Name = InstanceResourceStore.SafeName(Json("name").ToString())
        Dim Destination = Path.Combine(Json("folder").ToString(), Name)
        InstanceResourceStore.EnsureWithin(Root, Destination)
        Dim IsDirectory = Json("directory").ToObject(Of Boolean)()
        If If(IsDirectory, Not Directory.Exists(Payload), Not File.Exists(Payload)) Then Throw New IOException("回收站内容已不存在或类型已变化。")
        Return New Record With {.Id = Id, .Box = Box, .Destination = Destination, .Entry = New InstanceResourceEntry With {
            .Name = Name, .FullPath = Payload, .RecycleId = Id, .IsDirectory = IsDirectory,
            .CreatedUtc = Directory.GetCreationTimeUtc(Box), .ModifiedUtc = File.GetLastWriteTimeUtc(Payload),
            .Length = If(IsDirectory, 0, New FileInfo(Payload).Length),
            .Description = "删除于 " & Directory.GetCreationTime(Box).ToString("yyyy-MM-dd HH:mm") & " · 原位置：" & Path.GetDirectoryName(Destination)}}
    End Function

    Friend Shared Function List(Root As String, Recycle As String, Kind As InstanceResourceKind) As List(Of InstanceResourceEntry)
        Dim Result As New List(Of InstanceResourceEntry)
        If Not Directory.Exists(Recycle) Then Return Result
        For Each Box In Directory.EnumerateDirectories(Recycle)
            Try
                Dim Item = ReadRecord(Root, Recycle, Path.GetFileName(Box)).Entry
                If Kind = InstanceResourceKind.Saves Then Item.Icon = InstanceResourceStore.LoadImage(Path.Combine(Item.FullPath, "icon.png"), 40)
                If Kind = InstanceResourceKind.Screenshots Then Item.Icon = InstanceResourceStore.LoadImage(Item.FullPath, 64)
                Result.Add(Item)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is Newtonsoft.Json.JsonException OrElse TypeOf ex Is ArgumentException
                '保留不属于当前目录或损坏的记录，不将其作为可删除项暴露。
            End Try
        Next
        Return Result.OrderByDescending(Function(Item) Item.CreatedUtc).ToList()
    End Function

    Friend Shared Sub Restore(Root As String, Recycle As String, Ids As IEnumerable(Of String), Token As CancellationToken)
        Dim Items = Ids.Distinct().Select(Function(Id) ReadRecord(Root, Recycle, Id)).ToList()
        For Each Item In Items
            If File.Exists(Item.Destination) OrElse Directory.Exists(Item.Destination) Then Throw New IOException("原位置已有同名资源，未覆盖。请先重命名或移走它，再还原。")
        Next
        For Each Item In Items
            Token.ThrowIfCancellationRequested()
            Dim Payload = Item.Entry.FullPath
            Directory.CreateDirectory(Path.GetDirectoryName(Item.Destination))
            If Item.Entry.IsDirectory Then Directory.Move(Payload, Item.Destination) Else File.Move(Payload, Item.Destination)
            File.Delete(Path.Combine(Item.Box, "resource.json"))
            Directory.Delete(Item.Box, False)
        Next
    End Sub

    Private Shared Sub ValidateTree(Recycle As String, Folder As String)
        InstanceResourceStore.EnsureWithin(Recycle, Folder)
        For Each Item In Directory.EnumerateFileSystemEntries(Folder)
            InstanceResourceStore.EnsureWithin(Recycle, Item)
            If Directory.Exists(Item) Then ValidateTree(Recycle, Item)
        Next
    End Sub

    Friend Shared Sub Purge(Root As String, Recycle As String, Ids As IEnumerable(Of String), Token As CancellationToken)
        Dim Items = Ids.Distinct().Select(Function(Id) ReadRecord(Root, Recycle, Id)).ToList()
        For Each Item In Items
            Token.ThrowIfCancellationRequested()
            ValidateTree(Recycle, Item.Box)
        Next
        For Each Item In Items
            Token.ThrowIfCancellationRequested()
            If Item.Entry.IsDirectory Then Directory.Delete(Item.Entry.FullPath, True) Else File.Delete(Item.Entry.FullPath)
            File.Delete(Path.Combine(Item.Box, "resource.json"))
            Directory.Delete(Item.Box, False)
        Next
    End Sub
End Class
