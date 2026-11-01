Imports System.IO
Imports System.IO.Compression
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Friend NotInheritable Class ModDependencyIcon
    '只读包内图标，解码后释放 JAR 句柄；异常图标不影响依赖分析。
    Friend Shared Function Read(FileName As String, EntryName As String) As ImageSource
        If String.IsNullOrEmpty(FileName) OrElse String.IsNullOrEmpty(EntryName) OrElse EntryName.Length > 2048 Then Return Nothing
        Dim Name = EntryName.Replace("\"c, "/"c)
        If Name.StartsWith("/", StringComparison.Ordinal) OrElse Name.Contains(":") OrElse Name.Split("/"c).Any(Function(Part) Part = ".." OrElse Part = "" OrElse Part = ".") Then Return Nothing
        Try
            If (File.GetAttributes(FileName) And FileAttributes.ReparsePoint) <> 0 Then Return Nothing
            Using Input As New FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete), Archive As New ZipArchive(Input, ZipArchiveMode.Read)
                Dim Entry = Archive.GetEntry(Name)
                If Entry Is Nothing OrElse Entry.Length = 0 OrElse Entry.Length > 4 * 1024 * 1024 Then Return Nothing
                Using Data As New MemoryStream(), Icon = Entry.Open()
                    Dim Buffer(8191) As Byte, Count As Integer
                    Do
                        Count = Icon.Read(Buffer, 0, Buffer.Length)
                        If Data.Length + Count > 4 * 1024 * 1024 Then Return Nothing
                        Data.Write(Buffer, 0, Count)
                    Loop While Count <> 0
                    Data.Position = 0
                    Dim Image As New BitmapImage
                    Image.BeginInit()
                    Image.CacheOption = BitmapCacheOption.OnLoad
                    Image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile
                    Image.DecodePixelWidth = 32
                    Image.StreamSource = Data
                    Image.EndInit()
                    Image.Freeze()
                    Return Image
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function
End Class
