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
        Select Case Spec.Ease
            Case ControlEase.Linear
                Return New AniEaseLinear
            Case ControlEase.OutFluent
                Return New AniEaseOutFluent(ToPower(Spec.Power))
            Case ControlEase.InFluent
                Return New AniEaseInFluent(ToPower(Spec.Power))
            Case ControlEase.OutBack
                Return New AniEaseOutBack(ToPower(Spec.Power))
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
