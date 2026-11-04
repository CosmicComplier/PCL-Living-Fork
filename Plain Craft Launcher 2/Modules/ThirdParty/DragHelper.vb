'来源：https://blog.csdn.net/simpleman2000/article/details/140952294
'保留原公开事件、POINT 和属性身份；Win32 消息/查询/筛选业务由 C# 原规则实现。
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports PCLLF.Presentation.Windowing
Imports PCLCS.Startup

Public Class DragHelper
    Public Event DragDrop As EventHandler
    Public Property DropFilePaths As String()
        Get
            Return _DropFilePathsBackingField
        End Get
        Private Set(value As String())
            _DropFilePathsBackingField = value
        End Set
    End Property
    Private _DropFilePathsBackingField As String()
    Public Property DropPoint As POINT
        Get
            Return _DropPointBackingField
        End Get
        Private Set(value As POINT)
            _DropPointBackingField = value
        End Set
    End Property
    Private _DropPointBackingField As POINT
    Public Property HwndIntPtrSource As HwndSource
    Private ReadOnly Service As New LegacyDragHelperRuntime(New LegacyDragHelperHost With {
        .Source = Function() HwndIntPtrSource,
        .WritePoint = Sub(Value) DropPoint = New POINT With {.X = Value.X, .Y = Value.Y},
        .WriteFiles = Sub(Value) DropFilePaths = Value, .EmitDrop = AddressOf PublishDrop
    })
    <StructLayout(LayoutKind.Sequential)>
    Public Structure POINT
        Public X As Integer
        Public Y As Integer
    End Structure
    Private Sub PublishDrop()
        RaiseEvent DragDrop(Me, EventArgs.Empty)
    End Sub
    Public Sub AddHook()
        Service.AddHook(AddressOf WndProc)
    End Sub
    Public Sub RemoveDragHook()
        Service.RemoveDragHook(AddressOf WndProc)
    End Sub
    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        Return Service.WndProc(hwnd, msg, wParam, lParam, handled)
    End Function
    Private Shared Sub ChangeMessageFilter(handle As IntPtr)
        LegacyDragHelperRuntime.ChangeMessageFilter(handle)
    End Sub
    Private Shared Function TryGetDropInfo(msg As Integer, wParam As IntPtr, ByRef dropFilePaths As String(), ByRef dropPoint As POINT) As Boolean
        dropPoint = New POINT()
        Dim NativePoint As New LegacyDropPoint()
        Try
            Return LegacyDragHelperRuntime.TryGetDropInfo(msg, wParam, dropFilePaths, NativePoint)
        Finally
            dropPoint = New POINT With {.X = NativePoint.X, .Y = NativePoint.Y}
        End Try
    End Function
End Class
