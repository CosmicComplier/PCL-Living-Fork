'本机个人游戏资料，位于整合包和共享游戏配置之外。
Friend NotInheritable Class LocalGameData
    Friend Shared ReadOnly Gate As New Object
    Friend Shared Property TestRoot As String
    Private Shared ReadOnly Store As New PCLCS.LocalGameDataStore(Function() Root, Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName))
    Friend Shared ReadOnly Property Service As PCLCS.LocalGameDataStore
        Get
            Return Store
        End Get
    End Property
    Friend Shared ReadOnly Property Root As String
        Get
            Return PCLCS.LocalGameDataStore.GetRoot(TestRoot)
        End Get
    End Property
    Friend Shared Function Address(Name As String) As String
        Return Store.Address(Name)
    End Function
    Friend Shared Function ReadJson(Name As String) As JObject
        Return Store.ReadJson(Name)
    End Function
    Friend Shared Sub WriteJson(Name As String, Value As JObject)
        Store.WriteJson(Name, Value)
    End Sub
    Friend Shared Function Hash(Bytes As Byte()) As String
        Return PCLCS.LocalGameDataStore.Hash(Bytes)
    End Function
    Friend Shared Sub AtomicWrite(FileName As String, Bytes As Byte(), ExpectedHash As String, Backup As Boolean)
        PCLCS.LocalGameDataStore.AtomicWrite(FileName, Bytes, ExpectedHash, Backup)
    End Sub
    Friend Shared Function ProcessMatches(Id As Integer, StartedUtc As Long) As Boolean
        Return PCLCS.LocalGameDataStore.ProcessMatches(Id, StartedUtc)
    End Function
End Class
