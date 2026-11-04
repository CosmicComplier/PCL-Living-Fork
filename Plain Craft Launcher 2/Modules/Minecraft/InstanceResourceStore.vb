Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Imaging

Friend Enum InstanceResourceKind
    Saves
    Screenshots
    ResourcePacks
    ShaderPacks
    Schematics
End Enum

Friend NotInheritable Class InstanceResourceEntry
    Inherits PCLCS.InstanceResourceEntryData
    Public Icon As ImageSource
End Class

'只管理指定资源目录；读取不创建目录，写入不覆盖同名资源，删除可在本页恢复。
Friend NotInheritable Class InstanceResourceStore
    Private Shared ReadOnly Transactions As New PCLCS.InstanceResourceTransactions(AddressOf PathUtils.RemoveExtendedPrefix)

    Friend Shared Function Title(Kind As InstanceResourceKind) As String
        Return PCLCS.InstanceResourceKindRules.Title(CInt(Kind))
    End Function

    Friend Shared Function FolderName(Kind As InstanceResourceKind) As String
        Return PCLCS.InstanceResourceKindRules.FolderName(CInt(Kind))
    End Function

    Friend Shared Function Root(GameFolder As String, Kind As InstanceResourceKind) As String
        Return Path.Combine(GameFolder, FolderName(Kind))
    End Function

    Private Shared Function Normalize(Value As String) As String
        Return PCLCS.InstanceResourcePaths.Normalize(Value, AddressOf PathUtils.RemoveExtendedPrefix)
    End Function

    Friend Shared Sub EnsureWithin(Root As String, FileName As String, Optional AllowRoot As Boolean = False)
        PCLCS.InstanceResourcePaths.EnsureWithin(Root, FileName, AllowRoot, AddressOf PathUtils.RemoveExtendedPrefix)
    End Sub

    Friend Shared Function SafeName(Value As String) As String
        Return PCLCS.InstanceResourceNameRules.SafeName(Value)
    End Function

    Friend Shared Sub DeleteStage(Root As String, Stage As String)
        Transactions.DeleteStage(Root, Stage)
    End Sub

    Friend Shared Sub ValidatePackage(FileName As String, Kind As InstanceResourceKind)
        PCLCS.InstanceResourcePackageRules.ValidatePackage(FileName, CInt(Kind))
    End Sub

    Private Shared Function AcceptFile(Kind As InstanceResourceKind, FileName As String) As Boolean
        Return PCLCS.InstanceResourceFileRules.AcceptFile(CInt(Kind), FileName)
    End Function

    Friend Shared Function Scan(Root As String, Folder As String, Kind As InstanceResourceKind, Token As CancellationToken) As List(Of InstanceResourceEntry)
        Return PCLCS.InstanceResourceCatalog.Scan(Root, Folder, CInt(Kind), Token, MapDownloadInstaller.DownloadFolderName,
            Function() New InstanceResourceEntry(),
            Sub(Item, FileName, Width) Item.Icon = LoadImage(FileName, Width),
            Sub(Item, Input, Width) Item.Icon = DecodeImage(Input, Width),
            Function(Value) MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(Value, " "),
            AddressOf PathUtils.RemoveExtendedPrefix)
    End Function

    Private Shared Function FormatSize(Size As Long) As String
        Return PCLCS.InstanceResourceDetailsRules.FormatSize(Size)
    End Function

    Private Shared Sub ReadPackInfo(Entry As InstanceResourceEntry, ByRef Detail As String)
        PCLCS.InstanceResourceCatalog.ReadPackInfo(Entry, Detail,
            Sub(Item, FileName, Width) Item.Icon = LoadImage(FileName, Width),
            Sub(Item, Input, Width) Item.Icon = DecodeImage(Input, Width),
            Function(Value) MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(Value, " "))
    End Sub

    Friend Shared Function LoadImage(FileName As String, Width As Integer) As BitmapSource
        Return PCLCS.InstanceResourceCatalog.LoadImage(Of BitmapSource)(FileName, Width, AddressOf PCLCS.InstanceResourceImages.Decode)
    End Function

    Private Shared Function DecodeImage(Stream As Stream, Width As Integer) As BitmapSource
        Return PCLCS.InstanceResourceImages.Decode(Stream, Width)
    End Function

    Friend Shared Sub ValidateEntry(Root As String, Entry As InstanceResourceEntry)
        PCLCS.InstanceResourceEntryValidator.ValidateEntry(Root, Entry, Sub(Parent, FileName) EnsureWithin(Parent, FileName))
    End Sub

    Friend Shared Function Rename(Root As String, Entry As InstanceResourceEntry, NewName As String) As String
        Return Transactions.Rename(Root, Entry, NewName)
    End Function

    Friend Shared Function ImportFile(Root As String, Folder As String, Kind As InstanceResourceKind, Source As String, Token As CancellationToken) As String
        Return Transactions.ImportFile(Root, Folder, CInt(Kind), Source, Token)
    End Function

    Friend Shared Function ImportWorld(Root As String, Source As String, Name As String, Token As CancellationToken) As String
        Return Transactions.ImportWorld(Root, Source, Name, Token)
    End Function

    Friend Shared Function ImportFolder(Root As String, Folder As String, Kind As InstanceResourceKind, Source As String, Name As String, Token As CancellationToken) As String
        Return Transactions.ImportFolder(Root, Folder, CInt(Kind), Source, Name, Token)
    End Function

    Friend Shared Sub Export(Root As String, Entry As InstanceResourceEntry, Destination As String, Token As CancellationToken)
        Transactions.Export(Root, Entry, Destination, Token)
    End Sub

    Friend Shared Sub ExportMany(Root As String, Items As IEnumerable(Of InstanceResourceEntry), Folder As String, Token As CancellationToken)
        Transactions.ExportMany(Root, Items, Folder, Token)
    End Sub

    Friend Shared Sub RecycleMany(Root As String, Items As IEnumerable(Of InstanceResourceEntry), RecycleRoot As String, Token As CancellationToken)
        Transactions.RecycleMany(Root, Items, RecycleRoot, Token)
    End Sub

    Friend Shared Function MoveToRecycle(Root As String, Entry As InstanceResourceEntry, RecycleRoot As String) As String
        Return Transactions.MoveToRecycle(Root, Entry, RecycleRoot)
    End Function

    Friend Shared Function RestoreLast(Root As String, RecycleRoot As String) As String
        Return Transactions.RestoreLast(Root, RecycleRoot)
    End Function
End Class
