Friend NotInheritable Class InstalledComponentSummary
    Friend NotInheritable Class Item
        Inherits PCLCS.InstalledComponentSummaryItemData
        Friend Sub New(Name As String, Version As String, Icon As String)
            MyBase.New(Name, Version, Icon)
        End Sub
    End Class
    Friend Shared ReadOnly Engine As New PCLCS.InstalledComponentSummaryEngine(Of Item)(Function(Name, Version, Icon) New Item(Name, Version, Icon))
    Friend Shared Function Describe(Version As McVersion, Optional Components As McComponents = Nothing,
        Optional LocalMods As IDictionary(Of String, String) = Nothing) As List(Of Item)
        Return Engine.Describe(Version, Components, LocalMods)
    End Function
    Friend Shared Function ReadLocalMods(ModsFolder As String) As Dictionary(Of String, String)
        Return PCLCS.InstalledComponentSummaryEngine(Of Item).ReadLocalMods(ModsFolder)
    End Function
End Class
