Imports PCLCS

'原附加属性 owner 与原接口身份；遍历/保存次序由 C# controller 承接。
Public Class SettingService
    Public Shared ReadOnly KeyProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Key", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))
    Public Shared ReadOnly ValueProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Value", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))

    Public Shared Function GetKey(obj As DependencyObject) As String
        Return LegacySettingsBindingController.GetAttached(obj, KeyProperty)
    End Function
    Public Shared Sub SetKey(obj As DependencyObject, value As String)
        LegacySettingsBindingController.SetAttached(obj, KeyProperty, value)
    End Sub
    Public Shared Function GetValue(obj As DependencyObject) As String
        Return LegacySettingsBindingController.GetAttached(obj, ValueProperty)
    End Function
    Public Shared Sub SetValue(obj As DependencyObject, value As String)
        LegacySettingsBindingController.SetAttached(obj, ValueProperty, value)
    End Sub
    Public Shared Sub ResetSettings(Target As DependencyObject)
        LegacySettingsBindingController.ResetSettings(Target, KeyProperty, Sub(Key) Settings.Reset(Key, Instance:=PageInstanceLeft.Instance))
    End Sub
    Public Shared Sub RefreshSettings(Target As DependencyObject)
        LegacySettingsBindingController.RefreshSettings(Target, KeyProperty,
            Function(Key) Settings.Get(Key, Instance:=PageInstanceLeft.Instance), AddressOf RefreshControl)
    End Sub
    Private Shared Sub RefreshControl(Target As DependencyObject, NewValue As Object)
        If TypeOf Target Is ISettingControl Then DirectCast(Target, ISettingControl).RefreshSetting(NewValue)
    End Sub
    Public Shared Sub SaveSetting(sender As Object)
        LegacySettingsBindingController.SaveSetting(sender, AniControlEnabled,
            Function(Control) TypeOf Control Is ISettingControl, KeyProperty,
            Function(Control) DirectCast(Control, ISettingControl).GetCurrentSetting(),
            Sub(Key, Value) Settings.Set(Key, Value, Instance:=PageInstanceLeft.Instance))
    End Sub
End Class

Public Interface ISettingControl
    Sub RefreshSetting(NewValue As String)
    Function GetCurrentSetting() As String
End Interface
