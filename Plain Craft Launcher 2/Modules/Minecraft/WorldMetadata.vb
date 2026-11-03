Imports System.IO
Imports System.IO.Compression

Friend NotInheritable Class WorldMetadata
    Friend Name As String
    Friend LastPlayedUtc As Date
    Friend VersionName As String
    Friend DataVersion As Integer?
    Friend GameMode As Integer?
    Friend Difficulty As Integer?
    Friend Hardcore As Boolean?
    Friend AllowCommands As Boolean?
    Friend WasModded As Boolean?
    Friend Snapshot As Boolean?
    Friend Seed As Long?
    Friend SpawnX As Integer?, SpawnY As Integer?, SpawnZ As Integer?
    Friend DayTime As Long?
    Friend Time As Long?
    Friend Shared Function Read(Folder As String) As WorldMetadata
        Return ReadCore(Folder, False)
    End Function
    Friend Shared Function ReadDetails(Folder As String) As WorldMetadata
        Return ReadCore(Folder, True)
    End Function
    Private Shared Function ReadCore(Folder As String, Details As Boolean) As WorldMetadata
        Dim FileName = Path.Combine(Folder, "level.dat")
        If Not File.Exists(FileName) OrElse New FileInfo(FileName).Length > 8 * 1024 * 1024 Then Return Nothing
        InstanceResourceStore.EnsureWithin(Folder, FileName)
        Using File As New FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete), Uncompressed As New GZipStream(File, CompressionMode.Decompress)
            Dim Data = PCLCS.WorldMetadataReader.Read(Uncompressed, Details)
            If Data Is Nothing Then Return Nothing
            Return New WorldMetadata With {
                .Name = Data.Name, .LastPlayedUtc = Data.LastPlayedUtc, .VersionName = Data.VersionName,
                .DataVersion = Data.DataVersion, .GameMode = Data.GameMode, .Difficulty = Data.Difficulty,
                .Hardcore = Data.Hardcore, .AllowCommands = Data.AllowCommands, .WasModded = Data.WasModded,
                .Snapshot = Data.Snapshot, .Seed = Data.Seed,
                .SpawnX = Data.SpawnX, .SpawnY = Data.SpawnY, .SpawnZ = Data.SpawnZ,
                .DayTime = Data.DayTime, .Time = Data.Time}
        End Using
    End Function
    Friend Shared Function Describe(Folder As String) As String
        Dim Lines As New List(Of String) From {"存档文件夹：" & New DirectoryInfo(Folder).Name}
        Try
            Dim Data = ReadDetails(Folder)
            If Data Is Nothing Then
                Lines.Add("未找到可读取的 level.dat 元数据。")
            Else
                If Not String.IsNullOrWhiteSpace(Data.Name) Then Lines.Insert(0, "世界名称：" & Data.Name)
                If Not String.IsNullOrWhiteSpace(Data.VersionName) Then Lines.Add("游戏版本：" & Data.VersionName & If(Data.Snapshot.GetValueOrDefault(), "（快照）", ""))
                If Data.DataVersion.HasValue Then Lines.Add("数据版本：" & Data.DataVersion.Value)
                If Data.LastPlayedUtc <> Date.MinValue Then Lines.Add("最后游玩：" & Data.LastPlayedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))
                If Data.GameMode.HasValue Then
                    Dim Names = {"生存", "创造", "冒险", "旁观"}
                    Lines.Add("游戏模式：" & If(Data.GameMode.Value >= 0 AndAlso Data.GameMode.Value < Names.Length, Names(Data.GameMode.Value), "未知（" & Data.GameMode.Value & "）"))
                End If
                If Data.Difficulty.HasValue Then
                    Dim Names = {"和平", "简单", "普通", "困难"}
                    Lines.Add("难度：" & If(Data.Difficulty.Value >= 0 AndAlso Data.Difficulty.Value < Names.Length, Names(Data.Difficulty.Value), "未知（" & Data.Difficulty.Value & "）"))
                End If
                If Data.Hardcore.HasValue Then Lines.Add("极限模式：" & If(Data.Hardcore.Value, "开启", "关闭"))
                If Data.AllowCommands.HasValue Then Lines.Add("允许作弊：" & If(Data.AllowCommands.Value, "开启", "关闭"))
                If Data.WasModded.HasValue Then Lines.Add("模组环境记录：" & If(Data.WasModded.Value, "使用过", "未标记"))
                If Data.Seed.HasValue Then Lines.Add("世界种子：" & Data.Seed.Value)
                If Data.SpawnX.HasValue AndAlso Data.SpawnY.HasValue AndAlso Data.SpawnZ.HasValue Then Lines.Add($"世界出生点：{Data.SpawnX.Value}, {Data.SpawnY.Value}, {Data.SpawnZ.Value}")
                If Data.DayTime.HasValue AndAlso Data.DayTime.Value >= 0 Then Lines.Add("游戏日：第 " & (Data.DayTime.Value \ 24000) & " 天")
                If Data.Time.HasValue Then Lines.Add("世界累计刻数：" & Data.Time.Value)
            End If
        Catch ex As Exception
            Logger.Warn(ex, "读取本地存档详情失败")
            Lines.Add("无法读取 level.dat；文件可能正在写入、已损坏或格式不受支持。")
        End Try
        Lines.Add(vbCrLf & "位置：" & Path.GetFullPath(Folder))
        Return String.Join(vbCrLf, Lines)
    End Function
End Class
