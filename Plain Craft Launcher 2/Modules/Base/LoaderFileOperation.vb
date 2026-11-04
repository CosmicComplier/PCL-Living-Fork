Imports PCLCS.Loaders

' 文件事务独占范围由 C# 真实负载计数控制；旧调用入口保持不变。
Friend NotInheritable Class LoaderFileOperation
    Friend Shared Function CreateWorkload(Loader As LoaderCombo(Of String), Input As String) As Action
        Return CreateWorkloadWithProgress(Loader, Input, Nothing)
    End Function
    Friend Shared Function CreateWorkloadWithProgress(Loader As LoaderCombo(Of String), Input As String, Progress As ProgressProvider) As Action
        Return LegacyLoaderFileOperation.CreateWorkload(Of LoaderBase, LoadState, MyLoading.MyLoadingState)(Loader, Input, Progress)
    End Function
End Class
