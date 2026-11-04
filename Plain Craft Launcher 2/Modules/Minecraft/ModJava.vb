Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

'保留 LF Java 公共入口及原 WithEvents 工作器；规则、设置迁移、选择和下载计划由 C# 单实现维护。
Public Module ModJava
    ''' <summary>搜索、检查并缓存 Java，完成后由原事件刷新设置页。</summary>
    Public WithEvents JavaListRefreshWorker As New RedoableWorker(Sub(c, p) JavaUtils.RefreshListAsync(c, p).Run())
    Private ReadOnly Service As New PCLCS.LegacyJavaService(Of McInstance)(CreateJavaHost())

    Private Function CreateJavaHost() As PCLCS.LegacyJavaHost(Of McInstance)
        Return New PCLCS.LegacyJavaHost(Of McInstance) With {
            .ReadString = Function(Key, Instance) Settings.Get(Of String)(Key, Instance:=Instance),
            .ReadInteger = Function(Key, Instance) Settings.Get(Of Integer)(Key, Instance:=Instance),
            .WriteInteger = Sub(Key, Value, Instance) Settings.Set(Key, Value, Instance:=Instance),
            .Config = Function(Instance) Instance.Config,
            .Version = Function(Instance) Instance.Version,
            .VersionPath = Function(Instance) Instance.PathVersion,
            .InstanceName = Function(Instance) Instance.Name,
            .VersionDisplayName = Function(Instance) Instance.VersionDisplayName,
            .ReleaseTime = Function(Instance) Instance.ReleaseTime,
            .JsonObject = Function(Instance) Instance.JsonObject,
            .JsonVersion = Function(Instance) Instance.JsonVersion,
            .IsNideLogin = Function() Settings.Get(Of McLoginType)("LoginType") = McLoginType.Nide,
            .RefreshWorker = Function() JavaListRefreshWorker,
            .UpdateJavaLists = AddressOf UpdateJavaLists,
            .SelectJavaExecutable = Function() Dialogs.SelectFile("选择 java.exe（可能在 bin 文件夹中）", False, filter:="java.exe|java.exe").FirstOrDefault(),
            .Hint = AddressOf PublishJavaHint,
            .ReloadCurrentJava = Sub() PageInstanceLeft.ReloadCurrentJava(),
            .RunInThread = Sub(Work) RunInThread(Work),
            .SwitchToInstanceSetup = AddressOf SwitchToInstanceSetup,
            .Message = Sub(Caption, Title, IsWarn, ForceWait) MyMsgBox(Caption, Title, IsWarn:=IsWarn, ForceWait:=ForceWait),
            .DisableJavaSettings = AddressOf DisableJavaSettings,
            .RestoreJavaSettings = AddressOf RestoreJavaSettings,
            .VersionListOrder = Function(Official, Mirror) DlVersionListOrder(Official, Mirror),
            .SourceOrder = Function(Official, Mirror) DlSourceOrder(Official, Mirror),
            .RequestJson = Function(Urls) NetRequestByLoader(Urls, IsJson:=True),
            .CreateDownload = AddressOf CreateJavaDownload
        }
    End Function

    ''' <summary>初始化原 Java 缓存并迁移旧版设置；无可用项目时触发搜索。</summary>
    Public Sub JavaInit()
        Service.JavaInit()
    End Sub

    Public Sub UpdateJavaLists() Handles JavaListRefreshWorker.Started, JavaListRefreshWorker.Stopped
        RunInUi(
        Sub()
            FrmSetupLaunch?.UpdateJavaList()
            FrmInstanceSetup?.UpdateJavaList()
        End Sub)
    End Sub

    Public Sub ManuallyImportJava()
        Service.ManuallyImportJava()
    End Sub
    Public Sub ManuallyRemoveJava(JavaEntry As Java)
        Service.ManuallyRemoveJava(JavaEntry)
    End Sub
    Public Sub ManuallySearchJava(Optional CancellationToken As CancellationToken = Nothing)
        Service.ManuallySearchJava(CancellationToken)
    End Sub

    Public Function GetJavaRequirement(Instance As McInstance) As (Range As ValueRange(Of Version), RecommendedComponent As String)
        Return Service.GetJavaRequirement(Instance)
    End Function

    Public Function SelectOrDownloadJava(Target As OneOf(Of ValueRange(Of Version), McInstance), TryFixOrHint As Boolean, Optional c As CancellationToken = Nothing, Optional p As ProgressProvider = Nothing) As Java
        Return Service.SelectOrDownloadJava(Target, TryFixOrHint, c, p)
    End Function

    Private Sub PublishJavaHint(Text As String, Kind As PCLCS.LegacyJavaHintKind)
        Hint(Text, If(Kind = PCLCS.LegacyJavaHintKind.Red, HintType.Red, HintType.Green))
    End Sub
    Private Sub SwitchToInstanceSetup(Instance As McInstance)
        RunInUi(
        Sub()
            PageInstanceLeft.Instance = Instance
            FrmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.InstanceSetup)
        End Sub)
    End Sub
    Private Sub DisableJavaSettings()
        If FrmInstanceSetup IsNot Nothing Then RunInUiWait(Sub() FrmInstanceSetup.ComboArgumentJava.IsEnabled = False)
    End Sub
    Private Sub RestoreJavaSettings()
        If FrmInstanceSetup IsNot Nothing Then RunInUiWait(
        Sub()
            SettingService.RefreshSettings(FrmInstanceSetup.ComboArgumentJava)
            SettingService.RefreshSettings(FrmInstanceSetup.ComboArgumentJavaSelect)
            FrmInstanceSetup.ComboArgumentJava.IsEnabled = True
        End Sub)
    End Sub

    '仅适配原 Loader/NetFile 生命周期；manifest 解释、文件选择、校验参数和清理由 C# 计划提供。
    Private Function CreateJavaDownload(Plan As PCLCS.LegacyJavaDownloadPlan) As PCLCS.LegacyJavaDownloadOperation
        Dim FilesToDownload As List(Of NetFile) = Plan.Files.Select(
            Function(File) New NetFile(File.Sources, File.Destination, New FileChecker With {.ActualSize = File.ActualSize, .Hash = File.Hash})).ToList()
        Dim Loader = New LoaderCombo(Of Integer)("下载 Java", New LoaderBase() {
            New LoaderDownload("下载 Java 文件", FilesToDownload) With {.ProgressWeight = 10},
            New LoaderWorker("刷新 Java 列表", JavaListRefreshWorker)
        }) With {.OnStateChanged =
        Sub(RawLoader As LoaderCombo(Of Integer))
            If RawLoader.State = LoadState.Failed OrElse RawLoader.State = LoadState.Canceled Then Plan.Incomplete()
        End Sub}
        Return New PCLCS.LegacyJavaDownloadOperation With {
            .Wait = Sub(c, p) Loader.WaitForExit(IsForceRestart:=True, c:=c, p:=p),
            .Cancel = Sub() Loader.Cancel()
        }
    End Function
End Module
