'LF 动画模块的原调用面；调度、帧执行、工厂及缓动计算均由 C# 呈现程序集实现。
Imports System.Collections
Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.CompilerServices
Imports PCLLF.Presentation.Animation
Imports PCLLF.Presentation.Controls
Imports CoreAnimationEase = PCLLF.Presentation.Animation.AnimationEase

Public Module ModAnimation

    '这些字段是原调用者与 C# 引擎共同使用的实际状态，不复制字典、动画或组列表。
    Public AniGroups As New Dictionary(Of String, AniGroupEntry)
    Private AniLastTick As Long
    Public AniRunning As Boolean = False
    Public AniFPS As Integer = 0
    Private ReadOnly LegacySharedService As New AnimationEngine(Of AniData, AniGroupEntry)(
        CreateHost(), Function() AniGroups,
        Function(Data, Tick) New AniGroupEntry With {.Data = Data, .StartTick = Tick})
    Private ReadOnly Factories As New AnimationFactories(Of AniData)(LegacySharedService.Host)

    Public ReadOnly Property AniSpeed As Double
        Get
            Return LegacySharedService.AniSpeed
        End Get
    End Property
    Public Property AniControlEnabled() As Integer
        Get
            Return LegacySharedService.AniControlEnabled
        End Get
        Set(value As Integer)
            LegacySharedService.AniControlEnabled = value
        End Set
    End Property

    Public Class AniGroupEntry
        Implements IAnimationGroup(Of AniData)

        Public Data As List(Of AniData)
        Public StartTick As Long
        Public Uuid As Integer = GetUuid()

        Private Property CoreFrames As List(Of AniData) Implements IAnimationGroup(Of AniData).Frames
            Get
                Return Me.Data
            End Get
            Set(value As List(Of AniData))
                Me.Data = value
            End Set
        End Property
        Private Property CoreTick As Long Implements IAnimationGroup(Of AniData).Tick
            Get
                Return Me.StartTick
            End Get
            Set(value As Long)
                Me.StartTick = value
            End Set
        End Property
        Private Property CoreId As Integer Implements IAnimationGroup(Of AniData).Id
            Get
                Return Me.Uuid
            End Get
            Set(value As Integer)
                Me.Uuid = value
            End Set
        End Property
    End Class

    Public Structure AniData
        Implements IAnimationData

        Public TypeMain As AniType
        Public TypeSub As AniTypeSub
        Public TimeTotal As Integer
        Public TimeFinished As Double
        Public TimePercent As Double
        Public IsAfter As Boolean
        Public Ease As AniEase
        Public Obj As Object
        Public Value As Object
        Public ValueLast As Object

        Private Property CoreKind As AnimationKind Implements IAnimationData.Kind
            Get
                Return CType(CInt(Me.TypeMain), AnimationKind)
            End Get
            Set(value As AnimationKind)
                Me.TypeMain = CType(CInt(value), AniType)
            End Set
        End Property
        Private Property CoreSubKind As AnimationSubKind Implements IAnimationData.SubKind
            Get
                Return CType(CInt(Me.TypeSub), AnimationSubKind)
            End Get
            Set(value As AnimationSubKind)
                Me.TypeSub = CType(CInt(value), AniTypeSub)
            End Set
        End Property
        Private Property CoreDuration As Integer Implements IAnimationData.Duration
            Get
                Return Me.TimeTotal
            End Get
            Set(value As Integer)
                Me.TimeTotal = value
            End Set
        End Property
        Private Property CoreElapsed As Double Implements IAnimationData.Elapsed
            Get
                Return Me.TimeFinished
            End Get
            Set(value As Double)
                Me.TimeFinished = value
            End Set
        End Property
        Private Property CorePercent As Double Implements IAnimationData.Percent
            Get
                Return Me.TimePercent
            End Get
            Set(value As Double)
                Me.TimePercent = value
            End Set
        End Property
        Private Property CoreAfter As Boolean Implements IAnimationData.After
            Get
                Return Me.IsAfter
            End Get
            Set(value As Boolean)
                Me.IsAfter = value
            End Set
        End Property
        Private Property CoreEasing As CoreAnimationEase Implements IAnimationData.Easing
            Get
                Return Me.Ease
            End Get
            Set(value As CoreAnimationEase)
                Me.Ease = CType(value, AniEase)
            End Set
        End Property
        Private Property CoreTarget As Object Implements IAnimationData.Target
            Get
                Return Me.Obj
            End Get
            Set(value As Object)
                Me.Obj = value
            End Set
        End Property
        Private Property CoreValue As Object Implements IAnimationData.Value
            Get
                Return Me.Value
            End Get
            Set(value As Object)
                Me.Value = value
            End Set
        End Property
        Private Property CoreRemainder As Object Implements IAnimationData.Remainder
            Get
                Return Me.ValueLast
            End Get
            Set(value As Object)
                Me.ValueLast = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Return TypeMain.ToString() & " | " & TimeFinished & "/" & TimeTotal & "(" & Math.Round(TimePercent * 100) & "%)" & If(Obj Is Nothing, "", " | " & Obj.ToString & "(" & Obj.GetType.Name & ")")
        End Function
    End Structure

    Public Enum AniType
        Number
        Color
        Scale
        TextAppear
        Code
        ScaleTransform
        RotateTransform
    End Enum
    Public Enum AniTypeSub
        X
        Y
        Width
        Height
        Opacity
        Value
        Radius
        BorderThickness
        StrokeThickness
        TranslateX
        TranslateY
        [Double]
        DoubleParam
        GridLengthWidth
    End Enum

    Public Function AaX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaX(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaY(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaWidth(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaHeight(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaHeight(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaOpacity(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaOpacity(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaValue(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaValue(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaRadius(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaRadius(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaBorderThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaBorderThickness(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaStrokeThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaStrokeThickness(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaGridLengthWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaGridLengthWidth(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaDouble(Obj As Object, Prop As DependencyProperty, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaDouble(Obj, Prop, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaDouble(Lambda As ParameterizedThreadStart, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaDouble(Lambda, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaColor(Obj As FrameworkElement, Prop As DependencyProperty, Value As MyColor, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaColor(Obj, Prop, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaColor(Obj As FrameworkElement, Prop As DependencyProperty, Res As String, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaColor(Obj, Prop, Res, Time, Delay, Ease, After)
    End Function

    Public Function AaScale(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False, Optional Absolute As Boolean = False) As AniData
        Return Factories.AaScale(Obj, Value, Time, Delay, Ease, After, Absolute)
    End Function

    Public Function AaTextAppear(Obj As Object, Optional Hide As Boolean = False, Optional TimePerText As Boolean = True, Optional Time As Integer = 70, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaTextAppear(Obj, Hide, TimePerText, Time, Delay, Ease, After)
    End Function

    Public Function AaCode(Code As ThreadStart, Optional Delay As Integer = 0, Optional After As Boolean = False) As AniData
        Return Factories.AaCode(Code, Delay, After)
    End Function

    Public Function AaScaleTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaScaleTransform(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaRotateTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaRotateTransform(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaTranslateX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaTranslateX(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaTranslateY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return Factories.AaTranslateY(Obj, Value, Time, Delay, Ease, After)
    End Function

    Public Function AaStack(Stack As StackPanel, Optional Time As Integer = 100, Optional Delay As Integer = 25) As List(Of AniData)
        Return Factories.AaStack(Stack, Time, Delay)
    End Function

    Public Enum AniEasePower As Integer
        Weak = 2
        Middle = 3
        Strong = 4
        ExtraStrong = 5
    End Enum
    Public MustInherit Class AniEase
        Inherits CoreAnimationEase

        Public MustOverride Overrides Function GetValue(t As Double) As Double
        Public Overrides Function GetDelta(t1 As Double, t0 As Double) As Double
            Return MyBase.GetDelta(t1, t0)
        End Function
    End Class
    Public Class AniEaseInout
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInout
        Public Sub New(EaseIn As AniEase, EaseOut As AniEase, Optional EaseInPercent As Double = 0.5)
            Core = New AnimationEaseInout(EaseIn, EaseOut, EaseInPercent)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseLinear
        Inherits AniEase
        Private ReadOnly Core As New AnimationEaseLinear
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
        Public Overrides Function GetDelta(t1 As Double, t0 As Double) As Double
            Return Core.GetDelta(t1, t0)
        End Function
    End Class
    Public Class AniEaseInFluent
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInFluent
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseInFluent(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutFluent
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseOutFluent
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseOutFluent(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseInoutFluent
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInoutFluent
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle, Optional Middle As Double = 0.5)
            Core = New AnimationEaseInoutFluent(CType(CInt(Power), AnimationEasePower), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutFluentWithInitial
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseOutFluentWithInitial
        Public Sub New(InitialPixelPerSecond As Double, TotalSecond As Double, TotalDistance As Double)
            Core = New AnimationEaseOutFluentWithInitial(InitialPixelPerSecond, TotalSecond, TotalDistance)
        End Sub
        Public Overrides Function GetValue(percent As Double) As Double
            Return Core.GetValue(percent)
        End Function
    End Class
    Public Class AniEaseInBack
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInBack
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseInBack(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutBack
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseOutBack
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseOutBack(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseInCar
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInCar
        Public Sub New(Optional Middle As Double = 0.7, Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseInCar(Middle, CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutCar
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseOutCar
        Public Sub New(Optional Middle As Double = 0.3, Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseOutCar(Middle, CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseInElastic
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseInElastic
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseInElastic(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutElastic
        Inherits AniEase
        Private ReadOnly Core As AnimationEaseOutElastic
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            Core = New AnimationEaseOutElastic(CType(CInt(Power), AnimationEasePower))
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Core.GetValue(t)
        End Function
    End Class

    Public Sub AniStart(AniGroup As IList, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        LegacySharedService.AniStart(AniGroup, Name, RefreshTime)
    End Sub
    Public Sub AniStart(AniGroup As AniData, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        LegacySharedService.AniStart(AniGroup, Name, RefreshTime)
    End Sub
    Public Sub AniStop(Name As String)
        LegacySharedService.AniStop(Name)
    End Sub
    Public Function AniIsRun(Name As String) As Boolean
        Return LegacySharedService.AniIsRun(Name)
    End Function
    Public Sub AniStart()
        LegacySharedService.AniStart()
    End Sub
    Public Sub AniTimer(DeltaTick As Double)
        LegacySharedService.AniTimer(DeltaTick)
    End Sub

    '平台原语及兼容数据装箱；C# 不引用启动器主程序集。
    Private Function CreateHost() As AnimationHost
        Return New AnimationHost With {
            .Clock = AddressOf GetTimeMs,
            .ReadLastTick = Function() AniLastTick,
            .WriteLastTick = Sub(Value) AniLastTick = Value,
            .ReadRunning = Function() AniRunning,
            .WriteRunning = Sub(Value) AniRunning = Value,
            .ReadFps = Function() AniFPS,
            .WriteFps = Sub(Value) AniFPS = Value,
            .AnimationSetting = Function() Settings.Get(Of Integer)("SystemDebugAnim"),
            .DebugMode = Function() ModeDebug,
            .DownloadRemaining = Function() NetManager.FileRemain,
            .UiWait = Sub(Work) RunInUiWait(Work),
            .StartThread = Sub(Work, Name, Priority) RunInNewThread(Work, Name, Priority),
            .Sleep = Sub(Time) Thread.Sleep(Time),
            .Info = Sub(Message) Logger.Info(Message),
            .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast),
            .Fatal = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.AlertThenCrash),
            .ReportDiagnostics = AddressOf ReportDiagnostics,
            .NewUuid = AddressOf GetUuid,
            .RandomInteger = Function(Minimum, Maximum) RandomInteger(Minimum, Maximum),
            .DecodeText = Function(Bytes) Encoding.GetEncoding("GB18030").GetString(Bytes),
            .Radix86 = Function(Value) Value.ConvertRadix(86),
            .NewLinearEasing = Function() New AniEaseLinear,
            .FindResource = Function(Key) Application.Current.FindResource(Key),
            .Number = Function(Value) Conversions.ToDouble(Value),
            .GetProperty = Function(Target, Name) CallByName(Target, Name, CallType.Get),
            .SetProperty = Sub(Target, Name, Value) CallByName(Target, Name, CallType.Let, Value),
            .ReadColor = AddressOf ReadAnimationColor,
            .BoxColor = Function(Value) New MyColor(Value.A, Value.R, Value.G, Value.B),
            .ReadRect = AddressOf ReadAnimationRect,
            .BoxRect = Function(Value) New MyRect(Value.Left, Value.Top, Value.Width, Value.Height)
        }
    End Function

    Private Sub ReportDiagnostics(Fps As Integer, Count As Integer)
        Logger.Info($"FPS {Fps}, 动画 {Count}, 下载中 {NetManager.FileRemain}（{StringUtils.FormatByteSize(NetManager.Speed)}/s）")
        If NetManager.FileRemain < 5 Then
            For Each File In NetManager.AllFiles.Values
                If File.State < NetState.Finished Then Logger.Trace($"- {File.State} {StringUtils.FormatByteSize(File.Speed)}/s {File.LocalPath}")
            Next
        End If
    End Sub
    Private Function ReadAnimationColor(Value As Object) As ControlColor
        Dim Color As New MyColor(Value)
        Return New ControlColor(Color.A, Color.R, Color.G, Color.B)
    End Function
    Private Function ReadAnimationRect(Value As Object) As AnimationRect
        If TypeOf Value Is MyRect Then
            Dim Rect = DirectCast(Value, MyRect)
            Return New AnimationRect(Rect.Left, Rect.Top, Rect.Width, Rect.Height)
        End If
        Return New AnimationRect(
            Conversions.ToDouble(CallByName(Value, "Left", CallType.Get)),
            Conversions.ToDouble(CallByName(Value, "Top", CallType.Get)),
            Conversions.ToDouble(CallByName(Value, "Width", CallType.Get)),
            Conversions.ToDouble(CallByName(Value, "Height", CallType.Get)))
    End Function
End Module
