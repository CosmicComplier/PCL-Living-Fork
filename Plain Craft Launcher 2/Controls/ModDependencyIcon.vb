Imports System.Windows.Media

Friend NotInheritable Class ModDependencyIcon
    '保留原图视图／异步图标宿主入口，解码与 JAR 生命周期由 C# 实现。
    Friend Shared Function Read(FileName As String, EntryName As String) As ImageSource
        Return PCLCS.ModDependencyIconFiles.Read(FileName, EntryName)
    End Function
End Class
