Imports System
Imports System.Collections.Generic
Imports PCLCS
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Security.Principal

Friend NotInheritable Class NativeMemoryOptimizer
    <StructLayout(LayoutKind.Sequential, Pack:=4)>
    Private Structure PrivilegeToken
        Public PrivilegeCount As UInteger
        Public Luid As Long
        Public Attributes As UInteger
    End Structure

    Private Shared ReadOnly Backend As New LegacyNativeMemoryOptimizer(Of PrivilegeToken)(New LegacyNativeMemoryHost(Of PrivilegeToken) With {
        .CreateToken = Function(Count, Attributes) New PrivilegeToken With {.PrivilegeCount=Count,.Attributes=Attributes},
        .Count = Function(State) State.PrivilegeCount,
        .Lookup = AddressOf Lookup,
        .Capture = AddressOf AdjustTokenPrivilegesCapture,
        .Restore = AddressOf AdjustTokenPrivilegesRestore,
        .Apply = AddressOf NtSetSystemInformation
    })
    Private Shared Function Lookup(SystemName As String, Name As String, ByRef State As PrivilegeToken) As Boolean
        Return LookupPrivilegeValueW(SystemName,Name,State.Luid)
    End Function
    Private Sub New()
    End Sub
    Friend Shared Sub Run()
        Backend.Run()
    End Sub
    Friend Shared Sub ExecuteCommands(Acquire As Func(Of IDisposable), Apply As Func(Of Integer,Integer))
        LegacyMemoryOptimizationOperation.ExecuteCommands(Acquire,Apply)
    End Sub
    Friend Shared Function OpenPrivilegeIdentity() As WindowsIdentity
        Return LegacyNativeMemoryOptimizer(Of PrivilegeToken).OpenPrivilegeIdentity()
    End Function
    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, ExactSpelling:=True, SetLastError:=True)>
    Private Shared Function LookupPrivilegeValueW(SystemName As String, Name As String, ByRef Luid As Long) As Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="AdjustTokenPrivileges", ExactSpelling:=True, SetLastError:=True)>
    Private Shared Function AdjustTokenPrivilegesCapture(Token As IntPtr, DisableAllPrivileges As Boolean,
        ByRef NewState As PrivilegeToken, BufferLength As UInteger, ByRef PreviousState As PrivilegeToken,
        ByRef ReturnLength As UInteger) As Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="AdjustTokenPrivileges", ExactSpelling:=True, SetLastError:=True)>
    Private Shared Function AdjustTokenPrivilegesRestore(Token As IntPtr, DisableAllPrivileges As Boolean,
        ByRef NewState As PrivilegeToken, BufferLength As UInteger, PreviousState As IntPtr,
        ReturnLength As IntPtr) As Boolean
    End Function

    <DllImport("ntdll.dll", ExactSpelling:=True)>
    Private Shared Function NtSetSystemInformation(InformationClass As Integer, ByRef Command As Integer,
        InformationLength As UInteger) As Integer
    End Function
End Class
Friend NotInheritable Class MemoryOptimizationOperation
    Friend Const FailedExitCode As Integer = LegacyMemoryOptimizationOperation.FailedExitCode
    Private Sub New()
    End Sub
    Friend Shared Function Execute(IsElevated As Boolean,ReadAvailable As Func(Of Long),Optimize As Action,RunElevated As Func(Of Integer)) As Long
        Return LegacyMemoryOptimizationOperation.Execute(IsElevated,ReadAvailable,Optimize,RunElevated)
    End Function
    Friend Shared Function ExecuteHelper(ReadAvailable As Func(Of Long),Optimize As Action) As Integer
        Return LegacyMemoryOptimizationOperation.ExecuteHelper(ReadAvailable,Optimize)
    End Function
End Class
