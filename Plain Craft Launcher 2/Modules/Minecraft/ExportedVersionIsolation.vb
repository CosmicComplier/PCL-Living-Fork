Imports System.IO.Compression

'仅本项目导出的隔离标记生效；其他整合包保留原有的默认隔离行为。
Friend NotInheritable Class ExportedVersionIsolation
    Friend Const MarkerKey As String = "LivingExportVersionIsolation"
    Friend NotInheritable Class Choice
        Inherits PCLCS.InstanceCloneExportChoiceData
        Friend Sub New(Enabled As Boolean, Optional ExtraVersionFolders As IEnumerable(Of String) = Nothing)
            MyBase.New(Enabled, ExtraVersionFolders)
        End Sub
    End Class
    Private Shared ReadOnly Service As New PCLCS.InstanceCloneExportIsolationEngine(Of Choice)(
        New PCLCS.InstanceCloneExportIsolationHost With {
            .WriteIni = Sub(FileName, Key, Value) WriteIni(FileName, Key, CStr(Value)),
            .ClearIniCache = Sub(FileName) IniClearCache(FileName),
            .AddSlashSuffix = Function(Value) PathUtils.AddSlashSuffix(Value),
            .RemoveSettingCache = Sub(Key, Folder) Settings.Entries(Key).ValueCache.Remove(Folder),
            .CopyFile = Sub(Source, Target) FileUtils.Copy(Source, Target)},
        Function(Enabled, Folders) New Choice(Enabled, Folders))
    Friend Shared Sub ConfigureExport(OverridesFolder As String, Enabled As Boolean, Optional VersionFolders As IEnumerable(Of String) = Nothing)
        Service.ConfigureExport(OverridesFolder, Enabled, VersionFolders)
    End Sub
    Friend Shared Function ReadArchiveChoice(Archive As ZipArchive, ArchiveBaseFolder As String) As Choice
        Return Service.ReadArchiveChoice(Archive, ArchiveBaseFolder)
    End Function
    Friend Shared Sub ConfigureTarget(VersionFolder As String, Policy As Choice)
        Service.ConfigureTarget(VersionFolder, Policy)
    End Sub
    Friend Shared Function GameFolder(VersionFolder As String, MinecraftFolder As String, Policy As Choice) As String
        Return Service.GameFolder(VersionFolder, MinecraftFolder, Policy)
    End Function
    Private Shared Function NormalizeRelative(Value As String) As String
        Return PCLCS.InstanceCloneExportIsolationRules.NormalizeRelative(Value)
    End Function
    Friend Shared Function OverrideTarget(Relative As String, VersionFolder As String, GameFolder As String, Policy As Choice) As String
        Return Service.OverrideTarget(Relative, VersionFolder, GameFolder, Policy)
    End Function
    Friend Shared Sub CopyOverrides(OverridesFolder As String, VersionFolder As String, GameFolder As String, Policy As Choice)
        Service.CopyOverrides(OverridesFolder, VersionFolder, GameFolder, Policy)
    End Sub
End Class
