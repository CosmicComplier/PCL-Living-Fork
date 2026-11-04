Public Class PageSetupSystem
    Private RefreshingUpdateChannel As Boolean = True
    Friend ConfirmPrereleaseUpdates As Func(Of Boolean) = Function() MyMsgBox(
        "预发布版本可能存在尚未发现的问题，稳定性不及正式发布版。" & vbCrLf & vbCrLf & "启用后将同时接收正式发布版和预发布版的启动器更新。是否继续？",
        "接收预发布版更新", "启用预发布更新", "取消", IsWarn:=True) = 1

    Private Sub InitLauncherUpdateSource() Handles Me.Initialized
        AniControlEnabled += 1
        Try
            SettingService.RefreshSettings(ComboLauncherUpdateSource)
            RefreshUpdateChannel()
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub

    Private Sub RefreshUpdateChannel()
        RefreshingUpdateChannel = True
        Try
            ComboLauncherUpdateChannel.SelectedIndex = CInt(ModLauncherUpdate.PreferredUpdateChannel())
        Finally
            RefreshingUpdateChannel = False
        End Try
    End Sub
    Private Sub UpdateChannelChanged() Handles ComboLauncherUpdateChannel.SelectionChanged
        If RefreshingUpdateChannel OrElse AniControlEnabled <> 0 OrElse ComboLauncherUpdateChannel.SelectedIndex < 0 Then Return
        Try
            If Not ModLauncherUpdate.ChangeUpdateChannel(ComboLauncherUpdateChannel.SelectedIndex, ConfirmPrereleaseUpdates) Then RefreshUpdateChannel()
        Catch ex As Exception
            RefreshUpdateChannel()
            Logger.Error(ex, "切换启动器更新通道失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()
        RefreshModpackSwitch()
        LabLauncherVersion.Text = LauncherDisplayName & " " & VersionBaseName

        '非重复加载部分
        Static IsLoaded As Boolean = False
        If IsLoaded Then Return
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()
        SettingService.RefreshSettings(Me)
        RefreshUpdateChannel()
        RefreshModpackSwitch()
        LabLauncherVersion.Text = LauncherDisplayName & " " & VersionBaseName
    End Sub
    Friend Sub RefreshModpackSwitch()
        CheckModpackMode.Checked = ModModpackMode.Enabled
        CheckModpackMode.Visibility = If(ModModpackMode.CanEnable, Visibility.Visible, Visibility.Collapsed)
        LabModpackMode.Visibility = CheckModpackMode.Visibility
    End Sub
    Private Sub ModpackModeChanged(sender As Object, User As Boolean) Handles CheckModpackMode.Change
        ModModpackMode.Toggle(CheckModpackMode, User)
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Settings.Reset("ToolLauncherUpdateChannel")
            Logger.Info("已初始化启动器页设置")
            Hint("已初始化启动器页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化启动器页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    '滑动条
    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) v + 1
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Select Case v
                Case Is <= 14
                    Return (v + 1) * 0.1 & " M/s"
                Case Is <= 31
                    Return (v - 11) * 0.5 & " M/s"
                Case Is <= 41
                    Return (v - 21) & " M/s"
                Case Else
                    Return "无限制"
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, "关闭", (v / 10 + 0.1) & "x")
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Return
        If Not Settings.Get(Of Boolean)("HintDownloadThread") Then
            Settings.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    '调试模式
    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！",, False)
    End Sub

    '检查本项目更新；公告偏好继续保留，供将来接入公告源。
    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        UpdateCheckByButton()
    End Sub
#Region "导出 / 导入设置"

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Hint("该功能尚在开发中！")
    End Sub
    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        Hint("该功能尚在开发中！")
    End Sub

#End Region

End Class
