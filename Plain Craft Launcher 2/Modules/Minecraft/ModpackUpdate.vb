Imports System.IO.Compression
Imports System.Net.Http
Imports UpdateEngine = PCLCS.PackUpdateEngine(Of PCL.PackUpdateFile, PCL.PackUpdatePackage, PCL.PackUpdateChange, PCL.PackUpdatePlan, PCL.ModNet.NetFile)

Friend Class PackUpdateFile
    Inherits PCLCS.PackUpdateFileData
End Class
Friend Class PackUpdatePackage
    Inherits PCLCS.PackUpdatePackageData(Of PackUpdateFile)
End Class
Friend Class PackUpdateChange
    Inherits PCLCS.PackUpdateChangeData(Of PackUpdateFile)
End Class
Friend Class PackUpdatePlan
    Inherits PCLCS.PackUpdatePlanData(Of PackUpdatePackage, PackUpdateChange)
End Class

'原反射入口和 DTO 类型保留；事务、清单、冲突与恢复全部在 C#。
Friend NotInheritable Class ModpackUpdate
    Friend Const ManifestName As String = "PCL\ModpackOrigin.json"
    Friend Shared ReadOnly Engine As New UpdateEngine(New PCLCS.PackUpdateHost(Of PackUpdateFile, PackUpdatePackage, PackUpdateChange, PackUpdatePlan, NetFile) With {
        .CreateFile = Function() New PackUpdateFile,
        .CreatePackage = Function() New PackUpdatePackage,
        .CreateChange = Function() New PackUpdateChange,
        .CreatePlan = Function() New PackUpdatePlan,
        .EnsureWithin = Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
        .AtomicWrite = Sub(FileName, Bytes) LocalGameData.AtomicWrite(FileName, Bytes, Nothing, False),
        .Request = Function(Url, Method, Body) DlModRequest(Url, Method, Body, If(Body Is Nothing, Nothing, "application/json")),
        .ParseVersion = Function(Data, Kind) ResourceVersion.FromPlatformJson(Data, Kind),
        .CreateDownload = Function(Urls, FileName, Hash) New NetFile(Urls, FileName, New FileChecker With {.Hash = Hash}, True),
        .Download = AddressOf DownloadFiles,
        .TempFolder = Function() RequestTaskTempFolder(),
        .InstanceGameRoot = Function(VersionFolder) New McInstance(VersionFolder).PathIndie,
        .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)})
    Private Shared ReadOnly Downloads As New PCLCS.PackUpdateDownloads(Of NetFile, LoaderDownload, ProgressProvider)(
        New PCLCS.PackUpdateDownloadHost(Of NetFile, LoaderDownload, ProgressProvider) With {
            .CreateLoader = Function(Name, Files) New LoaderDownload(Name, Files),
            .LockState = Function(Loader) Loader.LockState,
            .Cancel = Sub(Loader) Loader.Cancel(),
            .Start = Sub(Loader) Loader.Start(),
            .Wait = Sub(Loader, Token, Progress, StartIfNeeded) Loader.WaitForExit(c:=Token, p:=Progress, StartIfNeeded:=StartIfNeeded),
            .SetProgress = Sub(Progress, Value) Progress.Set(Value)})
    Friend Shared Function RelativePath(Value As String) As String
        Return UpdateEngine.RelativePath(Value)
    End Function
    Friend Shared Function Target(Root As String, Relative As String) As String
        Return Engine.Target(Root, Relative)
    End Function
    Friend Shared Function Digest(FileName As String, Optional Algorithm As String = "sha1") As String
        Return UpdateEngine.Digest(FileName, Algorithm)
    End Function
    Private Shared Function Matches(FileName As String, Expected As String) As Boolean
        Return UpdateEngine.Matches(FileName, Expected)
    End Function
    Private Shared Function ValidHash(Algorithm As String, Value As String) As String
        Return UpdateEngine.ValidHash(Algorithm, Value)
    End Function
    Private Shared Function IsPrivate(Relative As String) As Boolean
        Return UpdateEngine.IsPrivate(Relative)
    End Function
    Private Shared Function IsPersonal(Relative As String) As Boolean
        Return UpdateEngine.IsPersonal(Relative)
    End Function
    Private Shared Function PersonalAreaExists(Root As String, Relative As String) As Boolean
        Return Engine.PersonalAreaExists(Root, Relative)
    End Function
    Private Shared Function JsonEntry(Entry As ZipArchiveEntry) As JObject
        Return UpdateEngine.JsonEntry(Entry)
    End Function
    Friend Shared Function ReadPackage(ArchivePath As String, Stage As String, Token As CancellationToken,
        Optional Request As Func(Of String, HttpMethod, String, JToken) = Nothing) As PackUpdatePackage
        Return Engine.ReadPackage(ArchivePath, Stage, Token, Request)
    End Function
    Private Shared Sub AddFile(Package As PackUpdatePackage, Value As PackUpdateFile)
        Engine.AddFile(Package, Value)
    End Sub
    Friend Shared Function Manifest(Package As PackUpdatePackage) As JObject
        Return UpdateEngine.Manifest(Package)
    End Function
    Friend Shared Function ReadManifest(VersionFolder As String) As JObject
        Return Engine.ReadManifest(VersionFolder)
    End Function
    Friend Shared Function Detect(VersionFolder As String) As JObject
        Return Engine.Detect(VersionFolder)
    End Function
    Friend Shared Function Plan(Root As String, Baseline As JObject, Package As PackUpdatePackage) As PackUpdatePlan
        Return Engine.Plan(Root, Baseline, Package)
    End Function
    Friend Shared Sub DownloadChanges(Plan As PackUpdatePlan, Stage As String, Token As CancellationToken,
        Optional Download As Action(Of List(Of NetFile), CancellationToken) = Nothing)
        Engine.DownloadChanges(Plan, Stage, Token, Download)
    End Sub
    Private Shared Function ModIds(FileName As String) As HashSet(Of String)
        Return UpdateEngine.ModIds(FileName)
    End Function
    Private Shared Sub PreserveModCollisions(Plan As PackUpdatePlan)
        Engine.PreserveModCollisions(Plan)
    End Sub
    Friend Shared Sub DownloadFiles(Files As List(Of NetFile), Token As CancellationToken)
        DownloadFilesWithProgress(Files, Token, Nothing)
    End Sub
    Friend Shared Sub DownloadFilesWithProgress(Files As List(Of NetFile), Token As CancellationToken, Progress As ProgressProvider)
        Downloads.Run(Files, Token, Progress)
    End Sub
    Friend Shared Function Commit(Plan As PackUpdatePlan, VersionFolder As String, Token As CancellationToken,
        Optional ExtraFiles As Dictionary(Of String, String) = Nothing, Optional BeforeApply As Action(Of Integer) = Nothing) As String
        Return Engine.Commit(Plan, VersionFolder, Token, ExtraFiles, BeforeApply)
    End Function
    Friend Shared Function ApplyTransaction(GameRoot As String, VersionFolder As String, Operations As List(Of Tuple(Of String, String, String)), Token As CancellationToken,
        Optional BeforeApply As Action(Of Integer) = Nothing, Optional Box As String = Nothing) As String
        Return Engine.ApplyTransaction(GameRoot, VersionFolder, Operations, Token, BeforeApply, Box)
    End Function
    Private Shared Sub ReplaceFrom(Source As String, Destination As String)
        UpdateEngine.ReplaceFrom(Source, Destination)
    End Sub
    Friend Shared Function Pending(VersionFolder As String) As List(Of String)
        Return Engine.Pending(VersionFolder)
    End Function
    Friend Shared Sub Recover(VersionFolder As String, GameRoot As String)
        Engine.Recover(VersionFolder, GameRoot)
    End Sub
    Friend Shared Sub RecordInstalled(ArchivePath As String, VersionFolder As String, Optional Source As JObject = Nothing)
        Engine.RecordInstalled(ArchivePath, VersionFolder, Source)
    End Sub
End Class
