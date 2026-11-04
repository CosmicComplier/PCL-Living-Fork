Imports PCLLF.Presentation.Accounts

Public Class MySkin
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Loader As LoaderTask(Of (String, String), String)
    Private ReadOnly Native As LegacySkinControl(Of McLoginLegacy, McLoginMs, McLoginServer, LoaderTask(Of (String, String), String))
    Public Sub New()
        Native = New LegacySkinControl(Of McLoginLegacy, McLoginMs, McLoginServer, LoaderTask(Of (String, String), String))(LoginPageRuntime.Skins,
            New LegacySkinView(Of LoaderTask(Of (String, String), String)) With {
                .Loader = Function() Loader, .Control = Function() Me, .Scale = Function() CType(RenderTransform, ScaleTransform).ScaleX,
                .ShadowOpacity = Function() ShadowSkin.Opacity,
                .AnimateShadow = Sub(Value, Time, Delay) AniStart(AaOpacity(ShadowSkin, Value, Time, Delay), "Skin Shadow"),
                .SetToolTip = Sub(Value) ToolTip = Value, .SetBackTag = Sub(Value) ImgBack.Tag = Value,
                .SetFore = Sub(Value) ImgFore.Source = Value, .SetBack = Sub(Value) ImgBack.Source = Value,
                .CapeVisibility = Function() BtnSkinCape.Visibility, .SetCapeVisibility = Sub(Value) BtnSkinCape.Visibility = Value,
                .SetSaveEnabled = Sub(Value) BtnSkinSave.IsEnabled = Value, .Click = AddressOf RaiseSkinClick})
        InitializeComponent()
    End Sub
    Private Sub RaiseSkinClick(sender As Object, e As MouseButtonEventArgs)
        RaiseEvent Click(sender, e)
    End Sub
    Public Property Address As String
        Get
            Return Native.Address
        End Get
        Set(value As String)
            Native.Address = value
        End Set
    End Property
    Public Property HasCape As Boolean
        Get
            Return Native.HasCape
        End Get
        Set(value As Boolean)
            Native.HasCape = value
        End Set
    End Property
    Private Sub PanSkin_MouseEnter(sender As Object, e As MouseEventArgs) Handles Me.MouseEnter
        Native.MouseEnter()
    End Sub
    Private Sub PanSkin_MouseLeave(sender As Object, e As MouseEventArgs) Handles Me.MouseLeave
        Native.MouseLeave()
    End Sub
    Private Sub PanSkin_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        Native.MouseDown()
    End Sub
    Private Sub PanSkin_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        Native.MouseUp(sender, e)
    End Sub
    Public Sub BtnSkinSave_Click() Handles BtnSkinSave.Click
        Native.Save()
    End Sub
    Public Shared Sub Save(Loader As LoaderTask(Of (String, String), String))
        LoginPageRuntime.Skins.Save(Loader)
    End Sub
    Private Sub BtnSkinSave_Checked(sender As MyMenuItem, e As RoutedEventArgs) Handles BtnSkinSave.Checked
        Native.SaveChecked()
    End Sub
    Public Sub Load()
        Native.Load()
    End Sub
    Public Sub Clear()
        Native.Clear()
    End Sub
    Public Sub RefreshClick() Handles BtnSkinRefresh.Click
        Native.RefreshClick()
    End Sub
    Public Shared Sub RefreshCache(Optional sender As LoaderTask(Of (String, String), String) = Nothing)
        LoginPageRuntime.Skins.RefreshCache(sender)
    End Sub
    Public Shared Sub ReloadCache(SkinAddress As String)
        LoginPageRuntime.Skins.ReloadCache(SkinAddress)
    End Sub
    Public Sub BtnSkinCape_Click() Handles BtnSkinCape.Click
        Native.ChangeCape()
    End Sub
End Class
