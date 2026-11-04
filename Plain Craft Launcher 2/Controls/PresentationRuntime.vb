Imports PCLLF.Presentation.Controls

'仅适配 LF 平台服务；控件状态、鼠标流程和动画计划均在 C# 呈现程序集。
Friend NotInheritable Class PresentationRuntime
    Private Shared ReadOnly Gate As New Object
    Private Shared Configured As Boolean

    Friend Shared Sub EnsureConfigured()
        SyncLock Gate
            If Configured Then Return
            Dim Animations As New ControlAnimationHost(Of AniData) With {
                .CreateColorResource = Function(Target, Prop, Key, Spec) AaColor(Target, Prop, Key, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateColorTo = Function(Target, Prop, Value, Spec) AaColor(Target, Prop, New MyColor(Value.A, Value.R, Value.G, Value.B) - CType(Target.GetValue(Prop), Brush), Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateScale = Function(Target, Value, Spec, Absolute) AaScale(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After, Absolute),
                .CreateScaleTransform = Function(Target, Value, Spec) AaScaleTransform(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .ReadSpeed = Function() AniSpeed,
                .CreateWidth = Function(Target, Value, Spec) AaWidth(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateTranslateX = Function(Target, Value, Spec) AaTranslateX(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateTranslateY = Function(Target, Value, Spec) AaTranslateY(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateX = Function(Target, Value, Spec) AaX(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateY = Function(Target, Value, Spec) AaY(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateHeight = Function(Target, Value, Spec) AaHeight(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateOpacity = Function(Target, Value, Spec) AaOpacity(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateRotateTransform = Function(Target, Value, Spec) AaRotateTransform(Target, Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateDouble = Function(Work, Value, Spec) AaDouble(Sub(Delta) Work(CDbl(Delta)), Value, Spec.Duration, Spec.Delay, ToEase(Spec), Spec.After),
                .CreateCode = Function(Work, Delay, After) AaCode(Sub() Work(), Delay, After),
                .StartFrames = Sub(Frames, Name, RefreshTime) AniStart(Frames, Name, RefreshTime),
                .StopFrames = Sub(Name) AniStop(Name)
            }
            ControlServices.Configure(New ControlServiceHost With {
                .NewUuid = AddressOf GetUuid,
                .AnimationEnabled = Function() AniControlEnabled = 0,
                .ThemeColor = AddressOf ReadThemeColor,
                .Info = Sub(Message) Logger.Info(Message),
                .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
                .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast),
                .RaiseCustomEvent = Sub(Control) Control.RaiseCustomEvent(),
                .Animations = Animations
            })
            Configured = True
        End SyncLock
    End Sub

    Private Shared Function ReadThemeColor(Kind As ControlThemeColor) As ControlColor
        Dim Value As MyColor
        Select Case Kind
            Case ControlThemeColor.Gray4
                Value = ColorGray4
            Case ControlThemeColor.Gray5
                Value = ColorGray5
            Case ControlThemeColor.Accent8
                Value = Color8
            Case ControlThemeColor.SemiTransparent
                Value = ColorSemiTransparent
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
        Return New ControlColor(Value.A, Value.R, Value.G, Value.B)
    End Function

    Private Shared Function ToEase(Spec As ControlAnimationSpec) As AniEase
        Dim Power = If(Spec.RawPower.HasValue, CType(Spec.RawPower.GetValueOrDefault(), AniEasePower), ToPower(Spec.Power))
        Select Case Spec.Ease
            Case ControlEase.Linear
                Return New AniEaseLinear
            Case ControlEase.OutFluent
                Return New AniEaseOutFluent(Power)
            Case ControlEase.InFluent
                Return New AniEaseInFluent(Power)
            Case ControlEase.OutBack
                Return New AniEaseOutBack(Power)
            Case ControlEase.InoutFluent
                Return New AniEaseInoutFluent(Power, Spec.Middle)
            Case ControlEase.InBack
                Return New AniEaseInBack(Power)
            Case ControlEase.OutElastic
                Return New AniEaseOutElastic(Power)
            Case ControlEase.OutFluentWithInitial
                Return New AniEaseOutFluentWithInitial(Spec.InitialSpeed, Spec.TotalSeconds, Spec.TotalDistance)
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(Spec))
        End Select
    End Function
    Private Shared Function ToPower(Power As ControlEasePower) As AniEasePower
        Select Case Power
            Case ControlEasePower.Weak
                Return AniEasePower.Weak
            Case ControlEasePower.Middle
                Return AniEasePower.Middle
            Case ControlEasePower.Strong
                Return AniEasePower.Strong
            Case ControlEasePower.ExtraStrong
                Return AniEasePower.ExtraStrong
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(Power))
        End Select
    End Function
End Class
