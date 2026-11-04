Public Class PageInstanceModDisabled
    Private ReadOnly Property Controller As PCLCS.InstanceModDisabledController
        Get
            Return New PCLCS.InstanceModDisabledController With {
                .Download = BtnDownload, .Version = BtnVersion,
                .Modpack = Function() ModModpackMode.Enabled,
                .DownloadHidden = Function() Settings.Get(Of Boolean)("UiHiddenPageDownload"), .HiddenForceShow = Function() PageSetupUI.HiddenForceShow,
                .SelectShowHidden = Function() If(FrmSelectRight Is Nothing, False, FrmSelectRight.ShowHidden),
                .DownloadPage = Sub() FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall),
                .LaunchPage = Sub() FrmMain.PageChange(FormMain.PageType.Launch), .SelectPage = Sub() FrmMain.PageChange(FormMain.PageType.InstanceSelect),
                .Resize = Sub() PanMain.TriggerForceResize()}
        End Get
    End Property
    Private Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles BtnDownload.Click
        Controller.OpenDownload()
    End Sub
    Private Sub BtnVersion_Click(sender As Object, e As EventArgs) Handles BtnVersion.Click
        Controller.OpenVersions()
    End Sub
    Public Sub BtnDownload_Loaded() Handles BtnDownload.Loaded
        Controller.Loaded()
    End Sub
End Class
