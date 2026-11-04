Imports PCLCS

'原 LF 类型、转换运算符与实际应用资源／日志边界；图片业务由 C# 实现。
Public Class MyBitmap
    Inherits LegacyBitmap

    Private Shared ReadOnly BitmapHost As New LegacyBitmapHost(
        GetType(MyBitmap).Assembly.GetName().Name,
        Function(Key) CType(My.Application.TryFindResource(Key), System.Drawing.Bitmap),
        Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message))

    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(FilePathOrResourceName As String)
        MyBase.New(FilePathOrResourceName, BitmapHost)
    End Sub
    Public Sub New(Image As ImageSource)
        MyBase.New(Image)
    End Sub
    Public Sub New(Image As System.Drawing.Image)
        MyBase.New(Image)
    End Sub
    Public Sub New(Image As System.Drawing.Bitmap)
        MyBase.New(Image)
    End Sub
    Public Sub New(Image As ImageBrush)
        MyBase.New(Image)
    End Sub

    Public Shared Widening Operator CType(Image As System.Drawing.Image) As MyBitmap
        If Image Is Nothing Then Return Nothing
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As System.Drawing.Image
        If Image Is Nothing Then Return Nothing
        Return Image.Pic
    End Operator
    Public Shared Widening Operator CType(Image As ImageSource) As MyBitmap
        If Image Is Nothing Then Return Nothing
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As ImageSource
        If Image Is Nothing Then Return Nothing
        Return Image.ToImageSource()
    End Operator
    Public Shared Widening Operator CType(Image As System.Drawing.Bitmap) As MyBitmap
        If Image Is Nothing Then Return Nothing
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As System.Drawing.Bitmap
        If Image Is Nothing Then Return Nothing
        Return Image.Pic
    End Operator
    Public Shared Widening Operator CType(Image As ImageBrush) As MyBitmap
        If Image Is Nothing Then Return Nothing
        Return New MyBitmap(Image)
    End Operator
    Public Shared Widening Operator CType(Image As MyBitmap) As ImageBrush
        If Image Is Nothing Then Return Nothing
        Return Image.ToImageBrush()
    End Operator

    Public Shared Shadows Function NormalizeResourceUri(Value As String) As String
        Return LegacyBitmap.NormalizeResourceUri(Value, GetType(MyBitmap).Assembly.GetName().Name)
    End Function
    Protected Overrides Function Wrap(Image As System.Drawing.Bitmap) As LegacyBitmap
        Return New MyBitmap(Image)
    End Function
    Public Shadows Function Clip(X As Integer, Y As Integer, Width As Integer, Height As Integer) As MyBitmap
        Return DirectCast(MyBase.Clip(X, Y, Width, Height), MyBitmap)
    End Function
    Public Shadows Function RotateFlip(Type As System.Drawing.RotateFlipType) As MyBitmap
        Return DirectCast(MyBase.RotateFlip(Type), MyBitmap)
    End Function
    Public Shadows Sub Save(FilePath As String)
        MyBase.Save(FilePath)
    End Sub
End Class
