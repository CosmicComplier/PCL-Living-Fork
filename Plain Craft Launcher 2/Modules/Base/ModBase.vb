Imports System.Text.RegularExpressions
Imports System.Runtime.CompilerServices
Imports System.Xaml
Imports BaseOps = PCLCS.LegacyBaseOperations
Imports BaseSearch = PCLCS.LegacyBaseSearch
Imports BaseUi = PCLLF.Presentation.Foundation.LegacyBaseUi
Imports BaseXaml = PCLLF.Presentation.Foundation.LegacyBaseXaml

Public Module ModBase

#Region "声明"

    '下列版本信息由更新器自动修改
    Public Const VersionBaseName As String = "1.3.4" 'LivingPCL 显示用版本名；内部兼容码沿用代码基线。
    Public Const LauncherDisplayName As String = "PCL Living Fork"
    Public Const LauncherExecutableName As String = "PCL-Living-Fork-" & VersionBaseName & ".exe"
    Public Const LauncherRepository As String = "DDZmumo/PCL-Living-Fork"
    Public Const LauncherRepositoryUrl As String = "https://github.com/" & LauncherRepository
    Public Const LauncherIssuesUrl As String = LauncherRepositoryUrl & "/issues"
    Public Const LauncherReleasesUrl As String = LauncherRepositoryUrl & "/releases"
    Public Const CommitHash As String = "" 'Commit Hash，由 GitHub Workflow 自动替换
#If RELEASE Then
    Public Const VersionCode As Integer = 411 '正式版
#Else
    Public Const VersionCode As Integer = 412 '快照版
#End If

    '版本信息
    Public Const VersionDisplay As String = BuildTypeDisplay & " " & VersionBaseName
#If DEBUG Then
    Public Const BuildTypeDisplay As String = "开发版"
    Public Const BuildType As BuildTypes = BuildTypes.Debug
#ElseIf RELEASE Then
    Public Const BuildTypeDisplay As String = "正式版"
    Public Const BuildType As BuildTypes = BuildTypes.Release
#Else
    Public Const BuildTypeDisplay As String = "快照版"
    Public Const BuildType As BuildTypes = BuildTypes.Snapshot
#End If
    Public Enum BuildTypes
        Debug = 100
        Release = 50
        Snapshot = 0
    End Enum

    ''' <summary>
    ''' 主窗口句柄。
    ''' </summary>
    Public Handle As IntPtr
    ''' <summary>
    ''' 包含程序名的完整路径。
    ''' </summary>
    Public PathExe As String = Paths.Base & AppDomain.CurrentDomain.SetupInformation.ApplicationName
    ''' <summary>
    ''' 程序内嵌图片文件夹路径，以 / 结尾。
    ''' </summary>
    Public PathImage As String = "pack://application:,,,/" & GetType(MyBitmap).Assembly.GetName().Name & ";component/Images/"
    ''' <summary>
    ''' 程序的缓存文件夹路径，以 \ 结尾。
    ''' </summary>
    Public PathTemp As String = BaseOps.CachePath(Function() Settings.Get(Of String)("SystemSystemCache"))
    ''' <summary>
    ''' 当前程序的语言。
    ''' </summary>
    Public Lang As String = "zh_CN"
    ''' <summary>
    ''' 程序的打开计时。
    ''' </summary>
    Public ApplicationStartTick As Long = GetTimeMs()
    ''' <summary>
    ''' 程序打开时的时间。
    ''' </summary>
    Public ApplicationOpenTime As Date = Date.Now
    ''' <summary>
    ''' 识别码。
    ''' </summary>
    Public Identify As String = GetIdentify()
    ''' <summary>
    ''' 程序是否正在结束。
    ''' </summary>
    Public IsProgramEnding As Boolean = False
    ''' <summary>
    ''' 是否使用 GBK 编码。
    ''' </summary>
    Public IsGBKEncoding As Boolean = Encoding.Default.CodePage = 936
    ''' <summary>
    ''' 系统盘盘符，以 \ 结尾。例如 “C:\”。
    ''' </summary>
    Public OsDrive As String = Environment.GetLogicalDrives().First(Function(p) Directory.Exists(p)) '#3799

    Public ReadOnly Property ModeDebug As Boolean
        Get
            Return Settings.Get(Of Boolean)("SystemDebugMode")
        End Get
    End Property

#End Region

#Region "矢量图标"

    Public Class Logo
        ''' <summary>
        ''' 图标按钮，心（空心），1.1x
        ''' </summary>
        Public Const IconButtonLikeLine As String = PCLCS.LegacyBaseIcons.IconButtonLikeLine
        ''' <summary>
        ''' 图标按钮，心（实心），1.1x
        ''' </summary>
        Public Const IconButtonLikeFill As String = PCLCS.LegacyBaseIcons.IconButtonLikeFill
        ''' <summary>
        ''' 图标按钮，垃圾桶，1.1x
        ''' </summary>
        Public Const IconButtonDelete As String = PCLCS.LegacyBaseIcons.IconButtonDelete
        ''' <summary>
        ''' 图标按钮，禁止，1x
        ''' </summary>
        Public Const IconButtonStop As String = PCLCS.LegacyBaseIcons.IconButtonStop
        ''' <summary>
        ''' 图标按钮，勾选，1x
        ''' </summary>
        Public Const IconButtonCheck As String = PCLCS.LegacyBaseIcons.IconButtonCheck
        ''' <summary>
        ''' 图标按钮，笔，1x
        ''' </summary>
        Public Const IconButtonEdit As String = PCLCS.LegacyBaseIcons.IconButtonEdit
        ''' <summary>
        ''' 图标按钮，齿轮，1.1x
        ''' </summary>
        Public Const IconButtonSetup As String = PCLCS.LegacyBaseIcons.IconButtonSetup
        ''' <summary>
        ''' 图标按钮，重置，0.9x
        ''' </summary>
        Public Const IconButtonReset As String = PCLCS.LegacyBaseIcons.IconButtonReset
        ''' <summary>
        ''' 图标按钮，刷新，0.85x
        ''' </summary>
        Public Const IconButtonRefresh As String = PCLCS.LegacyBaseIcons.IconButtonRefresh
        ''' <summary>
        ''' 图标按钮，软盘，1x
        ''' </summary>
        Public Const IconButtonSave As String = PCLCS.LegacyBaseIcons.IconButtonSave
        ''' <summary>
        ''' 图标按钮，信息，1.05x
        ''' </summary>
        Public Const IconButtonInfo As String = PCLCS.LegacyBaseIcons.IconButtonInfo
        ''' <summary>
        ''' 图标按钮，列表，1x
        ''' </summary>
        Public Const IconButtonList As String = PCLCS.LegacyBaseIcons.IconButtonList
        ''' <summary>
        ''' 图标按钮，文件夹，1.1x
        ''' </summary>
        Public Const IconButtonOpen As String = PCLCS.LegacyBaseIcons.IconButtonOpen
        ''' <summary>
        ''' 图标按钮，上箭头，0.95x
        ''' </summary>
        Public Const IconButtonArrowUp As String = PCLCS.LegacyBaseIcons.IconButtonArrowUp
        ''' <summary>
        ''' 图标按钮，下箭头，0.95x
        ''' </summary>
        Public Const IconButtonArrowDown As String = PCLCS.LegacyBaseIcons.IconButtonArrowDown
        ''' <summary>
        ''' 图标按钮，名片，1.1x
        ''' </summary>
        Public Const IconButtonCard As String = PCLCS.LegacyBaseIcons.IconButtonCard
        ''' <summary>
        ''' 图标按钮，×，0.85x
        ''' </summary>
        Public Const IconButtonCross As String = PCLCS.LegacyBaseIcons.IconButtonCross
        ''' <summary>
        ''' 图标按钮，Mojang，1.1x
        ''' </summary>
        Public Const IconButtonMojang As String = PCLCS.LegacyBaseIcons.IconButtonMojang
        ''' <summary>
        ''' 图标按钮，用户，0.95x
        ''' </summary>
        Public Const IconButtonUser As String = PCLCS.LegacyBaseIcons.IconButtonUser
        ''' <summary>
        ''' 图标按钮，盾牌，1x
        ''' </summary>
        Public Const IconButtonShield As String = PCLCS.LegacyBaseIcons.IconButtonShield
        ''' <summary>
        ''' 图标按钮，离线，0.85x
        ''' </summary>
        Public Const IconButtonOffline As String = PCLCS.LegacyBaseIcons.IconButtonOffline
        ''' <summary>
        ''' 图标，服务端，1x
        ''' </summary>
        Public Const IconButtonServer As String = PCLCS.LegacyBaseIcons.IconButtonServer
        ''' <summary>
        ''' 图标，音符，1x
        ''' </summary>
        Public Const IconMusic As String = PCLCS.LegacyBaseIcons.IconMusic
        ''' <summary>
        ''' 图标，播放，0.8x
        ''' </summary>
        Public Const IconPlay As String = PCLCS.LegacyBaseIcons.IconPlay
    End Class

#End Region

#Region "自定义类"

    ''' <summary>
    ''' 支持小数与常见类型隐式转换的颜色；数值规则由 C# 呈现基础实现。
    ''' </summary>
    Public Class MyColor
        Inherits PCLLF.Presentation.Foundation.LegacyColor

        Public Sub New()
            MyBase.New()
        End Sub
        Public Sub New(col As Color)
            MyBase.New(col)
        End Sub
        Public Sub New(HexString As String)
            MyBase.New(HexString)
        End Sub
        Public Sub New(newA As Double, col As MyColor)
            MyBase.New(newA, col)
        End Sub
        Public Sub New(newR As Double, newG As Double, newB As Double)
            MyBase.New(newR, newG, newB)
        End Sub
        Public Sub New(newA As Double, newR As Double, newG As Double, newB As Double)
            MyBase.New(newA, newR, newG, newB)
        End Sub
        Public Sub New(brush As Brush)
            MyBase.New(brush)
        End Sub
        Public Sub New(brush As SolidColorBrush)
            MyBase.New(brush)
        End Sub
        Public Sub New(obj As Object)
            MyBase.New(obj)
        End Sub

        Public Shared Widening Operator CType(str As String) As MyColor
            Return New MyColor(str)
        End Operator
        Public Shared Widening Operator CType(col As Color) As MyColor
            Return New MyColor(col)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Color
            Return conv.ToMediaColor()
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As System.Drawing.Color
            Return conv.ToDrawingColor()
        End Operator
        Public Shared Widening Operator CType(bru As SolidColorBrush) As MyColor
            Return New MyColor(bru.Color)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As SolidColorBrush
            Return conv.ToBrush()
        End Operator
        Public Shared Widening Operator CType(bru As Brush) As MyColor
            Return New MyColor(bru)
        End Operator
        Public Shared Widening Operator CType(conv As MyColor) As Brush
            Return conv.ToBrush()
        End Operator
        Private Shared Function FromChannels(Value As PCLLF.Presentation.Controls.ControlColor) As MyColor
            Return New MyColor(Value.A, Value.R, Value.G, Value.B)
        End Function
        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Add(a, b))
        End Operator
        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Subtract(a, b))
        End Operator
        Public Shared Operator *(a As MyColor, b As Double) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Multiply(a, b))
        End Operator
        Public Shared Operator /(a As MyColor, b As Double) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Divide(a, b))
        End Operator
        Public Shared Operator =(a As MyColor, b As MyColor) As Boolean
            Return PCLLF.Presentation.Foundation.LegacyColor.AreEqual(a, b)
        End Operator
        Public Shared Operator <>(a As MyColor, b As MyColor) As Boolean
            Return Not PCLLF.Presentation.Foundation.LegacyColor.AreEqual(a, b)
        End Operator
        Public Shared Shadows Function Lerp(ValueA As MyColor, ValueB As MyColor, Percent As Double) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Lerp(ValueA, ValueB, Percent))
        End Function
        Public Shared Shadows Function Round(col As MyColor, Optional w As Integer = 0) As MyColor
            Return FromChannels(PCLLF.Presentation.Foundation.LegacyColor.Round(col, w))
        End Function
        Public Shared Shadows Function ClampToByte(d As Double) As Byte
            Return PCLLF.Presentation.Foundation.LegacyColor.ClampToByte(d)
        End Function
        Public Shadows Function Hue(v1 As Double, v2 As Double, vH As Double) As Double
            Return MyBase.Hue(v1, v2, vH)
        End Function
        Public Overrides Function ToString() As String
            Return MyBase.ToString()
        End Function
        Public Shadows Function FromHSL(sH As Double, sS As Double, sL As Double) As MyColor
            MyBase.FromHSL(sH, sS, sL)
            Return Me
        End Function
        Public Shadows Function FromHSL2(sH As Double, sS As Double, sL As Double) As MyColor
            MyBase.FromHSL2(sH, sS, sL)
            Return Me
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            'Preserve VB's original object/operator binding at the legacy type boundary.
            Return Me = obj
        End Function
    End Class

    ''' <summary>
    ''' 支持负数与浮点数的矩形；旧类型仅保留构造入口。
    ''' </summary>
    Public Class MyRect
        Inherits PCLLF.Presentation.Foundation.LegacyRect

        Public Sub New()
            MyBase.New()
        End Sub
        Public Sub New(left As Double, top As Double, width As Double, height As Double)
            MyBase.New(left, top, width, height)
        End Sub
    End Class

    ''' <summary>
    ''' 模块加载状态枚举。
    ''' </summary>
    Public Enum LoadState
        Waiting
        Loading
        Finished
        Failed
        Canceled
    End Enum

    ''' <summary>
    ''' 执行返回值。
    ''' </summary>
    Public Enum ProcessReturnValues
        ''' <summary>
        ''' 执行成功。
        ''' </summary>
        Success = 0
        ''' <summary>
        ''' 执行失败。
        ''' </summary>
        Fail = 1
        ''' <summary>
        ''' 执行时出现未经处理的异常。
        ''' </summary>
        Exception = 2
        ''' <summary>
        ''' 执行超时。
        ''' </summary>
        Timeout = 3
        ''' <summary>
        ''' 取消执行。可能是由于不满足执行的前置条件。
        ''' </summary>
        Cancel = 4
        ''' <summary>
        ''' 任务成功完成。
        ''' </summary>
        TaskDone = 5
    End Enum

#End Region

#Region "文件"

    'ini

    Private ReadOnly IniCache As New ConcurrentDictionary(Of String, ConcurrentDictionary(Of String, String))
    ''' <summary>
    ''' 清除某 ini 文件的运行时缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    Public Sub IniClearCache(FileName As String)
        BaseOps.IniClearCache(FileName, IniCache)
    End Sub
    ''' <summary>
    ''' 获取 ini 文件缓存。如果没有，则新读取 ini 文件内容。
    ''' 在文件不存在或读取失败时返回 Nothing。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    Private Function IniGetContent(FileName As String) As ConcurrentDictionary(Of String, String)
        Return BaseOps.IniGetContent(FileName, IniCache)
    End Function
    ''' <summary>
    ''' 读取 ini 文件。这可能会使用到缓存。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    ''' <param name="Key">键。</param>
    ''' <param name="DefaultValue">没有找到键时返回的默认值。</param>
    Public Function ReadIni(FileName As String, Key As String, Optional DefaultValue As String = "") As String
        Return BaseOps.ReadIni(FileName, Key, DefaultValue, IniCache)
    End Function
    ''' <summary>
    ''' 判断 ini 文件中是否包含某个键。这可能会使用到缓存。
    ''' </summary>
    Public Function HasIniKey(FileName As String, Key As String) As Boolean
        Return BaseOps.HasIniKey(FileName, Key, IniCache)
    End Function
    ''' <summary>
    ''' 从 ini 文件中移除某个键。这会更新缓存。
    ''' </summary>
    Public Sub DeleteIniKey(FileName As String, Key As String)
        BaseOps.WriteIni(FileName, Key, Nothing, IniCache)
    End Sub
    ''' <summary>
    ''' 写入 ini 文件，这会更新缓存。
    ''' 若 Value 为 Nothing，则删除该键。
    ''' </summary>
    ''' <param name="FileName">文件完整路径或简写文件名。简写将会使用“ApplicationName\文件名.ini”作为路径。</param>
    ''' <param name="Key">键。</param>
    ''' <param name="Value">值。</param>
    Public Sub WriteIni(FileName As String, Key As String, Value As String)
        BaseOps.WriteIni(FileName, Key, Value, IniCache)
    End Sub

    '权限检查

    ''' <summary>
    ''' 检查是否拥有某一文件夹的 I/O 权限。如果文件夹不存在，会返回 False。
    ''' </summary>
    Public Function CheckPermission(Folder As String) As Boolean
        Return BaseOps.CheckPermission(Folder, AddressOf GetUuid)
    End Function
    ''' <summary>
    ''' 检查是否拥有某一文件夹的 I/O 权限。如果出错，则抛出异常。
    ''' </summary>
    Public Sub CheckPermissionWithException(Folder As String)
        BaseOps.CheckPermissionWithException(Folder, AddressOf GetUuid)
    End Sub

#End Region

#Region "文本"
    Public vbLQ As Char = Convert.ToChar(8220)
    Public vbRQ As Char = Convert.ToChar(8221)

    ''' <summary>
    ''' 不会报错的 Val。
    ''' 如果输入有误，返回 0。
    ''' </summary>
    Public Function Val(Str As Object) As Double
        Return BaseOps.Val(Str)
    End Function

#End Region

#Region "搜索"

    ''' <summary>
    ''' 获取搜索文本的相似度。
    ''' </summary>
    ''' <param name="Source">被搜索的长内容。</param>
    ''' <param name="Query">用户输入的搜索文本。</param>
    Private Function SearchSimilarity(Source As String, Query As String) As Double
        Return BaseSearch.Similarity(Source, Query)
    End Function
    ''' <summary>
    ''' 获取多段文本加权后的相似度。
    ''' </summary>
    Private Function SearchSimilarityWeighted(Source As List(Of SearchSource), Query As String) As Double
        Return BaseSearch.Weighted(Source, Query)
    End Function
    ''' <summary>
    ''' 用于搜索的项目。
    ''' </summary>
    Public Class SearchEntry(Of T)
        Implements PCLCS.ILegacyBaseSearchEntry(Of SearchSource)
        ''' <summary>
        ''' 该项目对应的源数据。
        ''' </summary>
        Public Item As T
        ''' <summary>
        ''' 该项目用于搜索的文本源。
        ''' 在搜索时，会对每个文本源单独加权，但单个文本源内的多个别名只取最高的一个的相似度。
        ''' </summary>
        Public SearchSource As List(Of SearchSource)
        ''' <summary>
        ''' 相似度。
        ''' </summary>
        Public Similarity As Double
        ''' <summary>
        ''' 是否完全匹配。
        ''' </summary>
        Public AbsoluteRight As Boolean
        Public Overrides Function ToString() As String
            Return BaseSearch.FormatEntry(Similarity, Item)
        End Function
        Private ReadOnly Property BridgeSources As List(Of SearchSource) Implements PCLCS.ILegacyBaseSearchEntry(Of SearchSource).Sources
            Get
                Return SearchSource
            End Get
        End Property
        Private Property BridgeSimilarity As Double Implements PCLCS.ILegacyBaseSearchEntry(Of SearchSource).Similarity
            Get
                Return Similarity
            End Get
            Set(Value As Double)
                Similarity = Value
            End Set
        End Property
        Private Property BridgeAbsoluteRight As Boolean Implements PCLCS.ILegacyBaseSearchEntry(Of SearchSource).AbsoluteRight
            Get
                Return AbsoluteRight
            End Get
            Set(Value As Boolean)
                AbsoluteRight = Value
            End Set
        End Property
    End Class
    ''' <summary>
    ''' 单个用于搜索的文本源。
    ''' </summary>
    Public Class SearchSource
        Implements PCLCS.ILegacyBaseSearchSource
        Public Aliases As String()
        Public Weight As Double
        Public Sub New(Aliases As String(), Optional Weight As Double = 1)
            Me.Aliases = Aliases
            Me.Weight = Weight
        End Sub
        Public Sub New(Text As String, Optional Weight As Double = 1)
            Me.Aliases = {Text}
            Me.Weight = Weight
        End Sub
        Private ReadOnly Property BridgeAliases As String() Implements PCLCS.ILegacyBaseSearchSource.Aliases
            Get
                Return Aliases
            End Get
        End Property
        Private ReadOnly Property BridgeWeight As Double Implements PCLCS.ILegacyBaseSearchSource.Weight
            Get
                Return Weight
            End Get
        End Property
    End Class
    ''' <summary>
    ''' 进行多段文本加权搜索，获取相似度较高的数项结果。
    ''' 在搜索时，会对每个文本源单独加权，但单个文本源内的多个别名只取最高的一个的相似度。
    ''' 这会修改 Entries 中每项的 Similarity 与 AbsoluteRight 字段。
    ''' </summary>
    ''' <param name="MaxBlurCount">返回的最大模糊结果数。</param>
    ''' <param name="MinBlurSimilarity">返回结果要求的最低相似度。</param>
    Public Function Search(Of T)(Entries As List(Of SearchEntry(Of T)), Query As String, Optional MaxBlurCount As Integer = 5, Optional MinBlurSimilarity As Double = 0.1) As List(Of SearchEntry(Of T))
        Return BaseSearch.Search(Of SearchEntry(Of T), SearchSource)(Entries, Query, MaxBlurCount, MinBlurSimilarity)
    End Function

#End Region

#Region "系统"

    ''' <summary>
    ''' 为 Task 设置超时，在超时时抛出 TimeoutException。
    ''' </summary>
    <Extension> Public Function GetResultWithTimeout(Of T)(TargetTask As Task(Of T), TokenSource As CancellationTokenSource, TimeoutMs As Integer) As T
        Return BaseOps.GetResultWithTimeout(TargetTask, TokenSource, TimeoutMs)
    End Function
    ''' <summary>
    ''' 为 Task 设置超时，在超时时抛出 TimeoutException。
    ''' </summary>
    <Extension> Public Sub GetResultWithTimeout(TargetTask As Task, TokenSource As CancellationTokenSource, TimeoutMs As Integer)
        BaseOps.GetResultWithTimeout(TargetTask, TokenSource, TimeoutMs)
    End Sub

    ''' <summary>
    ''' 可用于临时存放文件的，不含任何特殊字符的文件夹路径，以“\”结尾。
    ''' </summary>
    Public PathPure As New Lazy(Of String)(Function() BaseOps.PurePath(Function() PathTemp, Function() OsDrive))

    ''' <summary>
    ''' 指示接取到这个异常的函数进行重试。
    ''' </summary>
    Public Class RestartException
        Inherits Exception
    End Class

    ''' <summary>
    ''' 以管理员权限运行当前程序，并等待程序运行结束。
    ''' 返回程序的返回代码，如果运行失败将抛出异常。
    ''' </summary>
    Public Function RunAsAdmin(Argument As String) As Integer
        Return BaseOps.RunAsAdmin(Argument, PathExe)
    End Function

    Private Uuid As Integer = 1
    Private UuidLock As Object
    ''' <summary>
    ''' 获取一个全程序内不会重复的数字（伪 Uuid）。
    ''' </summary>
    Public Function GetUuid() As Integer
        Return BaseOps.GetUuid(Uuid, UuidLock)
    End Function

    '时间相关
    ''' <summary>
    ''' 获取一个单调递增时间值（毫秒）。
    ''' </summary>
    Public Function GetTimeMs() As Long
        Return BaseOps.GetTimeMs()
    End Function
    ''' <summary>
    ''' 获取十进制 Unix 时间戳。
    ''' </summary>
    Public Function GetUnixTimestampUtc() As Long
        Return BaseOps.GetUnixTimestampUtc()
    End Function
    ''' <summary>
    ''' 获取十进制 Unix 时间戳。
    ''' </summary>
    Public Function ToUnixTimestampUtc(Time As Date) As Long
        Return BaseOps.ToUnixTimestampUtc(Time)
    End Function
    ''' <summary>
    ''' 时间戳转化为日期。
    ''' </summary>
    Public Function GetDate(timeStamp As Integer) As Date
        Return BaseOps.GetDate(timeStamp)
    End Function
    ''' <summary>
    ''' 将 UTC 时间转化为当前时区的时间。
    ''' </summary>
    Public Function GetLocalTime(UtcDate As Date) As Date
        Return BaseOps.GetLocalTime(UtcDate)
    End Function

    ''' <summary>
    ''' 用于储存 RaiseByMouse 的 EventArgs。
    ''' </summary>
    Public NotInheritable Class RouteEventArgs
        Inherits EventArgs
        Public RaiseByMouse As Boolean
        Public Handled As Boolean = False
        Public Sub New(Optional RaiseByMouse As Boolean = False)
            Me.RaiseByMouse = RaiseByMouse
        End Sub
    End Class

    ''' <summary>
    ''' 启动进程并返回进程实例，若失败则抛出异常。
    ''' 会自动进行长路径处理。
    ''' </summary>
    Public Function StartProcess(FileName As String, Optional Arguments As String = "") As Process
        Return BaseOps.StartProcess(FileName, Arguments)
    End Function
    ''' <summary>
    ''' 启动进程并返回进程实例，若失败则抛出异常。
    ''' 会自动进行长路径处理。
    ''' </summary>
    Public Function StartProcess(StartInfo As ProcessStartInfo) As Process
        Return BaseOps.StartProcess(StartInfo)
    End Function

    ''' <summary>
    ''' 在新的工作线程中执行代码。
    ''' </summary>
    Public Function RunInNewThread(Action As Action, Optional Name As String = Nothing, Optional Priority As ThreadPriority = ThreadPriority.Normal) As Thread
        Return BaseOps.RunInNewThread(Action, Name, Priority, AddressOf GetUuid)
    End Function
    ''' <summary>
    ''' 确保在 UI 线程中执行代码。
    ''' 如果当前并非 UI 线程，则会阻断当前线程，直至 UI 线程执行完毕。
    ''' 为防止线程互锁，请仅在开始加载动画、从 UI 获取输入时使用！
    ''' </summary>
    Public Function RunInUiWait(Of Output)(Action As Func(Of Output)) As Output
        Return BaseUi.RunInUiWait(Action, UiThreadId)
    End Function
    ''' <summary>
    ''' 确保在 UI 线程中执行代码。
    ''' 如果当前并非 UI 线程，则会阻断当前线程，直至 UI 线程执行完毕。
    ''' 为防止线程互锁，请仅在开始加载动画、从 UI 获取输入时使用！
    ''' </summary>
    Public Sub RunInUiWait(Action As Action)
        BaseUi.RunInUiWait(Action, UiThreadId)
    End Sub
    ''' <summary>
    ''' 确保在 UI 线程中执行代码，代码按触发顺序执行。
    ''' 如果当前并非 UI 线程，也不阻断当前线程的执行。
    ''' </summary>
    Public Sub RunInUi(Action As Action, Optional ForceWaitUntilLoaded As Boolean = False)
        BaseUi.RunInUi(Action, ForceWaitUntilLoaded, UiThreadId)
    End Sub
    ''' <summary>
    ''' 确保在工作线程中执行代码。
    ''' </summary>
    Public Sub RunInThread(Action As Action)
        BaseOps.RunInThread(Action, Function() RunInUi(), AddressOf GetUuid)
    End Sub

    ''' <summary>
    ''' 按照既定的函数进行选择排序。
    ''' 返回第一个对象是否应该排在前面（a > b）。
    ''' </summary>
    <Extension> Public Function SortByComparison(Of T)(List As IList(Of T), SortRule As Func(Of T, T, Boolean)) As List(Of T)
        Return BaseOps.SortByComparison(List, SortRule)
    End Function

    ''' <summary>
    ''' 获取程序启动参数。
    ''' </summary>
    ''' <param name="Name">参数名。</param>
    ''' <param name="DefaultValue">默认值。</param>
    Public Function GetProgramArgument(Name As String, Optional DefaultValue As Object = "") As Object
        Return BaseOps.GetProgramArgument(Name, DefaultValue)
    End Function

    ''' <summary>
    ''' 打开网页。
    ''' </summary>
    Public Sub OpenWebsite(Url As String)
        BaseOps.OpenWebsite(Url, CreateBaseHost(), AddressOf GetUuid)
    End Sub
    ''' <summary>
    ''' 打开 explorer。
    ''' 若不以 \ 结尾，则将视作文件路径，打开并选中此文件。
    ''' </summary>
    Public Sub OpenExplorer(Location As String)
        BaseOps.OpenExplorer(Location)
    End Sub

    ''' <summary>
    ''' 设置剪贴板。将在另一线程运行，且不会抛出异常。
    ''' </summary>
    Public Sub ClipboardSet(Text As String, Optional ShowSuccessHint As Boolean = True)
        BaseOps.ClipboardSet(Text, ShowSuccessHint, CreateBaseHost(), AddressOf GetUuid)
    End Sub
    ''' <summary>
    ''' 获取剪贴板文本。将在 UI 线程运行，且不会抛出异常。
    ''' </summary>
    Public Function ClipboardGetText() As String
        Return BaseOps.ClipboardGetText(CreateBaseHost())
    End Function

    ''' <summary>
    ''' 将程序中的资源释放到文件。
    ''' </summary>
    Public Sub ExtractResources(FilePath As String, ResourceName As String)
        BaseOps.ExtractResources(FilePath, ResourceName, Function(Name) My.Resources.ResourceManager.GetObject(Name))
    End Sub

#End Region

#Region "UI"

    'DPI 转换
    Public ReadOnly DPI As Integer = System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiX
    ''' <summary>
    ''' 将经过 DPI 缩放的 WPF 尺寸转化为实际的像素尺寸。
    ''' </summary>
    Public Function GetPixelSize(WPFSize As Double) As Double
        Return BaseUi.GetPixelSize(WPFSize, DPI)
    End Function
    ''' <summary>
    ''' 将实际的像素尺寸转化为经过 DPI 缩放的 WPF 尺寸。
    ''' </summary>
    Public Function GetWPFSize(PixelSize As Double) As Double
        Return BaseUi.GetWpfSize(PixelSize, DPI)
    End Function

    ''' <summary>
    ''' 将 XML 转换为对应 UI 对象。
    ''' 注意：性能较差，不应大量使用。
    ''' </summary>
    Public Function GetObjectFromXML(Str As XElement) As Object
        Return BaseXaml.Load(Str, GetType(MyBitmap).Assembly.GetName().Name, AddressOf MyBitmap.NormalizeResourceUri, GetType(SettingService))
    End Function
    ''' <summary>
    ''' 将 XML 转换为对应 UI 对象。
    ''' 注意：性能较差，不应大量使用。
    ''' </summary>
    Public Function GetObjectFromXML(Str As String) As Object
        Return BaseXaml.Load(Str, GetType(MyBitmap).Assembly.GetName().Name, AddressOf MyBitmap.NormalizeResourceUri, GetType(SettingService))
    End Function

    Private ReadOnly UiThreadId As Integer = Thread.CurrentThread.ManagedThreadId
    ''' <summary>
    ''' 当前线程是否为主线程。
    ''' </summary>
    Public Function RunInUi() As Boolean
        Return BaseUi.RunInUi(UiThreadId)
    End Function

    ''' <summary>
    ''' 检查某个控件是否位于主窗口可视区域内，且控件本身可见。
    ''' </summary>
    <Extension> Public Function IsVisibleInForm(element As FrameworkElement) As Boolean
        Return BaseUi.IsVisibleInForm(element, FrmMain)
    End Function

    ''' <summary>
    ''' 控件是否受到 TextTrimming 属性影响，导致内容被截取。
    ''' </summary>
    <Extension> Public Function IsTextTrimmed(TextBlock As TextBlock) As Boolean
        Return BaseUi.IsTextTrimmed(TextBlock, DPI)
    End Function

    ''' <summary>
    ''' 将布尔值转换为 Visibility。True 转换为 Visible，False 转换为 Collapsed。
    ''' </summary>
    <Extension> Public Function ToVisibility(IsVisible As Boolean) As Visibility
        Return BaseUi.ToVisibility(IsVisible)
    End Function

#End Region

#Region "随机"

    Private ReadOnly Random As New Random

    ''' <summary>
    ''' 随机选择其一。
    ''' </summary>
    Public Function RandomOne(Of T)(objects As ICollection(Of T)) As T
        Return BaseOps.RandomOne(objects, AddressOf RandomInteger)
    End Function

    ''' <summary>
    ''' 取随机整数（包含）。
    ''' </summary>
    Public Function RandomInteger(min As Integer, max As Integer) As Integer
        Return BaseOps.RandomInteger(min, max, Random)
    End Function

    ''' <summary>
    ''' 将数组随机打乱。
    ''' </summary>
    <Extension> Public Function Shuffle(Of T)(Raw As IEnumerable(Of T)) As IEnumerable(Of T)
        Return BaseOps.Shuffle(Raw, AddressOf RandomInteger)
    End Function

#End Region

    'Only the real platform primitives. Algorithms, retries and threading stay in C#.
    Private Function CreateBaseHost() As PCLCS.LegacyBaseHost
        Return New PCLCS.LegacyBaseHost With {
            .OnUiThread = Function() RunInUi(), .DispatchWait = Sub(Work) RunInUiWait(Work),
            .ClearClipboard = Sub() My.Computer.Clipboard.Clear(),
            .WriteClipboardText = Sub(Text) My.Computer.Clipboard.SetText(Text),
            .ClipboardContainsText = Function() My.Computer.Clipboard.ContainsText(),
            .ReadClipboardText = Function() My.Computer.Clipboard.GetText(),
            .SuccessHint = Sub(Text) Hint(Text, HintType.Green),
            .Message = Sub(Text, Title) MyMsgBox(Text, Title)
        }
    End Function

End Module
