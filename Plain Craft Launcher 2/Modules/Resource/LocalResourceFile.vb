Public Class LocalResourceFile
    Inherits PCLCS.LocalResourceFileData(Of ResourceProject, ResourceVersion)

    Private Shared ReadOnly Services As New PCLCS.LocalResourceFileServices(Of ResourceProject, ResourceVersion) With {
        .ReadProject = Function(Data) New ResourceProject(Data),
        .ReadVersion = AddressOf ResourceVersion.FromCacheJson,
        .HideUpdates = Function() Settings.Get(Of Boolean)("UiHiddenFunctionModUpdate"),
        .DisableInstanceUpdates = Function() Settings.Get(Of Boolean)("VersionAdvanceDisableModUpdate", Instance:=PageInstanceLeft.Instance),
        .ReadHashCache = Function(Key) ReadIni(PathTemp & "Cache\ModHash.ini", Key),
        .WriteHashCache = Sub(Key, Value) WriteIni(PathTemp & "Cache\ModHash.ini", Key, Value),
        .ReadBytes = Function(Path) FileUtils.ReadAsBytes(Path),
        .Sha1 = Function(Path) CryptographyUtils.ComputeFileHash(Path, CryptographyUtils.HashMethod.Sha1)}

    Public Sub New(FileInfo As FileInfo)
        MyBase.New(FileInfo, Services)
    End Sub
    Public Sub New(FullName As String)
        Me.New(FileUtils.GetInfo(FullName))
    End Sub
    Public Event OnUpdate(sender As LocalResourceFile)
    Protected Overrides Sub NotifyUpdated()
        RaiseEvent OnUpdate(Me)
    End Sub
    Public Overrides Function Equals(Obj As Object) As Boolean
        Return MyBase.Equals(TryCast(Obj, LocalResourceFile))
    End Function
    Public Overrides Function GetHashCode() As Integer
        Return MyBase.GetHashCode()
    End Function
End Class
