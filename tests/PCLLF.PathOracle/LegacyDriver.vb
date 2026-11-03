' Driver only. The script replaces __LF_METHODS__ with verbatim methods from
' LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 (repository LICENCE/NOTICE.md).
' Prefix handling comes from the actual Shared c774ae4c85fd94e0eaa171db21e78a65870df19b DLL.
' This file deliberately does not contain a second implementation of the guard.
Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning
Imports System.Text
Imports MeloongCore
Imports Newtonsoft.Json.Linq

<Assembly: TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName:=".NET Framework 4.8")>

Friend NotInheritable Class LegacyResourcePaths
__LF_METHODS__

    Friend Shared Function InvokeNormalize(Value As String) As String
        Return Normalize(Value)
    End Function
End Class

Friend NotInheritable Class NativeOrdinalProbe
    ' Candidate only: the results are checked against real Frame String APIs below.
    ' No native result replaces the original VB Normalize/EnsureWithin baseline.
    Friend Shared Function Same(First As String, Other As String) As Boolean
        Return First.Length = Other.Length AndAlso (First.Length = 0 OrElse Compare(First, Other, First.Length))
    End Function
    Friend Shared Function Starts(First As String, Prefix As String) As Boolean
        Return First.Length >= Prefix.Length AndAlso (Prefix.Length = 0 OrElse Compare(First, Prefix, Prefix.Length))
    End Function
    Private Shared Function Compare(First As String, Other As String, Length As Integer) As Boolean
        Dim Result = CompareStringOrdinal(First, Length, Other, Length, True)
        If Result = 0 Then Throw New ArgumentException("Windows ordinal probe failed.", New Win32Exception(Marshal.GetLastWin32Error()))
        Return Result = 2
    End Function
    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, ExactSpelling:=True, SetLastError:=True)>
    Private Shared Function CompareStringOrdinal(First As String, FirstLength As Integer, Other As String, OtherLength As Integer, <MarshalAs(UnmanagedType.Bool)> IgnoreCase As Boolean) As Integer
    End Function
End Class

Friend Module OracleProgram
    Private Const Protocol As String = "pcllf-resource-paths-v2"
    Private Const Lf As String = "f4aa0fae62c3ee8af24c81e61844c8fdd13c8983"
    Private Const SharedRevision As String = "c774ae4c85fd94e0eaa171db21e78a65870df19b"
    Private ReadOnly Utf8 As New UTF8Encoding(False, True)

    Public Function Main(Arguments As String()) As Integer
        Try
            If Arguments.Length <> 2 Then Throw New ArgumentException("Usage: baseline.exe <cases.json> <baseline.json>")
            Dim Manifest = JObject.Parse(File.ReadAllText(Arguments(0), Utf8))
            If Text(Manifest("protocol")) <> Protocol OrElse Text(Manifest("lf")) <> Lf OrElse Text(Manifest("shared")) <> SharedRevision Then Throw New InvalidDataException("Fixed-source protocol mismatch.")
            Dim Results As New JArray()
            For Each Token As JToken In CType(Manifest("cases"), JArray)
                Dim Item = DirectCast(Token, JObject)
                Results.Add(Execute(Item))
            Next
            Dim Framework = CType(Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), GetType(TargetFrameworkAttribute)), TargetFrameworkAttribute)
            Dim Context As New JObject(New JProperty("runtime", ".NET Framework CLR4"), New JProperty("clr", Environment.Version.ToString()),
                New JProperty("domainFramework", AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName), New JProperty("assemblyFramework", Framework.FrameworkName),
                New JProperty("cwd", Environment.CurrentDirectory), New JProperty("culture", CultureInfo.CurrentCulture.Name), New JProperty("is64Bit", Environment.Is64BitProcess),
                New JProperty("useLegacyPathHandling", ReadSwitch("Switch.System.IO.UseLegacyPathHandling")), New JProperty("blockLongPaths", ReadSwitch("Switch.System.IO.BlockLongPaths")),
                New JProperty("legacyTrimFields", ReadTrimFields()), New JProperty("os", Environment.OSVersion.ToString()))
            Dim Provenance = JObject.Parse(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "provenance.json"), Utf8))
            Dim Output As New JObject(New JProperty("protocol", Protocol), New JProperty("lf", Lf), New JProperty("shared", SharedRevision),
                New JProperty("context", Context), New JProperty("provenance", Provenance), New JProperty("results", Results))
            File.WriteAllText(Arguments(1), Output.ToString(), Utf8)
            Console.WriteLine("Independent CLR4 path baseline: " & Results.Count.ToString(CultureInfo.InvariantCulture) & " cases; domain=" & AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName & "; cwd=" & Environment.CurrentDirectory)
            Return 0
        Catch ErrorValue As Exception
            Console.Error.WriteLine(ErrorValue)
            Return 1
        End Try
    End Function

    Private Function Text(Value As JToken) As String
        If Value Is Nothing OrElse Value.Type = JTokenType.Null Then Return Nothing
        Return Value.ToString()
    End Function

    Private Function Ok(Optional Value As Object = Nothing) As JObject
        Return New JObject(New JProperty("status", "ok"), New JProperty("value", Value))
    End Function

    Private Function Execute(Item As JObject) As JObject
        Dim Result As JObject
        Try
            Dim Value = Text(Item("value"))
            Select Case Text(Item("operation"))
                Case "normalize"
                    Result = Ok(LegacyResourcePaths.InvokeNormalize(Value))
                Case "prefix"
                    Result = Ok(PathUtils.RemoveExtendedPrefix(Value))
                Case "ensure"
                    LegacyResourcePaths.EnsureWithin(Text(Item("root")), Value, CType(Item("allowRoot"), Boolean))
                    Result = Ok()
                Case "ordinal"
                    Value = OrdinalText(Item, "value")
                    Dim Other = OrdinalText(Item, "other")
                    Result = Ok(String.Equals(Value, Other, StringComparison.OrdinalIgnoreCase))
                    Result("reverseValue") = New JValue(String.Equals(Other, Value, StringComparison.OrdinalIgnoreCase))
                    Result("startsWith") = New JValue(Value.StartsWith(Other, StringComparison.OrdinalIgnoreCase))
                    Result("reverseStartsWith") = New JValue(Other.StartsWith(Value, StringComparison.OrdinalIgnoreCase))
                    ' Keep a candidate failure separate from the real String API results.
                    Try
                        Result("native") = New JObject(New JProperty("value", NativeOrdinalProbe.Same(Value, Other)), New JProperty("reverseValue", NativeOrdinalProbe.Same(Other, Value)),
                            New JProperty("startsWith", NativeOrdinalProbe.Starts(Value, Other)), New JProperty("reverseStartsWith", NativeOrdinalProbe.Starts(Other, Value)))
                    Catch NativeFailure As Exception
                        Dim NativeResult As New JObject(New JProperty("status", "exception"), New JProperty("type", NativeFailure.GetType().FullName), New JProperty("message", NativeFailure.Message))
                        Dim NativeError = TryCast(NativeFailure.InnerException, Win32Exception)
                        If NativeError IsNot Nothing Then NativeResult("win32Error") = New JValue(NativeError.NativeErrorCode)
                        Result("native") = NativeResult
                    End Try
                Case "get-directory-name"
                    Result = Ok(Path.GetDirectoryName(Value))
                Case Else
                    Throw New InvalidDataException("Unknown operation in path protocol.")
            End Select
        Catch ErrorValue As Exception
            Result = New JObject(New JProperty("status", "exception"), New JProperty("type", ErrorValue.GetType().FullName), New JProperty("message", ErrorValue.Message))
            Dim NativeError = TryCast(ErrorValue.InnerException, Win32Exception)
            If NativeError IsNot Nothing Then Result("win32Error") = New JValue(NativeError.NativeErrorCode)
        End Try
        Result("id") = Item("id").DeepClone()
        Return Result
    End Function

    Private Function OrdinalText(Item As JObject, Name As String) As String
        Dim Units = DirectCast(Item(Name & "Utf16"), JArray)
        Dim Buffer(Units.Count - 1) As Char
        For Index = 0 To Buffer.Length - 1
            Buffer(Index) = Convert.ToChar(CType(Units(Index), Integer))
        Next
        Return New String(Buffer)
    End Function

    Private Function ReadTrimFields() As JArray
        Dim Results As New JArray()
        For Each Pair In New String()() {New String() {"System.IO.Path", "TrimEndChars"}, New String() {"System.IO.LongPathHelper", "s_trimEndChars"}}
            Dim Owner = GetType(Path).Assembly.GetType(Pair(0))
            Dim Field As FieldInfo = If(Owner Is Nothing, Nothing, Owner.GetField(Pair(1), BindingFlags.Static Or BindingFlags.Public Or BindingFlags.NonPublic))
            Dim Units As New JArray()
            If Field IsNot Nothing Then
                For Each Unit As Char In DirectCast(Field.GetValue(Nothing), Char())
                    Units.Add(Convert.ToInt32(Unit))
                Next
            End If
            Results.Add(New JObject(New JProperty("type", Pair(0)), New JProperty("field", Pair(1)), New JProperty("present", Field IsNot Nothing), New JProperty("utf16", Units)))
        Next
        Return Results
    End Function

    Private Function ReadSwitch(Name As String) As JObject
        Dim Value As Boolean
        Dim Present = AppContext.TryGetSwitch(Name, Value)
        Return New JObject(New JProperty("present", Present), New JProperty("value", Value))
    End Function
End Module
