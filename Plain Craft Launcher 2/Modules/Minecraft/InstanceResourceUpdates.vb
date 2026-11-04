Imports System.Net.Http
Imports System.Threading
Imports OnlineEngine = PCLCS.InstanceResourceOnlineFiles(Of PCL.InstanceResourceEntry, PCL.ResourceVersion, PCL.ModNet.NetFile, PCL.InstanceResourceOnlineInfo)
Imports OnlineHost = PCLCS.InstanceResourceOnlineHost(Of PCL.ResourceVersion, PCL.ModNet.NetFile)

Friend Class InstanceResourceOnlineInfo
    Inherits PCLCS.InstanceResourceOnlineInfoData(Of InstanceResourceEntry, ResourceVersion)
End Class

'原 ResourceVersion / NetFile / LoaderDownload 与注入式网络入口继续由 LF 宿主提供。
Friend NotInheritable Class InstanceResourceUpdates
    Private Shared ReadOnly Files As New OnlineEngine(AddressOf PathUtils.RemoveExtendedPrefix, CreateHost(), Function() New InstanceResourceOnlineInfo())

    Private Shared Function CreateHost() As OnlineHost
        Return New OnlineHost With {
            .ParseVersion = Function(Data, Kind) ResourceVersion.FromPlatformJson(Data, If(Kind = CInt(InstanceResourceKind.ResourcePacks), ResourceTypes.ResourcePack, ResourceTypes.Shader)),
            .DownloadAvailable = Function(Value) Value.DownloadAvailable,
            .Size = Function(Value) Value.Size,
            .ReleaseType = Function(Value) CInt(Value.ReleaseType),
            .Hash = Function(Value) Value.Hash,
            .FileName = Function(Value) Value.FileName,
            .Version = Function(Value) Value.Version,
            .Display = Function(Value) Value.Display,
            .DownloadUrls = Function(Value) Value.DownloadUrls,
            .ReleaseDate = Function(Value) Value.ReleaseDate,
            .ToNetFile = Function(Value, FileName, GameVersion) Value.ToNetFile(FileName, ResourceVersion.DownloadReason.Update, GameVersion),
            .Request = Function(Url, Method, Body) DlModRequest(Url, Method, Body, If(Body Is Nothing, Nothing, "application/json")),
            .Fingerprint = Function(FileName) New LocalResourceFile(FileName).CurseForgeHash,
            .Download = AddressOf DownloadVersions,
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)}
    End Function

    Private Shared Sub DownloadVersions(Files As List(Of NetFile), Cancellation As CancellationToken)
        Dim Loader As New LoaderDownload("下载资源更新", Files)
        Using Registration = Cancellation.Register(AddressOf Loader.Cancel)
            Loader.WaitForExit(c:=Cancellation)
        End Using
    End Sub

    Friend Shared Function Supported(Kind As InstanceResourceKind) As Boolean
        Return OnlineEngine.Supported(CInt(Kind))
    End Function
    Friend Shared Function FileHash(FileName As String, Optional Md5 As Boolean = False) As String
        Return OnlineEngine.FileHash(FileName, Md5)
    End Function
    Friend Shared Function Check(Root As String, Items As IEnumerable(Of InstanceResourceEntry), Kind As InstanceResourceKind, GameVersion As String, Token As CancellationToken,
        Optional Request As Func(Of String, HttpMethod, String, JToken) = Nothing, Optional Fingerprint As Func(Of String, UInteger) = Nothing) As List(Of InstanceResourceOnlineInfo)
        Return Files.Check(Root, Items, CInt(Kind), GameVersion, Token, Request, Fingerprint)
    End Function
    Friend Shared Sub Install(Root As String, Infos As IEnumerable(Of InstanceResourceOnlineInfo), Recycle As String, Kind As InstanceResourceKind, GameVersion As String, Token As CancellationToken,
        Optional Download As Action(Of List(Of NetFile), CancellationToken) = Nothing)
        Files.Install(Root, Infos, Recycle, CInt(Kind), GameVersion, Token, Download)
    End Sub
End Class
