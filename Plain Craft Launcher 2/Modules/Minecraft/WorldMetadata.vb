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
        Dim Data = PCLCS.WorldMetadataFiles.Read(Folder, Details, Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName))
        If Data Is Nothing Then Return Nothing
        Return New WorldMetadata With {
            .Name = Data.Name, .LastPlayedUtc = Data.LastPlayedUtc, .VersionName = Data.VersionName,
            .DataVersion = Data.DataVersion, .GameMode = Data.GameMode, .Difficulty = Data.Difficulty,
            .Hardcore = Data.Hardcore, .AllowCommands = Data.AllowCommands, .WasModded = Data.WasModded,
            .Snapshot = Data.Snapshot, .Seed = Data.Seed,
            .SpawnX = Data.SpawnX, .SpawnY = Data.SpawnY, .SpawnZ = Data.SpawnZ,
            .DayTime = Data.DayTime, .Time = Data.Time}
    End Function
    Friend Shared Function Describe(Folder As String) As String
        Return PCLCS.WorldMetadataFiles.Describe(Folder,
            Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
            Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message))
    End Function
End Class
