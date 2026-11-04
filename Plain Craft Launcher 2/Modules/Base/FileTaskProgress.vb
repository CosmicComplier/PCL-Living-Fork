Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading

'按实际读取/复制的字节分段上报；不使用计时器伪造文件任务的进度。
Friend NotInheritable Class FileTaskProgress
    Friend Shared Function Slice(Report As Action(Of Double), Start As Double, Finish As Double) As Action(Of Double)
        If Report Is Nothing Then Return Nothing
        Return Sub(Value) Report(Start + (Finish - Start) * Math.Max(0, Math.Min(1, Value)))
    End Function

    Friend Shared Function Bytes(Total As Long, Report As Action(Of Double)) As Action(Of Long)
        If Report Is Nothing Then Return Nothing
        Dim Completed As Long = 0
        Return Sub(Count)
                   Completed += Count
                   Report(Math.Min(1, Completed / Math.Max(1.0, Total)))
               End Sub
    End Function

    Friend Shared Function Size(Paths As IEnumerable(Of String)) As Long
        Return Paths.Sum(Function(Address) If(File.Exists(Address), Math.Max(1L, New FileInfo(Address).Length), 1L))
    End Function

    Friend Shared Function Digest(Address As String, Token As CancellationToken, Report As Action(Of Double)) As String
        If Not File.Exists(Address) Then Return Nothing
        Using Input = File.OpenRead(Address), Algorithm = SHA1.Create()
            Dim Buffer(131071) As Byte, Update = Bytes(Input.Length, Report)
            Do
                Token.ThrowIfCancellationRequested()
                Dim Count = Input.Read(Buffer, 0, Buffer.Length)
                If Count = 0 Then Exit Do
                Algorithm.TransformBlock(Buffer, 0, Count, Buffer, 0)
                Update?.Invoke(Count)
            Loop
            Algorithm.TransformFinalBlock(New Byte() {}, 0, 0)
            Report?.Invoke(1)
            Return "sha1:" & BitConverter.ToString(Algorithm.Hash).Replace("-", "").ToLowerInvariant()
        End Using
    End Function

    Friend Shared Sub Copy(Source As String, Destination As String, Token As CancellationToken, Report As Action(Of Double))
        Using Input = File.OpenRead(Source), Output As New FileStream(Destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)
            Dim Buffer(131071) As Byte, Update = Bytes(Input.Length, Report)
            Do
                Token.ThrowIfCancellationRequested()
                Dim Count = Input.Read(Buffer, 0, Buffer.Length)
                If Count = 0 Then Exit Do
                Output.Write(Buffer, 0, Count)
                Update?.Invoke(Count)
            Loop
            Output.Flush()
        End Using
        Report?.Invoke(1)
    End Sub
End Class
