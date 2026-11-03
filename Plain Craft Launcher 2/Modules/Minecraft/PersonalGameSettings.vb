Imports System.IO
Imports System.Text

Friend Class PersonalOptionsSession
    Friend Id As String = Guid.NewGuid().ToString("N")
    Friend VersionPath As String
    Friend GameFolder As String
    Friend Baseline As JObject
    Friend Keys As New HashSet(Of String)(StringComparer.Ordinal)
    Friend Tracked As Boolean
End Class

Friend Module PersonalGameSettings
    Private Const ProfileName As String = "personal-options.json"

    Friend Function Enabled(Instance As McInstance) As Boolean
        Return Instance IsNot Nothing AndAlso Settings.Get(Of Boolean)("LaunchPersonalOptionsSync") AndAlso Settings.Get(Of Integer)("VersionPersonalOptionsSync", Instance:=Instance) = 0
    End Function
    Private Function CanonicalKey(Key As String) As String
        Return PCLCS.PersonalOptionsRules.CanonicalKey(Key)
    End Function
    Private Function Selected(Key As String) As Boolean
        If Key.StartsWith("key_", StringComparison.Ordinal) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsKeys") AndAlso PCLCS.PersonalOptionsRules.VanillaBinding(Key)
        If PCLCS.PersonalOptionsRules.IsMouseKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsMouse")
        If PCLCS.PersonalOptionsRules.IsViewKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsView")
        If PCLCS.PersonalOptionsRules.IsAccessibilityKey(Key) Then Return Settings.Get(Of Boolean)("LaunchPersonalOptionsAccessibility")
        Return Key.StartsWith("soundCategory_", StringComparison.Ordinal) AndAlso Settings.Get(Of Boolean)("LaunchPersonalOptionsAudio")
    End Function
    Friend Function Parse(Text As String) As Dictionary(Of String, String)
        Return PCLCS.PersonalOptionsRules.Parse(Text)
    End Function
    Private Function ReadOptions(FileName As String) As Byte()
        If Not File.Exists(FileName) Then Return Nothing
        If New FileInfo(FileName).Length > 4 * 1024 * 1024 OrElse (File.GetAttributes(FileName) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("不读取过大或链接形式的 options.txt。")
        Return File.ReadAllBytes(FileName)
    End Function
    Private Function Decode(Bytes As Byte()) As String
        Return PCLCS.PersonalOptionsRules.Decode(Bytes)
    End Function
    Private Function Modern(Instance As McInstance) As Boolean
        Return Instance.Version.Valid AndAlso Instance.Version.Vanilla.Major >= 13
    End Function
    Private Function CanonicalValue(Key As String, Value As String) As String
        Return PCLCS.PersonalOptionsRules.CanonicalValue(Key, Value)
    End Function
    Private Function ForVersion(Key As String, Value As String, IsModern As Boolean) As String
        Return PCLCS.PersonalOptionsRules.ForVersion(Key, Value, IsModern)
    End Function
    Friend Function MergeText(Text As String, Values As JObject, IsModern As Boolean, Keys As ISet(Of String), Optional FovDegrees As Boolean = True) As String
        Return PCLCS.PersonalOptionsRules.MergeText(Text, Values, IsModern, Keys, AddressOf Selected, FovDegrees)
    End Function
    Friend Function HasProfile() As Boolean
        SyncLock LocalGameData.Gate
            Dim Values = TryCast(LocalGameData.ReadJson(ProfileName)("values"), JObject)
            Return Values IsNot Nothing AndAlso Values.Count > 0
        End SyncLock
    End Function
    Friend Sub ImportFrom(Instance As McInstance)
        If Instance Is Nothing Then Throw New IOException("请先选择一个已经运行过的游戏版本。")
        If HasRunningMinecraft Then Throw New IOException("请先关闭游戏，再导入个人设置。")
        Dim Bytes = ReadOptions(Path.Combine(Instance.PathIndie, "options.txt"))
        If Bytes Is Nothing Then Throw New IOException("该版本尚未生成 options.txt，请先启动一次游戏并保存设置。")
        Dim Values As New JObject
        For Each Pair In Parse(Decode(Bytes)).Where(Function(Item) Selected(Item.Key))
            Dim Value = CanonicalValue(Pair.Key, Pair.Value)
            If Value IsNot Nothing Then Values(Pair.Key) = Value
        Next
        If Values.Count = 0 Then Throw New IOException("该版本没有可导入的个人设置。")
        SyncLock LocalGameData.Gate
            LocalGameData.WriteJson(ProfileName, New JObject(New JProperty("version", 1), New JProperty("values", Values)))
        End SyncLock
    End Sub
    Friend Function Prepare(Instance As McInstance) As PersonalOptionsSession
        If Not Enabled(Instance) OrElse HasRunningMinecraft Then Return Nothing
        RecoverExitedSessions()
        Dim Folder = Instance.PathIndie, FileName = Path.Combine(Folder, "options.txt")
        If HasActiveSession(Folder) Then Return Nothing
        Dim Bytes = ReadOptions(FileName)
        '首次启动仍交给游戏/默认配置 Mod 生成文件，避免抢先创建文件阻止作者的默认配置生效。
        If Bytes Is Nothing Then Return Nothing
        SyncLock LocalGameData.Gate
            Dim Values = TryCast(LocalGameData.ReadJson(ProfileName)("values"), JObject)
            If Values Is Nothing OrElse Values.Count = 0 Then Return Nothing
            Dim Keys As New HashSet(Of String)(Values.Properties().Where(Function(Pair) Selected(Pair.Name)).Select(Function(Pair) Pair.Name), StringComparer.Ordinal)
            If Not Instance.Version.Valid Then Keys.RemoveWhere(Function(Key) Key.StartsWith("key_", StringComparison.Ordinal))
            Dim Text = Decode(Bytes), Merged = MergeText(Text, Values, Modern(Instance), Keys, Instance.Version.Valid AndAlso Instance.Version.Vanilla.Major >= 19)
            If Text <> Merged Then
                Dim Encoded = New UTF8Encoding(Bytes.Length >= 3 AndAlso Bytes(0) = &HEF AndAlso Bytes(1) = &HBB AndAlso Bytes(2) = &HBF)
                Dim Body = Encoded.GetPreamble().Concat(Encoded.GetBytes(Merged)).ToArray()
                LocalGameData.AtomicWrite(FileName, Body, LocalGameData.Hash(Bytes), True)
                IniClearCache(FileName)
            End If
            Dim Baseline As New JObject
            For Each Pair In Parse(Merged).Where(Function(Item) Keys.Contains(Item.Key))
                Dim Value = CanonicalValue(Pair.Key, Pair.Value)
                If Value IsNot Nothing Then Baseline(Pair.Key) = Value
            Next
            Return New PersonalOptionsSession With {.VersionPath = Instance.PathVersion, .GameFolder = Folder, .Baseline = Baseline, .Keys = Keys}
        End SyncLock
    End Function
    Friend Sub Track(Session As PersonalOptionsSession, Game As Process)
        If Session Is Nothing Then Return
        SyncLock LocalGameData.Gate
            LocalGameData.WriteJson("options-sessions/" & Session.Id & ".json", New JObject(New JProperty("versionPath", Session.VersionPath), New JProperty("gameFolder", Session.GameFolder), New JProperty("pid", Game.Id), New JProperty("started", Game.StartTime.ToUniversalTime().Ticks), New JProperty("baseline", Session.Baseline), New JProperty("keys", New JArray(Session.Keys))))
            Session.Tracked = True
        End SyncLock
    End Sub
    Friend Function NormalizeBinding(Value As String) As String
        Return PCLCS.PersonalOptionsRules.NormalizeBinding(Value)
    End Function
    Friend Function BindingForVersion(Value As String, IsModern As Boolean) As String
        Return PCLCS.PersonalOptionsRules.BindingForVersion(Value, IsModern)
    End Function
    Friend Function VanillaBinding(Key As String) As Boolean
        Return PCLCS.PersonalOptionsRules.VanillaBinding(Key)
    End Function
    Friend Sub CaptureEditedBinding(Instance As McInstance, Key As String, Value As String)
        If Not Enabled(Instance) OrElse Not Selected(Key) Then Return
        SyncLock LocalGameData.Gate
            Dim Profile = LocalGameData.ReadJson(ProfileName), Values = TryCast(Profile("values"), JObject)
            If Values Is Nothing Then Return
            Dim Canonical = CanonicalValue(Key, Value)
            If Canonical Is Nothing Then Return
            Values(CanonicalKey(Key)) = Canonical
            LocalGameData.WriteJson(ProfileName, Profile)
        End SyncLock
    End Sub
    Friend Sub Finish(Session As PersonalOptionsSession)
        If Session Is Nothing Then Return
        SyncLock LocalGameData.Gate
            Dim Journal = LocalGameData.Address("options-sessions/" & Session.Id & ".json")
            If Session.Tracked AndAlso Not File.Exists(Journal) Then Return
            Capture(Session)
            If File.Exists(Journal) Then File.Delete(Journal)
        End SyncLock
    End Sub
    Private Sub Capture(Session As PersonalOptionsSession)
        Dim Instance As New McInstance(Session.VersionPath)
        If Not Enabled(Instance) Then Return
        Dim Bytes = ReadOptions(Path.Combine(Session.GameFolder, "options.txt"))
        If Bytes Is Nothing Then Return
        Dim Current = Parse(Decode(Bytes))
        SyncLock LocalGameData.Gate
            Dim Profile = LocalGameData.ReadJson(ProfileName), Values = TryCast(Profile("values"), JObject)
            If Values Is Nothing Then Return
            For Each Key In Session.Keys.Where(Function(Value) Selected(Value))
                Dim Raw As String = Nothing
                If Not Current.TryGetValue(Key, Raw) Then Continue For
                Dim Value = CanonicalValue(Key, Raw)
                If Value IsNot Nothing AndAlso Session.Baseline(Key) IsNot Nothing AndAlso Value <> Session.Baseline(Key).ToString() Then Values(Key) = Value
            Next
            LocalGameData.WriteJson(ProfileName, Profile)
        End SyncLock
    End Sub
    Friend Sub RecoverExitedSessions()
        Dim Folder = LocalGameData.Address("options-sessions")
        If Not Directory.Exists(Folder) Then Return
        SyncLock LocalGameData.Gate
        For Each FileName In Directory.EnumerateFiles(Folder, "*.json")
            Try
                Dim Json = LocalGameData.ReadJson("options-sessions/" & Path.GetFileName(FileName))
                If LocalGameData.ProcessMatches(Json("pid").ToObject(Of Integer)(), Json("started").ToObject(Of Long)()) Then Continue For
                Dim Session As New PersonalOptionsSession With {.Id = Path.GetFileNameWithoutExtension(FileName), .VersionPath = Json("versionPath").ToString(), .GameFolder = Json("gameFolder").ToString(), .Baseline = DirectCast(Json("baseline"), JObject), .Keys = New HashSet(Of String)(Json("keys").Select(Function(Item) Item.ToString()), StringComparer.Ordinal), .Tracked = True}
                Finish(Session)
            Catch ex As Exception
                Logger.Warn(ex, "恢复个人游戏设置同步会话失败")
            End Try
        Next
        End SyncLock
    End Sub
    Private Function HasActiveSession(GameFolder As String) As Boolean
        Dim Folder = LocalGameData.Address("options-sessions")
        If Not Directory.Exists(Folder) Then Return False
        SyncLock LocalGameData.Gate
            For Each FileName In Directory.EnumerateFiles(Folder, "*.json")
                Dim Json = LocalGameData.ReadJson("options-sessions/" & Path.GetFileName(FileName))
                If PathUtils.IsSame(Json("gameFolder").ToString(), GameFolder) AndAlso LocalGameData.ProcessMatches(Json("pid").ToObject(Of Integer)(), Json("started").ToObject(Of Long)()) Then Return True
            Next
        End SyncLock
        Return False
    End Function
    Private Function CreateLegacyKeys() As Dictionary(Of Integer, String)
        Return PCLCS.PersonalOptionsRules.CreateLegacyKeys()
    End Function
End Module
