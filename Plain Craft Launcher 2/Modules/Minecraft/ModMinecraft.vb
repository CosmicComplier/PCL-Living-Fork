Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports MinecraftCore = PCLCS.LegacyMinecraftService(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McFolder, PCL.ModMinecraft.McLibEntry, PCL.ModNet.NetFile, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModMinecraft.McFolder.Types)

'原 LF 公共模块、嵌套模型身份和平台入口；实例／目录／库／资源业务由 C# 单实现维护。
Public Module ModMinecraft
    Private _McFolderSelected As String
    Private _McInstanceSelected As McInstance
    Public McFolderList As New List(Of McFolder)
    Public McInstanceList As New Dictionary(Of McInstanceCardType, List(Of McInstance))
    Public McInstanceListForceRefresh As Boolean = False
    Private IsFirstMcInstanceListLoad As Boolean = True
    Private OSVersion As String = My.Computer.Info.OSVersion
    Public McFolderListLoader As New LoaderTask(Of Integer, Integer)("Minecraft Folder List", AddressOf McFolderListLoadSub, Priority:=ThreadPriority.AboveNormal)
    Public McInstanceListLoader As New LoaderTask(Of String, Integer)("Minecraft Instance List", AddressOf InitMcInstanceList) With {.ReloadTimeout = 1}
    Private ReadOnly InstanceHost As PCLCS.LegacyMinecraftInstanceHost(Of McInstance) = CreateInstanceHost()
    Private ReadOnly Service As New MinecraftCore(CreateMinecraftHost())

    Private Function CreateInstanceHost() As PCLCS.LegacyMinecraftInstanceHost(Of McInstance)
        Return New PCLCS.LegacyMinecraftInstanceHost(Of McInstance) With {
            .SelectedFolder = Function() McFolderSelected,
            .CreateInstance = Function(Name) New McInstance(Name),
            .ReadString = Function(Key, Instance) Settings.Get(Of String)(Key, Instance:=Instance),
            .ReadInteger = Function(Key, Instance) Settings.Get(Of Integer)(Key, Instance:=Instance),
            .ReadBoolean = Function(Key, Instance) Settings.Get(Of Boolean)(Key, Instance:=Instance),
            .HasSaved = Function(Key, Instance) Settings.HasSaved(Key, Instance:=Instance),
            .WriteString = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .WriteInteger = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .WriteBoolean = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .ReadIni = Function(File, Key, DefaultValue) ReadIni(File, Key, DefaultValue),
            .WriteIni = Sub(File, Key, Value) WriteIni(File, Key, Value),
            .ClearIniCache = Sub(File) IniClearCache(File),
            .ClearInstanceSettingsCache = AddressOf ClearInstanceSettingsCache,
            .OnUiThread = Function() RunInUi(),
            .ImagePath = Function() PathImage,
            .NormalizeResourceUri = Function(Address) MyBitmap.NormalizeResourceUri(Address),
            .LogoForStorage = Function(Address) MinecraftLogoCompatibility.ToStorageUri(Address),
            .ReadVersionLogo = Function(File, DefaultValue) MinecraftLogoCompatibility.ReadVersionLogo(File, DefaultValue),
            .WriteVersionLogo = Sub(File, Address) MinecraftLogoCompatibility.WriteVersionLogo(File, Address)
        }
    End Function
    Private Function CreateMinecraftHost() As PCLCS.LegacyMinecraftHost(Of McInstance, McFolder, McLibEntry, NetFile, McInstanceCardType)
        Return New PCLCS.LegacyMinecraftHost(Of McInstance, McFolder, McLibEntry, NetFile, McInstanceCardType) With {
            .Instance = InstanceHost,
            .ModpackEnabled = Function() ModModpackMode.Enabled,
            .PackFolder = Function() MinecraftFolderPolicy.PackFolder(Paths.Base),
            .PackFolders = Function() ModModpackMode.CreatePackFolderList(),
            .FolderCache = Function() _McFolderSelected,
            .StoreFolderCache = Sub(Value) _McFolderSelected = Value,
            .Folders = Function() McFolderList,
            .StoreFolders = Sub(Value) McFolderList = Value,
            .SelectedInstance = Function() _McInstanceSelected,
            .StoreSelectedInstance = Sub(Value) _McInstanceSelected = Value,
            .Instances = Function() McInstanceList,
            .StoreInstances = Sub(Value) McInstanceList = Value,
            .ForceRefresh = Function() McInstanceListForceRefresh,
            .StoreForceRefresh = Sub(Value) McInstanceListForceRefresh = Value,
            .FirstLoad = Function() IsFirstMcInstanceListLoad,
            .StoreFirstLoad = Sub(Value) IsFirstMcInstanceListLoad = Value,
            .AnimationControlEnabled = Function() AniControlEnabled,
            .RefreshRecentWorlds = Sub() RunInUi(Sub() FrmLaunchRight?.RefreshRecentWorlds()),
            .ClearDownloadFolderCache = Sub() PageDownloadResourceDetail.CachedFolder.Clear(),
            .CheckPermission = AddressOf CheckPermissionWithException,
            .RandomDelay = Sub(Minimum, Maximum) Thread.Sleep(RandomInteger(Minimum, Maximum)),
            .RepairFolderIcons = Function(Folders) MinecraftLogoCompatibility.RepairFolders(Folders),
            .Hint = AddressOf PublishMinecraftHint,
            .Message = Sub(Text, Title, Warn) MyMsgBox(Text, Title, IsWarn:=Warn),
            .SelectSkin = Function() Dialogs.SelectFile("选择皮肤文件", False, filter:={({"png", "jpg", "jpeg", "webp"}, "皮肤文件")}).FirstOrDefault(),
            .ImageSize = AddressOf ReadSkinImageSize,
            .SelectSkinModel = Function() MyMsgBox("此皮肤为 Steve 模型（粗手臂）还是 Alex 模型（细手臂）？", "选择皮肤种类", "Steve 模型", "Alex 模型", "我不知道", HighLight:=False),
            .RequestJson = Function(Address, Backup) NetRequestByClientRetry(Address, BackupUrl:=Backup, RequireJson:=True),
            .DownloadFile = Sub(Address, Destination) NetDownloadByClient(Address, Destination),
            .TempFolder = Function() PathTemp,
            .PureFolder = Function() PathPure.Value,
            .OsVersion = Function() OSVersion,
            .NextUuid = Function() GetUuid(),
            .ClientJar = Function(Instance, CheckClient) DlClientJarGet(Instance, CheckClient),
            .CreateNetFile = Function(Urls, Destination, Checker) New NetFile(Urls, Destination, Checker),
            .DownloadPath = Function(File) File.LocalPath,
            .PreferMojang = Function() DlSourcePreferMojang,
            .LauncherSources = Function(Address) DlSourceLauncherOrMetaGet(Address),
            .LibrarySources = Function(Address) DlSourceLibraryGet(Address),
            .AssetSources = Function(Address) DlSourceAssetsGet(Address),
            .ExtractResource = Sub(Destination, Name) ExtractResources(Destination, Name),
            .AccessToken = Function() McLoginLoader.Output.AccessToken,
            .LoginUuid = Function() McLoginLoader.Output.Uuid,
            .ShowUpdateNotice = Function(Notice) MyMsgBox(Notice.Text, "Minecraft 更新提示", "确定", "下载", Notice.ThirdButton, Button3Action:=Notice.ThirdAction),
            .ShowUpdateLog = Sub(Version) McUpdateLogShow(Version),
            .OpenDownload = AddressOf OpenMinecraftDownload
        }
    End Function
    Private Sub ClearInstanceSettingsCache(Instance As McInstance)
        For Each Entry In Settings.Entries.Values
            If Entry.Source = Settings.Sources.Instance Then Entry.ValueCache.Remove(Instance.PathVersion)
        Next
    End Sub
    Private Sub PublishMinecraftHint(Text As String, IsError As Boolean)
        If IsError Then
            Hint(Text, HintType.Red)
        Else
            Hint(Text)
        End If
    End Sub
    Private Function ReadSkinImageSize(File As String) As (Width As Double, Height As Double)
        Dim Image As New MyBitmap(File)
        Return (Image.Pic.Width, Image.Pic.Height)
    End Function
    Private Sub OpenMinecraftDownload(VersionName As String)
        RunInUi(
        Sub()
            PageDownloadInstall.VersionWaitingSelect = VersionName
            FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
        End Sub)
    End Sub

    Public Property McFolderSelected As String
        Get
            Return Service.McFolderSelected
        End Get
        Set(Value As String)
            Service.McFolderSelected = Value
        End Set
    End Property
    Private Sub SetMcFolder(Value As String)
        Service.McFolderSelected = Value
    End Sub
    Private Sub McFolderListLoadSub()
        Service.McFolderListLoad()
    End Sub
    Friend Sub RepairSharedMinecraftIcons()
        Service.RepairSharedMinecraftIcons()
    End Sub
    Public Sub McFolderLauncherProfilesJsonCreate(Folder As String)
        Service.McFolderLauncherProfilesJsonCreate(Folder)
    End Sub
    Public Property McInstanceSelected As McInstance
        Get
            Return _McInstanceSelected
        End Get
        Set(Value As McInstance)
            Service.SelectInstance(Value)
        End Set
    End Property

    Public Class McFolder
        Inherits PCLCS.LegacyMinecraftFolder(Of Types)
        Public Enum Types
            Vanilla
            RenamedVanilla
            Custom
        End Enum
        Public Overrides Function Equals(Obj As Object) As Boolean
            Return TypeOf Obj Is McFolder AndAlso MyBase.Equals(Obj)
        End Function
    End Class

    Public Class McInstance
        Inherits PCLCS.LegacyMcInstance(Of McInstance, McInstanceState, McInstanceCardType)
        Public Enum McInstanceState
            [Error]
            Original
            Snapshot
            Fool
            OptiFine
            Old
            Forge
            NeoForge
            LiteLoader
            Fabric
        End Enum
        Public Sub New(NameOrFullPath As String)
            MyBase.New(NameOrFullPath, Function() InstanceHost)
        End Sub
        Friend Shadows ReadOnly Property LogoForStorage As String
            Get
                Return MyBase.LogoForStorage
            End Get
        End Property

        Public Function ToListItem(Optional ContentHandler As Action(Of MyListItem, EventArgs) = Nothing) As MyVirtualizingElement(Of MyListItem)
            Return New MyVirtualizingElement(Of MyListItem)(
            Function()
                Dim NewItem As New MyListItem With {
                    .Info = Info, .Height = 42, .Tag = Me, .SnapsToDevicePixels = True, .Type = MyListItem.CheckType.Clickable,
                    .ContentHandler = ContentHandler
                }
                NewItem.Inlines.Clear()
                NewItem.Inlines.Add(New Run(Name))
                If ReadIni(PathVersion & "PCL\Setup.ini", "CustomInfo") <> "" Then
                    NewItem.Inlines.Add(New Run("  |  " & VersionDisplayName()) With {.Foreground = New MyColor(215, 215, 215), .FontSize = 12})
                End If
                Try
                    If Logo.EndsWithF("PCL\Logo.png") Then
                        NewItem.Logo = PathVersion & "PCL\Logo.png"
                    Else
                        NewItem.Logo = Logo
                    End If
                Catch ex As Exception
                    Logger.Error(ex, "加载版本图标失败", LogBehavior.Toast)
                    NewItem.Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png"
                End Try
                Return NewItem
            End Function) With {.Height = 42}
        End Function
        Public Shared Operator =(a As McInstance, b As McInstance) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return PathUtils.IsSame(a.PathVersion, b.PathVersion)
        End Operator
        Public Shared Operator <>(a As McInstance, b As McInstance) As Boolean
            Return Not (a = b)
        End Operator
    End Class

    Public NotInheritable Class McInstanceLock
        Private Shared ReadOnly LockedPaths As New ConcurrentDictionary(Of String, IWorker)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly Native As New PCLCS.LegacyMinecraftInstanceLocks(LockedPaths)
        Friend Shared ReadOnly Property IsAnyLocked As Boolean
            Get
                Return Native.IsAnyLocked
            End Get
        End Property
        Public Shared Function TryAcquire(Worker As IWorker, InstancePath As String) As Boolean
            Return Native.TryAcquire(Worker, InstancePath)
        End Function
        Public Shared Function IsLocked(Instance As McInstance) As Boolean
            Return Native.IsLocked(Instance?.PathVersion)
        End Function
    End Class
    Public Function GetMcFoolName(Name As String) As String
        Return PCLCS.LegacyMinecraftText.GetMcFoolName(Name)
    End Function
    Private Sub InitMcInstanceList(Loader As LoaderTask(Of String, Integer))
        Service.InitMcInstanceList(Loader.Input, Function() Loader.IsCanceled)
    End Sub
    Private Function InitMcInstanceListWithCache(Folder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Return Service.InitMcInstanceListWithCache(Folder)
    End Function
    Private Function InitMcInstanceListWithoutCache(Folder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Return Service.InitMcInstanceListWithoutCache(Folder)
    End Function
    Public Enum McInstanceCardType
        Star = -1
        Auto = 0
        Hidden = 1
        API = 2
        OriginalLike = 3
        Rubbish = 4
        Fool = 5
        [Error] = 6
    End Enum

    Public Structure McSkinInfo
        Public IsSlim As Boolean
        Public LocalFile As String
        Public IsVaild As Boolean
    End Structure
    Public Function McSkinSelect() As McSkinInfo
        Dim Result = Service.McSkinSelect()
        Return New McSkinInfo With {.IsSlim = Result.IsSlim, .LocalFile = Result.LocalFile, .IsVaild = Result.IsVaild}
    End Function
    Public Function McSkinGetAddress(UUID As String, Type As String) As String
        Return Service.McSkinGetAddress(UUID, Type)
    End Function
    Public Function McSkinDownload(Address As String) As String
        Return Service.McSkinDownload(Address)
    End Function
    Public Function McSkinSex(Uuid As String) As String
        Return PCLCS.LegacyMinecraftText.McSkinSex(Uuid)
    End Function
    Public Class McLibEntry
        Inherits PCLCS.LegacyMinecraftLibrary
    End Class
    Public Function McJsonRuleCheck(RuleToken As JToken) As Boolean
        Return Service.McJsonRuleCheck(RuleToken)
    End Function
    Public Function McLibListGet(Instance As McInstance, IncludeMainJar As Boolean) As List(Of McLibEntry)
        Return Service.McLibListGet(Instance, IncludeMainJar)
    End Function
    Public Function McLibListGetWithJson(JsonObject As JObject, Optional KeepSameNameDifferentVersionResult As Boolean = False, Optional CustomMcFolder As String = Nothing) As List(Of McLibEntry)
        Return Service.McLibListGetWithJson(JsonObject, KeepSameNameDifferentVersionResult, CustomMcFolder)
    End Function
    Public Function McLibNetFilesFromInstance(Instance As McInstance) As List(Of NetFile)
        Return Service.McLibNetFilesFromInstance(Instance)
    End Function
    Public Function McLibNetFilesFromTokens(Libs As List(Of McLibEntry), Optional CustomMcFolder As String = Nothing) As List(Of NetFile)
        Return Service.McLibNetFilesFromTokens(Libs, CustomMcFolder)
    End Function
    Public Function McLibGet(Original As String, Optional WithHead As Boolean = True, Optional IgnoreLiteLoader As Boolean = False, Optional CustomMcFolder As String = Nothing) As String
        Return Service.McLibGet(Original, WithHead, IgnoreLiteLoader, CustomMcFolder)
    End Function
    Public Function ShouldIgnoreFileCheck(Instance As McInstance) As Object
        Return Service.ShouldIgnoreFileCheck(Instance)
    End Function
    Public Function McAssetsGetIndex(Instance As McInstance, Optional ReturnLegacyOnError As Boolean = False, Optional CheckURLEmpty As Boolean = False) As JToken
        Return Service.McAssetsGetIndex(Instance, ReturnLegacyOnError, CheckURLEmpty)
    End Function
    Public Function McAssetsGetIndexName(Instance As McInstance) As String
        Return Service.McAssetsGetIndexName(Instance)
    End Function
    Public Function McAssetsFixList(Instance As McInstance, CheckHash As Boolean, Optional ByRef ProgressFeed As LoaderBase = Nothing) As List(Of NetFile)
        Dim CurrentFeed = ProgressFeed
        Dim Update As Action(Of Double) = Nothing
        If CurrentFeed IsNot Nothing Then Update = Sub(Value) CurrentFeed.Progress = Value
        Return Service.McAssetsFixList(Instance, CheckHash, Update)
    End Function
    Public Sub McDownloadClientUpdateHint(VersionName As String, Json As JObject)
        Service.McDownloadClientUpdateHint(VersionName, Json)
    End Sub
    Public Function FilterAccessToken(Raw As String, FilterChar As Char) As String
        Return Service.FilterAccessToken(Raw, FilterChar)
    End Function
    Public Function FilterUserName(Raw As String, FilterChar As Char) As String
        Return PCLCS.LegacyMinecraftText.FilterUserName(Raw, FilterChar)
    End Function
End Module

Friend NotInheritable Class McInstanceDirectoryScanner
    Private Sub New()
    End Sub
    Friend Shared Function GetDirectories(Folder As String) As DirectoryInfo()
        Return PCLCS.LegacyMinecraftDirectoryScanner.GetDirectories(Folder)
    End Function
    Friend Shared Function HasFiles(Folder As DirectoryInfo) As Boolean
        Return PCLCS.LegacyMinecraftDirectoryScanner.HasFiles(Folder)
    End Function
End Class
