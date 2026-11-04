Imports System.Reflection
Imports PCLLF.Presentation.Foundation

'精确固定 Shared 服务的实际卡片；所有主题/生命周期业务在 C#。
Friend Module ModernTooltipTheme
    Private ReadOnly CardField As FieldInfo = GetType(MeloongCore.Wpf.ModernTooltipService).GetField("popupCard", BindingFlags.NonPublic Or BindingFlags.Static)
    Private ReadOnly Theme As New LegacyTooltipThemeAdapter(AddressOf CurrentCard)
    Private Function CurrentCard() As Border
        If CardField Is Nothing OrElse CardField.FieldType IsNot GetType(Border) Then Return Nothing
        Return TryCast(CardField.GetValue(Nothing), Border)
    End Function
    Friend Sub Install()
        Theme.Install()
    End Sub
End Module
