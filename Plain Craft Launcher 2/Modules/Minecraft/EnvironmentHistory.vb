Imports System.IO
Imports System.Text.RegularExpressions

'内容寻址存储使多次还原点共用未变化的文件；还原点仅保存在本机，不随整合包导出。
Friend NotInheritable Class EnvironmentHistory
    Private Shared ReadOnly Gate As New Object
    Private Shared Restoring As Integer
    Friend Shared Property TestRoot As String
    Private Shared ReadOnly AutomaticLabels As String() = {"游戏启动前", "修改游戏组件前", "更新模组前", "整合包更新前", "恢复前自动保存", "测试副本迁移前"}
    Private Const CurrentCoverageVersion As Integer = 2
    Private Shared ReadOnly LegacyGameFolders As String() = {"mods", "config", "defaultconfigs", "kubejs", "scripts", "resources"}
    Friend Shared ReadOnly GameEnvironmentFolders As String() = {"mods", "coremods", "config", "defaultconfigs", "configureddefaults", "kubejs", "scripts", "resources", "global_packs", "openloader"}
    Friend Shared Function CoverageVersion(Point As JObject) As Integer
        Dim Value = Point("coverageVersion")
        If Value Is Nothing Then Return 1
        If Value.Type <> JTokenType.Integer Then Throw New IOException("还原点的覆盖范围版本无效。")
        Dim Version = Value.ToObject(Of Integer)()
        If Version < 1 OrElse Version > CurrentCoverageVersion Then Throw New IOException("此还原点使用了不支持的覆盖范围，请使用对应版本的启动器。")
        Return Version
    End Function
    Friend Shared Function FoldersForCoverage(Version As Integer) As IEnumerable(Of String)
        Select Case Version
            Case 1 : Return LegacyGameFolders
            Case 2 : Return GameEnvironmentFolders
            Case Else : Throw New IOException("不支持此还原点的覆盖范围。")
        End Select
    End Function
    Private Shared Function Store(VersionFolder As String) As String
        Dim Key = LocalGameData.Hash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(VersionFolder).TrimEnd("\"c).ToLowerInvariant())).Replace("/", "_").Replace("+", "-").TrimEnd("="c)
        Return Path.Combine(If(TestRoot, Path.Combine(LocalGameData.Root, "environment-history")), Key)
    End Function

    '共享对象的回收与创建、恢复使用同一把跨进程锁。
    Private NotInheritable Class StoreLease
        Implements IDisposable
        Private ReadOnly Lock As Threading.Mutex
        Friend Sub New(VersionFolder As String)
            Dim Root = Store(VersionFolder)
            InstanceCloner.EnsureNoLinks(Root)
            Dim Key = LocalGameData.Hash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(Root).ToLowerInvariant())).Replace("/", "_").Replace("+", "-").TrimEnd("="c)
            Lock = New Threading.Mutex(False, "Local\PCLLF.EnvironmentHistory." & Key)
            Try
                If Not Lock.WaitOne(TimeSpan.FromSeconds(30)) Then Throw New IOException("其他启动器正在处理此版本的还原点，请稍后重试。")
            Catch ex As Threading.AbandonedMutexException
                '上次进程已退出，本线程已取得锁；清理仍按完整清单计算引用。
            Catch
                Lock.Dispose()
                Throw
            End Try
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            Lock.ReleaseMutex()
            Lock.Dispose()
        End Sub
    End Class
    Private Shared Function Safe(StoreRoot As String, Relative As String) As String
        Return ModpackUpdate.Target(StoreRoot, Relative)
    End Function
    Private Shared Function ObjectPath(StoreRoot As String, Hash As String) As String
        If Not Regex.IsMatch(If(Hash, ""), "\Asha1:[a-f0-9]{40}\z") Then Throw New IOException("还原点文件校验值无效。")
        Return Safe(StoreRoot, "objects/" & Hash.Substring(5))
    End Function
    Friend Shared Function IsGameFile(Relative As String) As Boolean
        Return IsGameFileCovered(Relative, CurrentCoverageVersion)
    End Function
    Friend Shared Function IsGameFileCovered(Relative As String, Version As Integer) As Boolean
        Dim Lower = Relative.Replace("\", "/").ToLowerInvariant(), Top = Lower.Split("/"c)(0)
        If Lower.Contains("waypoint") OrElse Lower.StartsWith("config/xaero") OrElse Lower.StartsWith("config/journeymap") OrElse Lower.StartsWith("config/voxelmap") Then Return False
        If FoldersForCoverage(Version).Contains(Top) Then Return True
        If Lower.Contains("/") Then Return False
        Return Lower = "options.txt" OrElse Lower.StartsWith("optionsof") OrElse Lower.StartsWith("optionsshaders") OrElse
            {".cfg", ".toml", ".properties"}.Contains(Path.GetExtension(Lower))
    End Function
    Private Shared Sub Walk(Root As String, Folder As String, Result As Dictionary(Of String, String))
        InstanceResourceStore.EnsureWithin(Root, Folder)
        If Not Directory.Exists(Folder) Then Return
        For Each FileName In Directory.GetFiles(Folder)
            InstanceResourceStore.EnsureWithin(Root, FileName)
            Dim Relative = FileName.Substring(PathUtils.AddSlashSuffix(Root).Length).Replace("\", "/")
            If IsGameFile(Relative) Then Result("g/" & Relative) = FileName
        Next
        For Each Child In Directory.GetDirectories(Folder)
            Walk(Root, Child, Result)
        Next
    End Sub
    Friend Shared Function Files(VersionFolder As String, GameRoot As String) As Dictionary(Of String, String)
        Return FilesWithCoverage(VersionFolder, GameRoot, CurrentCoverageVersion)
    End Function
    Private Shared Function FilesWithCoverage(VersionFolder As String, GameRoot As String, Version As Integer) As Dictionary(Of String, String)
        Dim Result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each Name In FoldersForCoverage(Version)
            Walk(GameRoot, Path.Combine(GameRoot, Name), Result)
        Next
        If Directory.Exists(GameRoot) Then
            For Each FileName In Directory.GetFiles(GameRoot)
                If IsGameFile(Path.GetFileName(FileName)) Then Result("g/" & Path.GetFileName(FileName)) = ModpackUpdate.Target(GameRoot, Path.GetFileName(FileName))
            Next
        End If
        If Directory.Exists(VersionFolder) Then
            For Each FileName In Directory.GetFiles(VersionFolder)
                If {".json", ".jar"}.Contains(Path.GetExtension(FileName).ToLowerInvariant()) Then Result("v/" & Path.GetFileName(FileName)) = ModpackUpdate.Target(VersionFolder, Path.GetFileName(FileName))
            Next
        End If
        For Each Name In {"PCL/Setup.ini", "PCL/config.json", "PCL/ModpackOrigin.json"}
            Dim FileName = ModpackUpdate.Target(VersionFolder, Name)
            If File.Exists(FileName) Then Result("v/" & Name) = FileName
        Next
        Return Result
    End Function
    Friend Shared Function Capture(VersionFolder As String, GameRoot As String, Label As String, Token As CancellationToken,
        Optional Dependencies As Dictionary(Of String, String) = Nothing, Optional Description As String = "") As String
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                Dim StoreRoot = Store(VersionFolder), Id = Guid.NewGuid().ToString("N"), Entries = Files(VersionFolder, GameRoot), Content As New JObject
                If Dependencies IsNot Nothing Then
                    For Each Pair In Dependencies
                        Entries.Add("m/" & ModpackUpdate.RelativePath(Pair.Key), Pair.Value)
                    Next
                End If
                For Each Pair In Entries
                    Token.ThrowIfCancellationRequested()
                    Dim Hash = ModpackUpdate.Digest(Pair.Value)
                    If Hash Is Nothing Then Throw New IOException("创建还原点期间文件被移走：" & Pair.Key)
                    Dim Blob = ObjectPath(StoreRoot, Hash)
                    If Not File.Exists(Blob) Then
                        Directory.CreateDirectory(Path.GetDirectoryName(Blob))
                        Dim Temporary = Blob & ".tmp-" & Guid.NewGuid().ToString("N")
                        Try
                            File.Copy(Pair.Value, Temporary, False)
                            If ModpackUpdate.Digest(Temporary) <> Hash Then Throw New IOException("创建还原点期间文件发生变化。")
                            File.Move(Temporary, Blob)
                        Finally
                            If File.Exists(Temporary) Then File.Delete(Temporary)
                        End Try
                    ElseIf ModpackUpdate.Digest(Blob) <> Hash Then
                        Throw New IOException("已有还原点内容校验失败，请保留备份并检查磁盘。")
                    End If
                    If ModpackUpdate.Digest(Pair.Value) <> Hash Then Throw New IOException("创建还原点期间文件发生变化。")
                    Content(Pair.Key) = Hash
                Next
                Dim Latest = Files(VersionFolder, GameRoot)
                If Not New HashSet(Of String)(Entries.Keys.Where(Function(Key) Not Key.StartsWith("m/")), StringComparer.OrdinalIgnoreCase).SetEquals(Latest.Keys) Then Throw New IOException("创建还原点期间目录内容发生变化。")
                For Each Pair In Latest
                    If ModpackUpdate.Digest(Pair.Value) <> Content(Pair.Key).ToString() Then Throw New IOException("创建还原点期间配置发生变化。")
                Next
                Dim Data As New JObject(New JProperty("schema", 1), New JProperty("coverageVersion", CurrentCoverageVersion), New JProperty("id", Id), New JProperty("created", Date.UtcNow), New JProperty("label", Label),
                    New JProperty("description", Description), New JProperty("gameRoot", Path.GetFullPath(GameRoot)), New JProperty("healthy", False), New JProperty("manual", Not AutomaticLabels.Contains(Label)), New JProperty("files", Content))
                LocalGameData.AtomicWrite(Safe(StoreRoot, "points/" & Id & ".json"), System.Text.Encoding.UTF8.GetBytes(Data.ToString()), Nothing, False)
                Return Id
            End Using
        End SyncLock
    End Function
    Friend Shared Function CaptureInstance(Instance As McInstance, Label As String, Token As CancellationToken) As String
        Dim McRoot = Path.GetDirectoryName(Path.GetDirectoryName(Instance.PathVersion.TrimEnd("\"c))) & "\"
        Dim Dependencies As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each Library In McLibListGet(Instance, True)
            If PathUtils.IsParentOf(Instance.PathVersion, Library.LocalPath) Then Continue For
            InstanceResourceStore.EnsureWithin(McRoot, Library.LocalPath)
            Dim Relative = PathUtils.RemoveExtendedPrefix(Path.GetFullPath(Library.LocalPath)).Substring(McRoot.Length).Replace("\", "/")
            If Not Relative.StartsWith("libraries/", StringComparison.OrdinalIgnoreCase) AndAlso Not Relative.StartsWith("versions/", StringComparison.OrdinalIgnoreCase) Then Throw New IOException("游戏依赖不在标准目录中，无法完整记录环境。")
            Dependencies(Relative) = Library.LocalPath
        Next
        Dim Json = Instance.JsonObject, Seen As New HashSet(Of String)
        While Json("inheritsFrom") IsNot Nothing
            Dim Name = Json("inheritsFrom").ToString()
            InstanceResourceStore.SafeName(Name)
            If Not Seen.Add(Name) OrElse Seen.Count > 32 Then Throw New IOException("版本继承关系无效。")
            Dim Relative = "versions/" & Name & "/" & Name & ".json", Address = ModpackUpdate.Target(McRoot, Relative)
            Dependencies(Relative) = Address
            Json = JObject.Parse(File.ReadAllText(Address))
        End While
        Dim Id = Capture(Instance.PathVersion, Instance.PathIndie, Label, Token, Dependencies, Instance.Version.VanillaName & " · " & String.Join("、", Instance.Version.ModLoaderVersions.Select(Function(Pair) Pair.Key.ToString() & " " & Pair.Value)))
        ApplyRetention(Instance, Id, Token)
        Return Id
    End Function
    Friend Shared Function Points(VersionFolder As String) As List(Of JObject)
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                Dim StoreRoot = Store(VersionFolder), Folder = Path.Combine(StoreRoot, "points"), Result As New List(Of JObject)
                InstanceResourceStore.EnsureWithin(StoreRoot, Folder)
                If Not Directory.Exists(Folder) Then Return Result
                For Each FileName In Directory.GetFiles(Folder, "*.json")
                    InstanceResourceStore.EnsureWithin(StoreRoot, FileName)
                    If New FileInfo(FileName).Length > 32 * 1024 * 1024 Then Throw New IOException("还原点清单过大。")
                    Dim Value = JObject.Parse(File.ReadAllText(FileName))
                    If CInt(Value("schema")) <> 1 Then Throw New IOException("不支持此版本的环境还原点。")
                    Result.Add(Value)
                Next
                Return Result.OrderByDescending(Function(Item) CDate(Item("created"))).ToList()
            End Using
        End SyncLock
    End Function

    Friend Shared Function IsManualPoint(Point As JObject) As Boolean
        Dim Value = Point("manual")
        If Value IsNot Nothing Then Return Value.Type <> JTokenType.Boolean OrElse Value.ToObject(Of Boolean)()
        Return Not AutomaticLabels.Contains(Point("label")?.ToString())
    End Function

    Friend Shared Sub ApplyRetention(Instance As McInstance, NewestId As String, Token As CancellationToken)
        If Not Settings.Get(Of Boolean)("VersionEnvironmentHistoryAutoCleanup", Instance:=Instance) Then Return
        Try
            Dim Removed = PruneAutomatic(Instance.PathVersion, Settings.Get(Of Integer)("VersionEnvironmentHistoryRetention", Instance:=Instance), NewestId, Token)
            If Removed > 0 Then Logger.Info($"已自动清理 {Removed} 个旧环境还原点，手动还原点已保留。")
        Catch ex As OperationCanceledException
            Throw
        Catch ex As Exception
            Logger.Warn(ex, "新还原点已保存，但自动清理旧还原点失败")
        End Try
    End Sub

    Private Shared Sub AddReferences(Point As JObject, References As HashSet(Of String))
        If CInt(Point("schema")) <> 1 OrElse Not Regex.IsMatch(If(Point("id")?.ToString(), ""), "\A[a-f0-9]{32}\z") Then Throw New IOException("还原点清单无效，未清理旧还原点。")
        CoverageVersion(Point)
        Dim Content = TryCast(Point("files"), JObject)
        If Content Is Nothing Then Throw New IOException("还原点文件清单缺失，未清理旧还原点。")
        For Each Entry In Content.Properties()
            Dim Hash = Entry.Value.ToString()
            If Not Regex.IsMatch(Hash, "\Asha1:[a-f0-9]{40}\z") Then Throw New IOException("还原点引用无效，未清理旧还原点。")
            References.Add(Hash.Substring(5))
        Next
    End Sub

    Friend Shared Function PruneAutomatic(VersionFolder As String, Count As Integer, NewestId As String, Token As CancellationToken) As Integer
        If Count < 1 OrElse Count > 100 Then Throw New IOException("自动还原点保留数量必须为 1 到 100。")
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                '恢复时仍需使用所选旧环境的共享对象；恢复成功后再处理保留上限。
                If Restoring > 0 Then Return 0
                Token.ThrowIfCancellationRequested()
                Dim StoreRoot = Store(VersionFolder), All = Points(VersionFolder)
                Dim References As New HashSet(Of String)(StringComparer.Ordinal), Seen As New HashSet(Of String)(StringComparer.Ordinal)
                For Each Point In All
                    AddReferences(Point, References)
                    Dim Id = Point("id").ToString(), Address = Safe(StoreRoot, "points/" & Id & ".json")
                    If Not Seen.Add(Id) OrElse Not File.Exists(Address) OrElse Not JToken.DeepEquals(JObject.Parse(File.ReadAllText(Address)), Point) Then Throw New IOException("还原点列表已变化，未清理旧还原点。")
                Next
                Dim Automatic = All.Where(Function(Point) Not IsManualPoint(Point)).ToList()
                If Automatic.Count <= Count Then Return 0
                Dim Keep = Automatic.Where(Function(Point) Point("id").ToString() = NewestId).Concat(Automatic.Where(Function(Point) Point("id").ToString() <> NewestId)).Take(Count).Select(Function(Point) Point("id").ToString()).ToHashSet()
                Dim Expired = Automatic.Where(Function(Point) Not Keep.Contains(Point("id").ToString())).ToList()
                References.Clear()
                For Each Point In All.Where(Function(Item) IsManualPoint(Item) OrElse Keep.Contains(Item("id").ToString()))
                    AddReferences(Point, References)
                Next
                '手动删除的记录仍可从 deleted 目录恢复，继续保留它们引用的对象。
                Dim Deleted = Path.Combine(StoreRoot, "deleted")
                InstanceCloner.EnsureNoLinks(Deleted)
                If Directory.Exists(Deleted) Then
                    For Each Batch In Directory.GetDirectories(Deleted)
                        InstanceCloner.EnsureNoLinks(Batch)
                        If Directory.GetDirectories(Batch).Any() Then Throw New IOException("删除记录的目录结构未知，未清理旧还原点。")
                        For Each Address In Directory.GetFiles(Batch, "*.json")
                            InstanceCloner.EnsureNoLinks(Address)
                            If New FileInfo(Address).Length > 32 * 1024 * 1024 Then Throw New IOException("删除记录过大，未清理旧还原点。")
                            AddReferences(JObject.Parse(File.ReadAllText(Address)), References)
                        Next
                    Next
                End If
                Dim Objects = Path.Combine(StoreRoot, "objects"), Unused As New List(Of String)
                InstanceCloner.EnsureNoLinks(Objects)
                If Directory.Exists(Objects) Then
                    For Each Address In Directory.GetFiles(Objects)
                        InstanceCloner.EnsureNoLinks(Address)
                        Dim Hash = Path.GetFileName(Address)
                        If Regex.IsMatch(Hash, "\A[a-f0-9]{40}\z") AndAlso Not References.Contains(Hash) Then Unused.Add(Address)
                    Next
                End If
                Token.ThrowIfCancellationRequested()
                For Each Point In Expired
                    Token.ThrowIfCancellationRequested()
                    File.Delete(Safe(StoreRoot, "points/" & Point("id").ToString() & ".json"))
                Next
                '只有全部过期清单移除后，才回收已不被其他记录引用的对象。
                For Each Address In Unused
                    Token.ThrowIfCancellationRequested()
                    InstanceCloner.EnsureNoLinks(Address)
                    File.Delete(Address)
                Next
                Return Expired.Count
            End Using
        End SyncLock
    End Function
    Friend Shared Sub MarkHealthy(VersionFolder As String, Id As String, Healthy As Boolean)
        If String.IsNullOrEmpty(Id) OrElse Not Healthy Then Return
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                If Not Regex.IsMatch(Id, "\A[a-f0-9]{32}\z") Then Throw New IOException("还原点编号无效。")
                Dim Address = Safe(Store(VersionFolder), "points/" & Id & ".json")
                If Not File.Exists(Address) Then Return
                Dim Value = JObject.Parse(File.ReadAllText(Address))
                Value("healthy") = True
                LocalGameData.AtomicWrite(Address, System.Text.Encoding.UTF8.GetBytes(Value.ToString()), Nothing, False)
            End Using
        End SyncLock
    End Sub
    Friend Shared Function Restore(VersionFolder As String, GameRoot As String, McRoot As String, Id As String, Token As CancellationToken,
        Optional BeforeApply As Action(Of Integer) = Nothing, Optional CaptureBefore As Func(Of String) = Nothing) As String
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                Restoring += 1
                Try
                    Dim Point = Points(VersionFolder).FirstOrDefault(Function(Item) Item("id").ToString() = Id)
                    If Point Is Nothing Then Throw New IOException("找不到该环境还原点。")
                    If Not PathUtils.IsSame(Point("gameRoot").ToString(), GameRoot) Then Throw New IOException("此还原点的游戏目录与当前版本隔离设置不同，请先恢复原隔离设置。")
                    Dim Coverage = CoverageVersion(Point)
                    Dim StoreRoot = Store(VersionFolder), Current = FilesWithCoverage(VersionFolder, GameRoot, Coverage), Sources As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                    For Each Entry As JProperty In CType(Point("files"), JObject).Properties()
                        Token.ThrowIfCancellationRequested()
                        Dim Key = Entry.Name, Hash = Entry.Value.ToString(), Blob = ObjectPath(StoreRoot, Hash)
                        If ModpackUpdate.Digest(Blob) <> Hash Then Throw New IOException("还原点文件缺失或损坏，当前环境未修改。")
                        If Key.StartsWith("m/") Then
                            Dim Relative = ModpackUpdate.RelativePath(Key.Substring(2))
                            If Not Relative.StartsWith("libraries/", StringComparison.OrdinalIgnoreCase) AndAlso Not Relative.StartsWith("versions/", StringComparison.OrdinalIgnoreCase) Then Throw New IOException("无效的依赖文件路径。")
                            Dim Destination = ModpackUpdate.Target(McRoot, Relative)
                            If File.Exists(Destination) AndAlso ModpackUpdate.Digest(Destination) <> Hash Then Throw New IOException("共享支持库已被修改，已停止恢复：" & Relative)
                            Sources(Key) = Blob
                        ElseIf Key.StartsWith("g/") AndAlso IsGameFileCovered(Key.Substring(2), Coverage) Then
                            ModpackUpdate.Target(GameRoot, Key.Substring(2))
                            Sources(Key) = Blob
                        ElseIf Key.StartsWith("v/") AndAlso (Not Key.Substring(2).Contains("/") AndAlso {".json", ".jar"}.Contains(Path.GetExtension(Key).ToLowerInvariant()) OrElse {"v/PCL/Setup.ini", "v/PCL/config.json", "v/PCL/ModpackOrigin.json"}.Contains(Key)) Then
                            ModpackUpdate.Target(VersionFolder, Key.Substring(2))
                            Sources(Key) = Blob
                        Else
                            Throw New IOException("还原点包含不支持的文件范围。")
                        End If
                    Next
                    Dim Operations As New List(Of Tuple(Of String, String, String))
                    For Each Key In Current.Keys.Union(Sources.Keys.Where(Function(Value) Not Value.StartsWith("m/")), StringComparer.OrdinalIgnoreCase)
                        Dim Destination = ModpackUpdate.Target(If(Key.StartsWith("v/"), VersionFolder, GameRoot), Key.Substring(2)), Payload As String = Nothing
                        Sources.TryGetValue(Key, Payload)
                        Dim Before = ModpackUpdate.Digest(Destination)
                        If Payload IsNot Nothing AndAlso Before = ModpackUpdate.Digest(Payload) Then Continue For
                        Operations.Add(Tuple.Create(Destination, Payload, Before))
                    Next
                    '恢复前再留一份完整环境，反向恢复不依赖单文件备份。
                    Dim UndoId = If(CaptureBefore Is Nothing, Capture(VersionFolder, GameRoot, "恢复前自动保存", Token), CaptureBefore())
                    For Each Pair In Sources.Where(Function(Item) Item.Key.StartsWith("m/"))
                        Dim Destination = ModpackUpdate.Target(McRoot, Pair.Key.Substring(2))
                        If Not File.Exists(Destination) Then
                            Directory.CreateDirectory(Path.GetDirectoryName(Destination))
                            File.Copy(Pair.Value, Destination, False)
                        End If
                    Next
                    ModpackUpdate.ApplyTransaction(GameRoot, VersionFolder, Operations, Token, BeforeApply)
                    Return UndoId
                Finally
                    Restoring -= 1
                End Try
            End Using
        End SyncLock
    End Function
    Friend Shared Sub OpenFolder(VersionFolder As String)
        Dim Folder = Store(VersionFolder)
        If Directory.Exists(Folder) Then OpenExplorer(Folder) Else Hint("此版本还没有环境还原点。")
    End Sub
    Friend Shared Function DeletePoints(VersionFolder As String, Ids As IEnumerable(Of String)) As String
        SyncLock Gate
            Using Lease As New StoreLease(VersionFolder)
                Dim Selected = Ids.Distinct(StringComparer.Ordinal).ToList(), StoreRoot = Store(VersionFolder)
                If Selected.Count = 0 Then Return Nothing
                Dim Moves As New List(Of Tuple(Of String, String))
                Dim Deleted = "deleted/" & Guid.NewGuid().ToString("N") & "/"
                For Each Id In Selected
                    If Not Regex.IsMatch(Id, "\A[a-f0-9]{32}\z") Then Throw New IOException("还原点编号无效。")
                    Dim Source = Safe(StoreRoot, "points/" & Id & ".json"), Destination = Safe(StoreRoot, Deleted & Id & ".json")
                    If Not File.Exists(Source) Then Throw New IOException("还原点已变化，请刷新列表。")
                    Moves.Add(Tuple.Create(Source, Destination))
                Next
                Dim Moved As New List(Of Tuple(Of String, String))
                Try
                    For Each Pair In Moves
                        Directory.CreateDirectory(Path.GetDirectoryName(Pair.Item2))
                        File.Move(Pair.Item1, Pair.Item2)
                        Moved.Add(Pair)
                    Next
                Catch
                    For Each Pair In Moved.AsEnumerable().Reverse()
                        File.Move(Pair.Item2, Pair.Item1)
                    Next
                    Throw
                End Try
                '共享对象不回收：其他还原点与并行启动任务仍可能引用；删除不触及游戏目录。
                Return Path.GetDirectoryName(Moves(0).Item2)
            End Using
        End SyncLock
    End Function
End Class

Friend NotInheritable Class EnvironmentHistoryUi
    Friend Shared Event Changed()
    Private Shared Busy As Integer
    Friend Shared ReadOnly Property IsBusy As Boolean
        Get
            Return Threading.Volatile.Read(Busy) <> 0
        End Get
    End Property
    Friend Shared Sub Start(Instance As McInstance, Restore As Boolean, Optional PointId As String = Nothing, Optional DeleteIds As IEnumerable(Of String) = Nothing)
        If Instance Is Nothing OrElse HasRunningMinecraft OrElse QuickPlayWorlds.HasRunningSessions() OrElse McLaunchLoader.State = LoadState.Loading OrElse ModpackUpdateFlow.IsUpdating OrElse McInstanceLock.IsAnyLocked OrElse PageInstanceMod.UpdatingInstanceModFolders.Any() Then
            Hint("请先关闭游戏并等待版本任务完成。", HintType.Red)
            Return
        End If
        If Interlocked.CompareExchange(Busy, 1, 0) <> 0 Then Return
        Dim Removing = DeleteIds IsNot Nothing, Ids = DeleteIds?.ToList()
        Dim StepLoader As New LoaderTask(Of String, String)(If(Removing, "删除还原点", If(Restore, "恢复游戏环境", "创建环境还原点")),
            Sub(Task)
                Dim Token = Task.CreateCancellationToken()
                If Removing Then
                    If MyMsgBox($"删除选中的 {Ids.Count} 个还原点？{vbCrLf}不会删除游戏文件或其他还原点。删除记录将保留在还原点文件夹的 deleted 目录中。", "删除还原点", "删除", "取消", IsWarn:=True) <> 1 Then Throw New OperationCanceledException()
                    Token.ThrowIfCancellationRequested()
                    Task.Output = EnvironmentHistory.DeletePoints(Instance.PathVersion, Ids)
                    Return
                End If
                If ModpackUpdate.Pending(Instance.PathVersion).Any() Then
                    If MyMsgBox("上次环境修改被中断，需要先恢复到修改前。", "恢复未完成的操作", "恢复", "取消", IsWarn:=True) <> 1 Then Throw New OperationCanceledException()
                    Instance = ModpackUpdateFlow.RecoverInstance(Instance)
                End If
                If Not Restore Then
                    Task.Output = EnvironmentHistory.CaptureInstance(Instance, "手动还原点", Token)
                    Return
                End If
                Dim Points = EnvironmentHistory.Points(Instance.PathVersion).OrderByDescending(Function(Item) CBool(Item("healthy"))).ThenByDescending(Function(Item) CDate(Item("created"))).ToList()
                If Not Points.Any() Then Throw New IOException("尚无环境还原点。可先创建还原点，后续更新前也会自动保存。")
                Dim Choice As Integer? = Nothing
                If PointId IsNot Nothing Then
                    Dim Index = Points.FindIndex(Function(Item) Item("id").ToString() = PointId)
                    If Index < 0 Then Throw New IOException("该还原点已不存在，请刷新列表。")
                    Choice = Index
                Else
                    RunInUiWait(Sub() Choice = MyMsgBoxSelect(Points.Select(Function(Item) CType(New MyRadioBox With {
                    .Text = CDate(Item("created")).ToLocalTime().ToString("yyyy-MM-dd HH:mm") & " · " & Item("label").ToString() & If(CBool(Item("healthy")), " · 正常运行", ""),
                    .ToolTip = Item("description")?.ToString()}, IMyRadio)), "选择环境还原点", "下一步", "取消"))
                End If
                If Not Choice.HasValue Then Throw New OperationCanceledException()
                Dim Point = Points(Choice.Value)
                If MyMsgBox("将整体恢复该时间点的 Minecraft、加载器、模组和配置，并移除之后新增的环境文件。" & vbCrLf &
                    "存档、截图和地图标记保持当前内容；恢复旧游戏版本后，请确认它仍支持你的存档。" & vbCrLf & vbCrLf &
                    If(PathUtils.IsSame(Instance.PathVersion, Instance.PathIndie), "", "此版本未使用独立目录，恢复模组和配置也会影响共用目录的其他版本。" & vbCrLf & vbCrLf) &
                    "恢复前会自动保存当前环境。" & vbCrLf & Point("description")?.ToString(), "恢复游戏环境", "恢复", "取消", IsWarn:=True) <> 1 Then Throw New OperationCanceledException()
                Dim McRoot = Path.GetDirectoryName(Path.GetDirectoryName(Instance.PathVersion.TrimEnd("\"c))) & "\"
                Task.Output = EnvironmentHistory.Restore(Instance.PathVersion, Instance.PathIndie, McRoot, Point("id").ToString(), Token,
                    CaptureBefore:=Function() EnvironmentHistory.CaptureInstance(Instance, "恢复前自动保存", Token))
                EnvironmentHistory.ApplyRetention(Instance, Task.Output, Token)
                RunInUiWait(Sub()
                                Instance.ResetSettingsCache()
                                Dim Refreshed = New McInstance(Instance.PathVersion).Load()
                                IniClearCache(Instance.PathVersion & "PCL\Setup.ini")
                                If PageInstanceLeft.Instance IsNot Nothing AndAlso PathUtils.IsSame(PageInstanceLeft.Instance.PathVersion, Instance.PathVersion) Then PageInstanceLeft.Instance = Refreshed
                                If McInstanceSelected IsNot Nothing AndAlso PathUtils.IsSame(McInstanceSelected.PathVersion, Instance.PathVersion) Then McInstanceSelected = Refreshed
                                FrmMain.PageNameRefresh()
                            End Sub)
            End Sub)
        Dim Loader As New LoaderCombo(Of String)(If(Removing, "删除还原点：", If(Restore, "环境恢复：", "保存环境：")) & Instance.Name, {StepLoader}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
        Dim Worker As New RedoableWorker(LoaderFileOperation.CreateWorkload(Loader, Instance.PathVersion))
        If Not McInstanceLock.TryAcquire(Worker, Instance.PathVersion) Then
            Interlocked.Exchange(Busy, 0)
            Return
        End If
        AddHandler Worker.Stopped, Sub()
                                       Interlocked.Exchange(Busy, 0)
                                       RunInUi(Sub() RaiseEvent Changed())
                                   End Sub
        Worker.Start()
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
    End Sub
End Class
