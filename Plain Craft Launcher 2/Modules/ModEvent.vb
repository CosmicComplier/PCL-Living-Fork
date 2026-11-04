Imports PCL.FormMain
Imports EventController = PCLCS.CustomEventController(Of PCL.CustomEvent.EventType, PCL.FormMain.PageType, PCL.FormMain.PageSubType, PCL.ModMinecraft.McInstance, PCL.ModMain.HintType)
Imports EventHost = PCLCS.CustomEventHost(Of PCL.FormMain.PageType, PCL.FormMain.PageSubType, PCL.ModMinecraft.McInstance, PCL.ModMain.HintType)

<Markup.ContentProperty("Events")>
Public Class CustomEventCollection
    Inherits PCLCS.CustomEventCollectionData(Of CustomEvent)
    Implements IEnumerable(Of CustomEvent)
    Private _Events As New List(Of CustomEvent)
    Protected Overrides ReadOnly Property EventStorage As List(Of CustomEvent)
        Get
            Return _Events
        End Get
    End Property
    Public Shadows ReadOnly Property Events As List(Of CustomEvent)
        Get
            Return MyBase.Events
        End Get
    End Property
    Public Shadows Function GetEnumerator() As IEnumerator(Of CustomEvent) Implements IEnumerable(Of CustomEvent).GetEnumerator
        Return MyBase.GetEnumerator()
    End Function
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return MyBase.GetEnumerator()
    End Function
End Class

Public Class CustomEventService
    Public Shared ReadOnly EventsProperty As DependencyProperty = PCLCS.CustomEventProperties.Collection(GetType(CustomEventService), GetType(CustomEventCollection))
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Sub SetEvents(d As DependencyObject, value As CustomEventCollection)
        PCLCS.CustomEventProperties.Set(d, EventsProperty, value)
    End Sub
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Function GetEvents(d As DependencyObject) As CustomEventCollection
        Return PCLCS.CustomEventProperties.Collection(d, EventsProperty, Function() New CustomEventCollection)
    End Function
    Public Shared ReadOnly EventTypeProperty As DependencyProperty = PCLCS.CustomEventProperties.AttachedType(GetType(CustomEventService), GetType(CustomEvent.EventType))
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Sub SetEventType(d As DependencyObject, value As CustomEvent.EventType)
        PCLCS.CustomEventProperties.Set(d, EventTypeProperty, value)
    End Sub
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Function GetEventType(d As DependencyObject) As CustomEvent.EventType
        Return PCLCS.CustomEventProperties.Get(Of CustomEvent.EventType)(d, EventTypeProperty)
    End Function
    Public Shared ReadOnly EventDataProperty As DependencyProperty = PCLCS.CustomEventProperties.AttachedData(GetType(CustomEventService))
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Sub SetEventData(d As DependencyObject, value As String)
        PCLCS.CustomEventProperties.Set(d, EventDataProperty, value)
    End Sub
    <AttachedPropertyBrowsableForType(GetType(DependencyObject))>
    Public Shared Function GetEventData(d As DependencyObject) As String
        Return PCLCS.CustomEventProperties.Get(Of String)(d, EventDataProperty)
    End Function
End Class

Partial Public Module ModMain
    <Runtime.CompilerServices.Extension>
    Public Sub RaiseCustomEvent(Control As DependencyObject)
        PCLCS.CustomEventBatch.Raise(Function() CustomEventService.GetEvents(Control), Function() CustomEventService.GetEventType(Control), Function() CustomEventService.GetEventData(Control), Function(Type, Data) New CustomEvent(Type, Data), Sub(EventValue) EventValue.Raise(), Sub(Work, Name) RunInNewThread(Work, Name), AddressOf GetUuid)
    End Sub
End Module

Public Class CustomEvent
    Inherits PCLCS.CustomEventObject(Of CustomEvent.EventType)
    Public Shadows Property Type As EventType
        Get
            Return MyBase.Type
        End Get
        Set(value As EventType)
            MyBase.Type = value
        End Set
    End Property
    Public Shadows Property Data As String
        Get
            Return MyBase.Data
        End Get
        Set(value As String)
            MyBase.Data = value
        End Set
    End Property
    Public Shared ReadOnly TypeProperty As DependencyProperty = PCLCS.CustomEventProperties.ObjectType(GetType(CustomEvent), EventType.None)
    Public Shared ReadOnly DataProperty As DependencyProperty = PCLCS.CustomEventProperties.ObjectData(GetType(CustomEvent))
    Protected Overrides ReadOnly Property TypeKey As DependencyProperty
        Get
            Return TypeProperty
        End Get
    End Property
    Protected Overrides ReadOnly Property DataKey As DependencyProperty
        Get
            Return DataProperty
        End Get
    End Property
    Protected Overrides Sub ReadInUi(Work As Action)
        RunInUiWait(Work)
    End Sub
    Public Sub New()
    End Sub
    Public Sub New(Type As EventType, Data As String)
        Me.Type = Type
        Me.Data = Data
    End Sub
    Public Sub Raise()
        Raise(Type, Data)
    End Sub
    Public Enum EventType
        None = 0
        打开网页
        打开文件
        打开帮助
        执行命令
        启动游戏
        复制文本
        刷新主页
        刷新页面
        刷新帮助
        今日人品
        内存优化
        清理垃圾
        弹出窗口
        弹出提示
        切换页面
        导入整合包
        安装整合包
        下载文件
        修改设置
        写入设置
        修改变量
        写入变量
        加入房间
        检查更新
    End Enum
    Private Shared ReadOnly Controller As New EventController(CreateHost())
    Private Shared Function CreateHost() As EventHost
        Dim Host As New EventHost With {
            .RunInThread = AddressOf RunInThread, .RunInUi = Sub(Work) RunInUi(Work), .RunInUiWait = Sub(Work) RunInUiWait(Work), .IsUiThread = Function() RunInUi(),
            .Message = Function(Text, Title, First, Second, Third) MyMsgBox(Text, Title, First, Second, Third),
            .Hint = Sub(Text, Kind) Hint(Text, Kind), .Blue = HintType.Blue, .Green = HintType.Green, .Red = HintType.Red,
            .Info = Sub(Text) Logger.Info(Text), .OpenWebsite = Sub(Url) OpenWebsite(Url), .Clipboard = Sub(Text) ClipboardSet(Text), .EnterHelp = Sub(Path) PageOtherHelp.EnterHelpPage(Path),
            .ErrorAlert = Sub(Err, Text) Logger.Error(Err, Text, LogBehavior.Alert), .WarnToast = Sub(Err, Text) Logger.Warn(Err, Text, LogBehavior.Toast),
            .StartProcess = Sub(Info) StartProcess(Info), .SelectedInstance = Function() McInstanceSelected, .InstanceName = Function(Target) Target.Name,
            .CreateInstance = Function(Name) New McInstance(Name), .Launch = Function(Target, Server) McLaunchStart(New McLaunchOptions With {.ServerIp = Server, .Instance = Target}),
            .RefreshHome = Sub() FrmLaunchRight.ForceRefresh(), .RefreshCurrent = Sub() CType(FrmMain.PageRight, IRefreshable).Refresh(), .CanRefreshCurrent = Function() TypeOf FrmMain.PageRight Is IRefreshable,
            .RefreshHelp = Sub() PageOtherLeft.RefreshHelp(), .Jrrp = AddressOf PageOtherTest.Jrrp,
            .MemoryOptimize = Sub() PageOtherTest.MemoryOptimize(True), .RubbishClear = Sub() PageOtherTest.RubbishClear(),
            .InstallModpack = Sub() ModpackInstall(), .CheckUpdates = AddressOf UpdateCheckByButton,
            .HintCustomCommand = Function() Settings.Get(Of Boolean)("HintCustomCommand"), .RememberCustomCommand = Sub() Settings.Set("HintCustomCommand", True),
            .Locked = AddressOf McInstanceLock.BlocksNavigation, .SetEditedInstance = Sub(Target) PageInstanceLeft.Instance = Target,
            .InstanceSetup = PageType.InstanceSetup, .DefaultSubPage = PageSubType.Default, .ChangePage = Sub(Page, SubPage) FrmMain.PageChange(Page, SubPage),
            .Download = Sub(Url, Name, Folder) PageOtherTest.StartCustomDownload(Url, Name, Folder),
            .SetSetting = Sub(Key, Value, Target) Settings.SetSafe(Key, Value, Target), .RegistryFolder = Function() RegFolder,
            .WriteRegistry = Sub(Path, Name, Value) RegistryUtils.Write(Path, Name, Value),
            .BaseFolder = Function() Paths.Base, .CacheFolder = Function() PathTemp, .RequestTempFolder = Function() RequestTaskTempFolder(),
            .ExtractHelp = AddressOf HelpTryExtract, .FileExists = Function(Path) FileUtils.Exists(Path), .DownloadFile = Sub(Url, Path) NetDownloadByClient(Url, Path)}
#If LEGACY_LINK Then
        Host.LegacyLink = True
        Host.JoinDefault = Sub() PageLinkMain.Join()
        Host.Join = Sub(Arg) PageLinkMain.Join(Arg)
#End If
        Return Host
    End Function
    Public Shared Sub Raise(Type As EventType, Arg As String)
        Controller.Raise(Type, Arg)
    End Sub
    Public Shared Function GetAbsoluteUrls(RelativeUrl As String, Type As EventType) As String()
        Return Controller.GetAbsoluteUrls(RelativeUrl, Type)
    End Function
    Private Shared Function EventSafetyConfirm(Message As String) As Boolean
        Return Controller.SafetyConfirm(Message)
    End Function
End Class
