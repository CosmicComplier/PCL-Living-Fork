Imports System.Threading

' LF 50b028: typed original entry points; all real byte I/O remains in C#.
Friend NotInheritable Class FileTaskProgress
    Friend Shared Function Slice(Report As Action(Of Double), Start As Double, Finish As Double) As Action(Of Double)
        Return PCLCS.FileTaskProgress.Slice(Report, Start, Finish)
    End Function
    Friend Shared Function Bytes(Total As Long, Report As Action(Of Double)) As Action(Of Long)
        Return PCLCS.FileTaskProgress.Bytes(Total, Report)
    End Function
    Friend Shared Function Size(Paths As IEnumerable(Of String)) As Long
        Return PCLCS.FileTaskProgress.Size(Paths)
    End Function
    Friend Shared Function Digest(Address As String, Token As CancellationToken, Report As Action(Of Double)) As String
        Return PCLCS.FileTaskProgress.Digest(Address, Token, Report)
    End Function
    Friend Shared Sub Copy(Source As String, Destination As String, Token As CancellationToken, Report As Action(Of Double))
        PCLCS.FileTaskProgress.Copy(Source, Destination, Token, Report)
    End Sub
End Class
