Imports PCLLF.Presentation.Controls

Public Class MyImage
    Inherits MyImageView

    Public Shared Shadows ReadOnly EnableCacheProperty As DependencyProperty = MyImageView.EnableCacheProperty
    Public Shared Shadows ReadOnly SourceProperty As DependencyProperty = MyImageView.SourceProperty
    Protected Overrides Function NewUuid() As Integer
        Return GetUuid()
    End Function
    Protected Overrides ReadOnly Property TempRoot As String
        Get
            Return PathTemp
        End Get
    End Property
    Protected Overrides Function ReadBitmap(Address As String) As ImageSource
        Return CType(New MyBitmap(Address), ImageSource)
    End Function
    Protected Overrides Sub DeleteFile(Path As String)
        FileUtils.Delete(Path)
    End Sub
    Protected Overrides Function FileInformation(Path As String) As IO.FileInfo
        Return FileUtils.GetInfo(Path)
    End Function
    Protected Overrides Sub UiPost(Work As Action)
        RunInUi(Work)
    End Sub
    Protected Overrides Sub Warn(ErrorValue As Exception, Message As String)
        Logger.Warn(ErrorValue, Message)
    End Sub
    Protected Overrides Sub [Error](ErrorValue As Exception, Message As String)
        Logger.Error(ErrorValue, Message, LogBehavior.Toast)
    End Sub
    Protected Overrides Sub Download(Addresses As String(), Path As String)
        NetDownloadByLoader(Addresses, Path, SimulateBrowserHeaders:=True)
    End Sub
    Protected Overrides Sub StartWorker(Work As Action, Name As String, Priority As ThreadPriority)
        RunInNewThread(Work, Name, Priority)
    End Sub
End Class
