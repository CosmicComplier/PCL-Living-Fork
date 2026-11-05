' Immutable LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 MyBitmap.vb excerpts.
' Original bitmap field, Bitmap conversion/constructor, Clip and RotateFlip only.
' Repository LICENCE applies. Only the enclosing test compilation is synthetic.
Public Class MyBitmap
    Public Pic As System.Drawing.Bitmap

    Public Shared Widening Operator CType(Image As System.Drawing.Bitmap) As MyBitmap
        If Image Is Nothing Then Return Nothing
        Return New MyBitmap(Image)
    End Operator

    Public Sub New(Image As System.Drawing.Bitmap)
        Pic = Image
    End Sub

    Public Function Clip(X As Integer, Y As Integer, Width As Integer, Height As Integer) As MyBitmap
        Dim bmp As New System.Drawing.Bitmap(Width, Height, Pic.PixelFormat)
        bmp.SetResolution(Pic.HorizontalResolution, Pic.VerticalResolution)
        Using g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmp)
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
            g.TranslateTransform(-X, -Y)
            g.DrawImage(Pic, New System.Drawing.Rectangle(0, 0, Pic.Width, Pic.Height))
        End Using
        Return bmp
    End Function

    Public Function RotateFlip(Type As System.Drawing.RotateFlipType) As MyBitmap
        Dim bmp As New System.Drawing.Bitmap(Pic)
        bmp.SetResolution(Pic.HorizontalResolution, Pic.VerticalResolution)
        bmp.RotateFlip(Type)
        Return bmp
    End Function
End Class
