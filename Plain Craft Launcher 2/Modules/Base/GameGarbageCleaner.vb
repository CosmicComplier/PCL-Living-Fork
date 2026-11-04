Imports System
Imports System.Collections.Generic
Imports PCLCS
Friend NotInheritable Class GameGarbageCleaner
    Friend NotInheritable Class CleanupResult
        Inherits LegacyGameGarbageCleanupResult
    End Class
    Private Sub New()
    End Sub
    Friend Shared Function Clean(GameFolders As IEnumerable(Of String), Optional ShouldStop As Func(Of Boolean)=Nothing) As CleanupResult
        Return LegacyGameGarbageCleaner.Clean(GameFolders,Function() New CleanupResult(),ShouldStop)
    End Function
End Class
