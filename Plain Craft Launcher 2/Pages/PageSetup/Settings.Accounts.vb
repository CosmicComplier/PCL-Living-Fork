Partial Public Class Settings
    Private Shared ReadOnly SharedAccountSync As New Object

    Friend Shared Function PlanLegacyMicrosoftUpgrade(Raw As IDictionary(Of String, String), DeviceIdentify As String) As Dictionary(Of String, String)
        Return Backend.PlanLegacyMicrosoftUpgrade(Raw, DeviceIdentify)
    End Function
    Friend Shared Sub EnsureMicrosoftCacheUpgrade()
        Backend.EnsureMicrosoftCacheUpgrade()
    End Sub
    Friend Shared Function AccountSnapshot(Prefix As String) As Dictionary(Of String, Object)
        Return Backend.AccountSnapshot(Prefix)
    End Function
    Private Shared Sub WriteAccountValues(Values As IDictionary(Of String, Object))
        Backend.WriteAccountValues(Values)
    End Sub
    Public Shared Function UpdateMicrosoftAccounts(Change As Action(Of JObject)) As JObject
        Return Backend.UpdateMicrosoftAccounts(Change)
    End Function
    Public Shared Sub SaveMicrosoftSession(Values As IDictionary(Of String, Object), PreviousName As String, Optional Canceled As Func(Of Boolean) = Nothing)
        Backend.SaveMicrosoftSession(Values, PreviousName, Canceled)
    End Sub
    Public Shared Sub SaveAccountGroup(Values As IDictionary(Of String, Object), Optional Canceled As Func(Of Boolean) = Nothing)
        Backend.SaveAccountGroup(Values, Canceled)
    End Sub
    Public Shared Function UpdateLegacyNames(Change As Action(Of List(Of String))) As List(Of String)
        Return Backend.UpdateLegacyNames(Change)
    End Function
    Public Shared Function UpdateServerAccounts(Token As String, Name As String, Password As String, Remove As Boolean) As Dictionary(Of String, String)
        Return Backend.UpdateServerAccounts(Token, Name, Password, Remove)
    End Function
End Class
