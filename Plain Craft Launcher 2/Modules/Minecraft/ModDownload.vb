Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Http
Imports System.Text
Imports System.Threading

'LF 原类型、公开入口与 Loader/实例/平台服务适配；完整下载决策及解析在 C# 中。
Public Module ModDownload
    Private ReadOnly Sources As New PCLCS.MinecraftDownloadSources(CreateSourceHost())
    Private ReadOnly Catalog As New PCLCS.MinecraftDownloadCatalog(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry)(Sources,
        New PCLCS.MinecraftDownloadCatalogHost(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry) With {
            .Forge = Function(Version, Branch, Inherit) New DlForgeVersionEntry(Version, Branch, Inherit),
            .NeoForge = Function(ApiName) New DlNeoForgeListEntry(ApiName),
            .OptiFine = Function() New DlOptiFineListEntry,
            .LiteLoader = Function() New DlLiteLoaderListEntry})
    Private ReadOnly Client As New PCLCS.MinecraftDownloadClient(Of McInstance, NetFile, LoaderBase, FileChecker)(Sources, CreateClientHost())

    Private Function CreateSourceHost() As PCLCS.MinecraftDownloadSourceHost
        Return New PCLCS.MinecraftDownloadSourceHost With {
            .SettingInteger = Function(Name) Settings.Get(Of Integer)(Name),
            .SettingString = Function(Name) Settings.Get(Of String)(Name),
            .SetSetting = Sub(Name, Value) Settings.Set(Name, Value),
            .Request = Function(Request) NetRequestByClient(Request.Url, Request.Method, Request.Content, Request.ContentType,
                Accept:=Request.Accept, Timeout:=Request.Timeout, RequireJson:=Request.RequireJson, Encoding:=Request.Encoding, SimulateBrowserHeaders:=Request.SimulateBrowserHeaders),
            .RetryRequest = Function(Request) NetRequestByClientRetry(Request.Url, Request.Method, Content:=Request.Content, ContentType:=Request.ContentType,
                Accept:=Request.Accept, RequireJson:=Request.RequireJson, Encoding:=Request.Encoding, SimulateBrowserHeaders:=Request.SimulateBrowserHeaders),
            .LoaderRequest = Function(Url, Browser) NetRequestByLoader(Url, SimulateBrowserHeaders:=Browser),
            .Clock = AddressOf GetTimeMs,
            .Sleep = AddressOf Thread.Sleep,
            .VersionToDrop = AddressOf McVersion.VersionToDrop,
            .TempPath = Function() PathTemp,
            .FileExists = Function(Name) FileUtils.Exists(Name),
            .ReadJson = Function(Name) FileUtils.ReadAsJson(Name),
            .Info = Sub(Message) Logger.Info(Message),
            .Warn = Sub(Message) Logger.Warn(Message),
            .WarnException = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .ErrorDisplay = Function(ErrorValue, Stack) ErrorValue.GetDisplay(Stack),
            .ForgeRecommended = AddressOf McDownloadForgeRecommendedGet,
            .GetDate = Function(Value) GetDate(Value),
            .LocalTime = AddressOf GetLocalTime}
    End Function
    Private Function CreateClientHost() As PCLCS.MinecraftDownloadClientHost(Of McInstance, NetFile, LoaderBase, FileChecker)
        Return New PCLCS.MinecraftDownloadClientHost(Of McInstance, NetFile, LoaderBase, FileChecker) With {
            .InheritName = Function(Instance) Instance.InheritName,
            .CreateInstance = Function(Name) New McInstance(Name),
            .JsonObject = Function(Instance) Instance.JsonObject,
            .Name = Function(Instance) Instance.Name,
            .VersionPath = Function(Instance) Instance.PathVersion,
            .AssetIndex = Function(Instance) McAssetsGetIndex(Instance, True, True),
            .GameFolder = Function() McFolderSelected,
            .TempPath = Function() PathTemp,
            .Checker = Function(Value) New FileChecker With {.MinSize = Value.MinSize, .ActualSize = Value.ActualSize, .Hash = Value.Hash, .IsJson = Value.IsJson},
            .Check = Function(Checker, Name) Checker.Check(Name),
            .NetFile = Function(Urls, Name, Checker) New NetFile(Urls, Name, Checker),
            .LocalPath = Function(File) File.LocalPath,
            .LocalName = Function(File) File.LocalName,
            .SetLocalPath = Sub(File, Name) File.LocalPath = Name,
            .CheckFile = Function(File, Name) File.Check.Check(Name),
            .LibraryFiles = AddressOf McLibNetFilesFromInstance,
            .IgnoreFileCheck = Function(Instance) ShouldIgnoreFileCheck(Instance),
            .AssetFiles = AddressOf ReadAssetFiles,
            .FileExists = Function(Name) FileUtils.Exists(Name),
            .LastWriteTime = Function(Name) FileUtils.GetInfo(Name).LastWriteTime,
            .MoveFile = Sub(SourceName, Target) FileUtils.Move(SourceName, Target),
            .Now = Function() Date.Now,
            .Task = AddressOf NewFileTask,
            .Download = AddressOf NewFileDownload,
            .Combo = AddressOf NewFileCombo,
            .Start = Sub(Loader) Loader.Start(),
            .Info = Sub(Message) Logger.Info(Message),
            .Warn = Sub(Message) Logger.Warn(Message),
            .WarnNone = Sub(Message) Logger.Warn(Message, LogBehavior.None),
            .WarnException = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message)}
    End Function
    Private Function ReadAssetFiles(Instance As McInstance, CheckHash As Boolean, Loader As LoaderBase) As List(Of NetFile)
        Return McAssetsFixList(Instance, CheckHash, Loader)
    End Function
    Private Function NewFileTask(Name As String, Work As Action(Of PCLCS.MinecraftDownloadFileTask(Of NetFile, LoaderBase)), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Dim Result As New LoaderTask(Of String, List(Of NetFile))(Name, Sub(Task)
            Work(New PCLCS.MinecraftDownloadFileTask(Of NetFile, LoaderBase) With {
                .Native = Task, .SetOutput = Sub(Files) Task.Output = Files, .Cancel = AddressOf Task.Cancel})
        End Sub)
        If Options.ProgressWeight.HasValue Then Result.ProgressWeight = Options.ProgressWeight.Value
        If Options.Show.HasValue Then Result.Show = Options.Show.Value
        If Options.Block.HasValue Then Result.Block = Options.Block.Value
        Return Result
    End Function
    Private Function NewFileDownload(Name As String, Files As List(Of NetFile), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Dim Result As New LoaderDownload(Name, Files)
        If Options.ProgressWeight.HasValue Then Result.ProgressWeight = Options.ProgressWeight.Value
        If Options.Show.HasValue Then Result.Show = Options.Show.Value
        If Options.Block.HasValue Then Result.Block = Options.Block.Value
        Return Result
    End Function
    Private Function NewFileCombo(Name As String, Steps As List(Of LoaderBase), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
        Dim Result As New LoaderCombo(Of String)(Name, Steps)
        If Options.Block.HasValue Then Result.Block = Options.Block.Value
        If Options.Show.HasValue Then Result.Show = Options.Show.Value
        If Options.ProgressWeight.HasValue Then Result.ProgressWeight = Options.ProgressWeight.Value
        Return Result
    End Function
    Private Function Wrap(Of TIn, TOut)(Loader As LoaderTask(Of TIn, TOut)) As PCLCS.MinecraftDownloadSourceTask(Of TOut)
        Return New PCLCS.MinecraftDownloadSourceTask(Of TOut) With {
            .Input = Function() Loader.Input,
            .ClearInput = Sub() Loader.Input = Nothing,
            .Output = Function() Loader.Output,
            .SetOutput = Sub(Value) Loader.Output = Value,
            .IsFailed = Function() Loader.State = LoadState.Failed,
            .IsFinished = Function() Loader.State = LoadState.Finished,
            .IsLoading = Function() Loader.State = LoadState.Loading,
            .IsCanceled = Function() Loader.IsCanceled,
            .IsCanceledState = Function() Loader.State = LoadState.Canceled,
            .IsWaiting = Function() Loader.State = LoadState.Waiting,
            .IsForceRestarting = Function() Loader.IsForceRestarting,
            .Error = Function() Loader.Error,
            .Start = Sub(Input, Force) Loader.Start(Input, Force),
            .WaitForExit = Sub(Input, Force) Loader.WaitForExit(Input, IsForceRestart:=Force),
            .SetWaiting = Sub() Loader.State = LoadState.Waiting,
            .Cancel = AddressOf Loader.Cancel}
    End Function

    Public Function DlClientJarGet(Instance As McInstance, ReturnNothingOnFileUseable As Boolean) As NetFile
        Return Client.DlClientJarGet(Instance, ReturnNothingOnFileUseable)
    End Function
    Public Function DlClientAssetIndexGet(Instance As McInstance) As NetFile
        Return Client.DlClientAssetIndexGet(Instance)
    End Function
    Public Function DlClientFix(Instance As McInstance, CheckAssetsHash As Boolean, DownloadAssetIndexInBackground As Boolean) As List(Of LoaderBase)
        Return Client.DlClientFix(Instance, CheckAssetsHash, DownloadAssetIndexInBackground)
    End Function
    Public Property AllDrops As List(Of Integer)
        Get
            Return Sources.AllDrops
        End Get
        Set(value As List(Of Integer))
            Sources.AllDrops = value
        End Set
    End Property

    Public Structure DlClientListResult
        ''' <summary>
        ''' 数据来源名称，如“Mojang”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的 Json 数据。
        ''' </summary>
        Public Value As JObject
    End Structure

    Public Structure DlOptiFineListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of DlOptiFineListEntry)
    End Structure

    Public Structure DlForgeListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of String)
    End Structure

    Public Structure DlNeoForgeListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 所有版本的列表。已经按从新到老排序。
        ''' </summary>
        Public Value As List(Of DlNeoForgeListEntry)
    End Structure

    Public Structure DlLiteLoaderListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As List(Of DlLiteLoaderListEntry)
        ''' <summary>
        ''' 官方源的失败原因。若没有则为 Nothing。
        ''' </summary>
        Public OfficialError As Exception
    End Structure

    Public Structure DlFabricListResult
        ''' <summary>
        ''' 数据来源名称，如“Official”，“BMCLAPI”。
        ''' </summary>
        Public SourceName As String
        ''' <summary>
        ''' 是否为官方的实时数据。
        ''' </summary>
        Public IsOfficial As Boolean
        ''' <summary>
        ''' 获取到的数据。
        ''' </summary>
        Public Value As JObject
    End Structure

    Public Class DlOptiFineListEntry
        Inherits PCLCS.MinecraftOptiFineEntryData
    End Class
    Public MustInherit Class DlForgelikeEntry
        Inherits PCLCS.MinecraftForgelikeEntryData
        Implements IComparable(Of DlForgelikeEntry)
        Public MustOverride Overrides ReadOnly Property LoaderName As String
        Public MustOverride Overrides ReadOnly Property FileExtension As String
        Public Overloads Function CompareTo(other As DlForgelikeEntry) As Integer Implements IComparable(Of DlForgelikeEntry).CompareTo
            Return MyBase.CompareTo(other)
        End Function
    End Class
    Public Class DlForgeVersionEntry
        Inherits DlForgelikeEntry
        Implements PCLCS.IMinecraftForgeVersionFields
        Public ReleaseTime As String
        Public Hash As String = Nothing
        Public IsRecommended As Boolean
        Public Category As String
        Public FileVersion As String
        Private Property CoreReleaseTime As String Implements PCLCS.IMinecraftForgeVersionFields.ReleaseTime
            Get
                Return Me.ReleaseTime
            End Get
            Set(Value As String)
                Me.ReleaseTime = Value
            End Set
        End Property
        Private Property CoreHash As String Implements PCLCS.IMinecraftForgeVersionFields.Hash
            Get
                Return Me.Hash
            End Get
            Set(Value As String)
                Me.Hash = Value
            End Set
        End Property
        Private Property CoreIsRecommended As Boolean Implements PCLCS.IMinecraftForgeVersionFields.IsRecommended
            Get
                Return Me.IsRecommended
            End Get
            Set(Value As Boolean)
                Me.IsRecommended = Value
            End Set
        End Property
        Private Property CoreCategory As String Implements PCLCS.IMinecraftForgeVersionFields.Category
            Get
                Return Me.Category
            End Get
            Set(Value As String)
                Me.Category = Value
            End Set
        End Property
        Private Property CoreFileVersion As String Implements PCLCS.IMinecraftForgeVersionFields.FileVersion
            Get
                Return Me.FileVersion
            End Get
            Set(Value As String)
                Me.FileVersion = Value
            End Set
        End Property
        Public Overrides ReadOnly Property LoaderName As String
            Get
                Return PCLCS.MinecraftDownloadEntries.ForgeLoaderName
            End Get
        End Property
        Public Overrides ReadOnly Property FileExtension As String
            Get
                Return PCLCS.MinecraftDownloadEntries.ForgeFileExtension(Category)
            End Get
        End Property
        Public Sub New(Version As String, Branch As String, Inherit As String)
            PCLCS.MinecraftDownloadEntries.InitializeForge(Me, Version, Branch, Inherit)
        End Sub
    End Class
    Public Class DlNeoForgeListEntry
        Inherits DlForgelikeEntry
        Implements PCLCS.IMinecraftNeoForgeFields
        Public IsBeta As Boolean
        Public ApiName As String
        Private Property CoreIsBeta As Boolean Implements PCLCS.IMinecraftNeoForgeFields.IsBeta
            Get
                Return Me.IsBeta
            End Get
            Set(Value As Boolean)
                Me.IsBeta = Value
            End Set
        End Property
        Private Property CoreApiName As String Implements PCLCS.IMinecraftNeoForgeFields.ApiName
            Get
                Return Me.ApiName
            End Get
            Set(Value As String)
                Me.ApiName = Value
            End Set
        End Property
        Public ReadOnly Property UrlBase As String
            Get
                Return PCLCS.MinecraftDownloadEntries.NeoForgeUrlBase(Me, ApiName)
            End Get
        End Property
        Public Overrides ReadOnly Property LoaderName As String
            Get
                Return PCLCS.MinecraftDownloadEntries.NeoForgeLoaderName
            End Get
        End Property
        Public Overrides ReadOnly Property FileExtension As String
            Get
                Return PCLCS.MinecraftDownloadEntries.JarExtension
            End Get
        End Property
        Public Sub New(ApiName As String)
            PCLCS.MinecraftDownloadEntries.InitializeNeoForge(Me, ApiName)
        End Sub
    End Class
    Public Class DlLiteLoaderListEntry
        Inherits PCLCS.MinecraftLiteLoaderEntryData
    End Class

    Public DlClientListLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Main", AddressOf DlClientListMain)
    Private Sub DlClientListMain(Loader As LoaderTask(Of String, DlClientListResult))
        Sources.RunClientVersionList(Wrap(Loader), Wrap(DlClientListMojangLoader), Wrap(DlClientListBmclapiLoader), Function(Value) Value.Value)
    End Sub

    Public DlClientListMojangLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Mojang", AddressOf DlClientListMojangMain)
    Private Sub DlClientListMojangMain(Loader As LoaderTask(Of String, DlClientListResult))
        Dim Result = Sources.DlClientListMojangMain()
        Loader.Output = New DlClientListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlClientListBmclapiLoader As New LoaderTask(Of String, DlClientListResult)("DlClientList Bmclapi", AddressOf DlClientListBmclapiMain)
    Private Sub DlClientListBmclapiMain(Loader As LoaderTask(Of String, DlClientListResult))
        Dim Result = Sources.DlClientListBmclapiMain(Loader.Input, Function() DlClientListLoader.Output.Value)
        Loader.Output = New DlClientListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlOptiFineListLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Main", AddressOf DlOptiFineListMain)
    Private Sub DlOptiFineListMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Sources.RunVersionList(Wrap(Loader), Wrap(DlOptiFineListOfficialLoader), Wrap(DlOptiFineListBmclapiLoader))
    End Sub

    Public DlOptiFineListOfficialLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Official", AddressOf DlOptiFineListOfficialMain)
    Private Sub DlOptiFineListOfficialMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Result = Catalog.DlOptiFineListOfficialMain()
        Loader.Output = New DlOptiFineListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlOptiFineListBmclapiLoader As New LoaderTask(Of Integer, DlOptiFineListResult)("DlOptiFineList Bmclapi", AddressOf DlOptiFineListBmclapiMain)
    Private Sub DlOptiFineListBmclapiMain(Loader As LoaderTask(Of Integer, DlOptiFineListResult))
        Dim Result = Catalog.DlOptiFineListBmclapiMain()
        Loader.Output = New DlOptiFineListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlForgeListLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Main", AddressOf DlForgeListMain)
    Private Sub DlForgeListMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Sources.RunVersionList(Wrap(Loader), Wrap(DlForgeListOfficialLoader), Wrap(DlForgeListBmclapiLoader))
    End Sub

    Public DlForgeListOfficialLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Official", AddressOf DlForgeListOfficialMain)
    Private Sub DlForgeListOfficialMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result = Catalog.DlForgeListOfficialMain()
        Loader.Output = New DlForgeListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlForgeListBmclapiLoader As New LoaderTask(Of Integer, DlForgeListResult)("DlForgeList Bmclapi", AddressOf DlForgeListBmclapiMain)
    Private Sub DlForgeListBmclapiMain(Loader As LoaderTask(Of Integer, DlForgeListResult))
        Dim Result = Catalog.DlForgeListBmclapiMain()
        Loader.Output = New DlForgeListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlNeoForgeListLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Main", AddressOf DlNeoForgeListMain)
    Private Sub DlNeoForgeListMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))
        Sources.RunVersionList(Wrap(Loader), Wrap(DlNeoForgeListOfficialLoader), Wrap(DlNeoForgeListBmclapiLoader))
    End Sub

    Public DlNeoForgeListOfficialLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Official", AddressOf DlNeoForgeListOfficialMain)
    Private Sub DlNeoForgeListOfficialMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))
        Dim Result = Catalog.DlNeoForgeListMain(False)
        Loader.Output = New DlNeoForgeListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlNeoForgeListBmclapiLoader As New LoaderTask(Of Integer, DlNeoForgeListResult)("DlNeoForgeList Bmclapi", AddressOf DlNeoForgeListBmclapiMain)
    Public Sub DlNeoForgeListBmclapiMain(Loader As LoaderTask(Of Integer, DlNeoForgeListResult))
        Dim Result = Catalog.DlNeoForgeListMain(True)
        Loader.Output = New DlNeoForgeListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlLiteLoaderListLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Main", AddressOf DlLiteLoaderListMain)
    Private Sub DlLiteLoaderListMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Sources.RunVersionList(Wrap(Loader), Wrap(DlLiteLoaderListOfficialLoader), Wrap(DlLiteLoaderListBmclapiLoader))
    End Sub

    Public DlLiteLoaderListOfficialLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Official", AddressOf DlLiteLoaderListOfficialMain)
    Private Sub DlLiteLoaderListOfficialMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result = Catalog.DlLiteLoaderListMain(False)
        Loader.Output = New DlLiteLoaderListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlLiteLoaderListBmclapiLoader As New LoaderTask(Of Integer, DlLiteLoaderListResult)("DlLiteLoaderList Bmclapi", AddressOf DlLiteLoaderListBmclapiMain)
    Private Sub DlLiteLoaderListBmclapiMain(Loader As LoaderTask(Of Integer, DlLiteLoaderListResult))
        Dim Result = Catalog.DlLiteLoaderListMain(True)
        Loader.Output = New DlLiteLoaderListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlFabricListLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Main", AddressOf DlFabricListMain)
    Private Sub DlFabricListMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Sources.RunVersionList(Wrap(Loader), Wrap(DlFabricListOfficialLoader), Wrap(DlFabricListBmclapiLoader))
    End Sub

    Public DlFabricListOfficialLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Official", AddressOf DlFabricListOfficialMain)
    Private Sub DlFabricListOfficialMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result = Catalog.DlFabricListMain(False)
        Loader.Output = New DlFabricListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public DlFabricListBmclapiLoader As New LoaderTask(Of Integer, DlFabricListResult)("DlFabricList Bmclapi", AddressOf DlFabricListBmclapiMain)
    Private Sub DlFabricListBmclapiMain(Loader As LoaderTask(Of Integer, DlFabricListResult))
        Dim Result = Catalog.DlFabricListMain(True)
        Loader.Output = New DlFabricListResult With {.SourceName = Result.SourceName, .IsOfficial = Result.IsOfficial, .Value = Result.Value}
    End Sub

    Public Function DlClientListGet(Id As String)
        Return Sources.DlClientListGet(Id, Wrap(DlClientListLoader), Function(Value) Value.Value)
    End Function
    Public Sub DlForgeVersionMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Dim Official As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Official", AddressOf DlForgeVersionOfficialMain)
        Dim Mirror As New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion Bmclapi", AddressOf DlForgeVersionBmclapiMain)
        Sources.RunVersionList(Wrap(Loader), Wrap(Official), Wrap(Mirror))
    End Sub
    Public Sub DlForgeVersionOfficialMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Loader.Output = Catalog.DlForgeVersionOfficialMain(Loader.Input)
    End Sub
    Public Sub DlForgeVersionBmclapiMain(Loader As LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Loader.Output = Catalog.DlForgeVersionBmclapiMain(Loader.Input)
    End Sub
    Private Function GetNeoForgeEntries(LatestJson As String, LatestLegacyJson As String) As List(Of DlNeoForgeListEntry)
        Return Catalog.GetNeoForgeEntries(LatestJson, LatestLegacyJson)
    End Function
    Public DlFabricApiLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))("Fabric API List Loader",
        Sub(Task) Task.Output = Catalog.DlFabricApiList(Function(Id, Platform, Dependencies) ResourceVersion.FromProjectId(Id, Platform, Dependencies)))
    Public DlOptiFabricLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))("OptiFabric List Loader",
        Sub(Task) Task.Output = Catalog.DlOptiFabricList(Function(Id, Platform, Dependencies) ResourceVersion.FromProjectId(Id, Platform, Dependencies)))

    Public Function DlModRequest(Url As String, Optional Method As HttpMethod = Nothing,
                                 Optional Content As String = Nothing, Optional ContentType As String = Nothing) As JToken
        Return Sources.DlModRequest(Url, Method, Content, ContentType)
    End Function
    Public ReadOnly Property DlSourcePreferMojang As Boolean
        Get
            Return Sources.DlSourcePreferMojang
        End Get
    End Property
    Public Function DlSourceOrder(OfficialUrls As IEnumerable(Of String), MirrorUrls As IEnumerable(Of String)) As IEnumerable(Of String)
        Return Sources.DlSourceOrder(OfficialUrls, MirrorUrls)
    End Function
    Public ReadOnly Property DlVersionListPreferMojang As Boolean
        Get
            Return Sources.DlVersionListPreferMojang
        End Get
    End Property
    Public Function DlVersionListOrder(OfficialUrls As IEnumerable(Of String), MirrorUrls As IEnumerable(Of String)) As IEnumerable(Of String)
        Return Sources.DlVersionListOrder(OfficialUrls, MirrorUrls)
    End Function
    Public Function DlSourceAssetsGet(Original As String) As IEnumerable(Of String)
        Return Sources.DlSourceAssetsGet(Original)
    End Function
    Public Function DlSourceLibraryGet(Original As String) As IEnumerable(Of String)
        Return Sources.DlSourceLibraryGet(Original)
    End Function
    Public Function DlSourceLauncherOrMetaGet(Original As String) As IEnumerable(Of String)
        Return Sources.DlSourceLauncherOrMetaGet(Original)
    End Function
    Public Function DlSourceModGet(Original As String) As String
        Return Sources.DlSourceModGet(Original)
    End Function
    Private Sub DlSourceLoader(Of InputType, OutputType)(MainLoader As LoaderTask(Of InputType, OutputType),
                                                         LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)),
                                                         Optional IsForceRestart As Boolean = False)
        Sources.DlSourceLoader(Wrap(MainLoader), LoaderList.Select(Function(Item) New KeyValuePair(Of PCLCS.MinecraftDownloadSourceTask(Of OutputType), Integer)(Wrap(Item.Key), Item.Value)).ToList(), IsForceRestart)
    End Sub
    Private Sub DlSourceLoaderCancel(Of InputType, OutputType)(LoaderList As List(Of KeyValuePair(Of LoaderTask(Of InputType, OutputType), Integer)))
        Sources.DlSourceLoaderCancel(LoaderList.Select(Function(Item) New KeyValuePair(Of PCLCS.MinecraftDownloadSourceTask(Of OutputType), Integer)(Wrap(Item.Key), Item.Value)).ToList())
    End Sub
End Module
