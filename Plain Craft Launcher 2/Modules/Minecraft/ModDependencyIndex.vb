Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Inspector = PCL.ModDependencyInspector

'Persistent local graph snapshots. Validation enumerates file metadata; it never opens an unchanged mod JAR.
Friend NotInheritable Class ModDependencyIndex
    Private Sub New()
    End Sub

    Private Const Schema As Integer = 2
    Private Const MaxBytes As Long = 32L * 1024 * 1024
    Friend Shared Property CacheDirectory As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCLLivingFork", "Cache", "ModDependencyIndex")
    Private Shared ReadOnly Gates As New ConcurrentDictionary(Of String, SemaphoreSlim)(StringComparer.Ordinal)
    Private Shared ReadOnly Memory As New ConcurrentDictionary(Of String, MemoryEntry)(StringComparer.Ordinal)
    Private Shared ReadOnly JsonOptions As New JsonSerializerSettings With {
        .PreserveReferencesHandling = PreserveReferencesHandling.Objects, .ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        .ObjectCreationHandling = ObjectCreationHandling.Replace, .ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        .TypeNameHandling = TypeNameHandling.None, .MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead, .MaxDepth = 128}

    Private Class Document
        Public Version As Integer = Schema
        Public Root As String
        Public Scope As String
        Public Fingerprint As String
        Public Generation As String
        Public Tree As Inspector.InspectionReport
    End Class
    Private Class MemoryEntry
        Public Json As String
        Public DiskStamp As String
        Public Persisted As Boolean
    End Class
    Private Class Inventory
        Public Signature As String
        Public Files As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    End Class

    Private Shared Function RootKey(Root As String) As String
        Return Inspector.FilePathKey(Root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    End Function
    Private Shared Function Digest(Value As String) As String
        Using Hash = SHA256.Create()
            Return BitConverter.ToString(Hash.ComputeHash(Encoding.UTF8.GetBytes(Value))).Replace("-", "").ToLowerInvariant()
        End Using
    End Function
    Private Shared Function ParseScope(Context As Inspector.InspectionContext) As String
        Return Context.LoaderKnown & "|" & Context.MinecraftVersion & "|" & Context.LegacyForgeVersionFolder & "|" & Inspector.IsLegacyForge(Context) & "|" &
            String.Join(";", Context.LoaderVersions.Keys.OrderBy(Function(Id) Id, StringComparer.Ordinal))
    End Function
    Friend Shared Function IndexPath(Root As String, Context As Inspector.InspectionContext) As String
        Return Path.Combine(CacheDirectory, Digest(RootKey(Root).ToUpperInvariant() & "|" & ParseScope(Context)) & ".json")
    End Function
    Friend Shared Sub ClearMemory()
        Memory.Clear()
    End Sub

    Private Shared Function Capture(Root As String, Context As Inspector.InspectionContext, Token As CancellationToken) As Inventory
        Dim Result As New Inventory(), Lines As New List(Of String), Folders As New List(Of String) From {RootKey(Root)}
        If Not String.IsNullOrEmpty(Context.LegacyForgeVersionFolder) AndAlso System.Text.RegularExpressions.Regex.IsMatch(Context.LegacyForgeVersionFolder, "^[0-9.]+$") Then
            Folders.Add(Path.Combine(RootKey(Root), Context.LegacyForgeVersionFolder))
        End If
        For Each Folder In Folders
            Token.ThrowIfCancellationRequested()
            If Not Directory.Exists(Folder) Then
                Lines.Add("missing|" & Folder.ToUpperInvariant()) : Continue For
            End If
            Dim Attributes = File.GetAttributes(Folder)
            Lines.Add("directory|" & Folder.ToUpperInvariant() & "|" & CInt(Attributes And FileAttributes.ReparsePoint))
            If (Attributes And FileAttributes.ReparsePoint) <> 0 Then Continue For
            For Each FullPath In Directory.EnumerateFiles(Folder, "*", SearchOption.TopDirectoryOnly).Where(AddressOf Inspector.IsModFile).OrderBy(Function(Value) Value, StringComparer.OrdinalIgnoreCase)
                Token.ThrowIfCancellationRequested()
                Dim Info As New FileInfo(FullPath), Key = Inspector.FilePathKey(FullPath)
                Result.Files.Add(Key)
                Lines.Add(Key.ToUpperInvariant() & "|" & Info.Length & "|" & Info.LastWriteTimeUtc.Ticks & "|" & Info.CreationTimeUtc.Ticks & "|" & CInt(Info.Attributes And FileAttributes.ReparsePoint))
            Next
        Next
        Result.Signature = Digest(String.Join(vbLf, Lines))
        Return Result
    End Function
    Private Shared Function DiskStamp(FileName As String) As String
        Try
            Dim Info As New FileInfo(FileName)
            Return If(Info.Exists, Info.Length & "|" & Info.LastWriteTimeUtc.Ticks, "missing")
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            Return "unavailable"
        End Try
    End Function
    Private Shared Function ReadDocument(FileName As String) As Document
        Try
            Dim Stamp = DiskStamp(FileName), Saved As MemoryEntry = Nothing, Content As String, Persisted As Boolean
            If Memory.TryGetValue(FileName, Saved) AndAlso Saved.DiskStamp = Stamp Then
                Content = Saved.Json : Persisted = Saved.Persisted
            Else
                Dim Info As New FileInfo(FileName)
                If Not Info.Exists OrElse Info.Length > MaxBytes OrElse (Info.Attributes And FileAttributes.ReparsePoint) <> 0 Then Return Nothing
                Using Stream As New FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete), Reader As New StreamReader(Stream, Encoding.UTF8, True)
                    Dim Builder As New StringBuilder(), Buffer(4095) As Char, Count As Integer
                    Do
                        Count = Reader.Read(Buffer, 0, Buffer.Length)
                        If Builder.Length + Count > MaxBytes Then Throw New InvalidDataException("依赖索引超过读取上限")
                        Builder.Append(Buffer, 0, Count)
                    Loop While Count > 0
                    Content = Builder.ToString()
                End Using
                If Content.Length > MaxBytes Then Return Nothing
                If Memory.Count > 16 Then Memory.Clear()
                Persisted = True
                Memory(FileName) = New MemoryEntry With {.Json = Content, .DiskStamp = Stamp, .Persisted = True}
            End If
            Dim Result = JsonConvert.DeserializeObject(Of Document)(Content, JsonOptions)
            If Result?.Tree IsNot Nothing Then Result.Tree.IndexPersisted = Persisted
            Return Result
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is JsonException
            Return Nothing
        End Try
    End Function
    Private Shared Function Valid(Document As Document, Root As String, Scope As String, Files As Inventory) As Boolean
        Try
            If Document Is Nothing OrElse Document.Version <> Schema OrElse Not String.Equals(Document.Root, RootKey(Root), StringComparison.OrdinalIgnoreCase) OrElse Document.Scope <> Scope OrElse
                Document.Fingerprint <> Files.Signature OrElse Document.Tree?.Context Is Nothing OrElse String.IsNullOrEmpty(Document.Generation) Then Return False
            Dim Tree = Document.Tree
            If Tree.Mods Is Nothing OrElse Tree.Builtins Is Nothing OrElse Tree.Edges Is Nothing OrElse Tree.Issues Is Nothing OrElse Tree.ReverseDependencies Is Nothing OrElse
                Tree.Notices Is Nothing OrElse Tree.OnlineFiles Is Nothing OrElse Tree.OnlineNotes Is Nothing OrElse Tree.Context.LoaderVersions Is Nothing OrElse Tree.Context.AdditionalBuiltins Is Nothing Then Return False
            If Tree.Mods.Any(Function(Item) Item Is Nothing OrElse Item.IsBuiltin OrElse Item.Dependencies Is Nothing OrElse Item.Provides Is Nothing OrElse Not Files.Files.Contains(Inspector.FilePathKey(Item.RootPath))) Then Return False
            If Tree.Builtins.Any(Function(Item) Item Is Nothing OrElse Not Item.IsBuiltin) Then Return False
            For Each Item In Tree.Mods
                For Each Requirement In Item.Dependencies
                    If Not ValidRequirement(Requirement, New HashSet(Of Inspector.Dependency), 0) Then Return False
                Next
            Next
            If Tree.OnlineFiles.Any(Function(Item) Item Is Nothing OrElse Item.Matches Is Nothing OrElse Item.Candidates Is Nothing OrElse Not Files.Files.Contains(Inspector.FilePathKey(Item.FullPath))) Then Return False
            For Each Info In Tree.OnlineFiles
                For Each Match In Info.Matches
                    If Match Is Nothing OrElse Match.Dependencies Is Nothing Then Return False
                    For Each Requirement In Match.Dependencies
                        If Requirement Is Nothing OrElse Requirement.ProjectPaths Is Nothing OrElse Requirement.MatchedPaths Is Nothing OrElse Requirement.EnabledPaths Is Nothing OrElse
                            Requirement.ProjectPaths.Concat(Requirement.MatchedPaths).Concat(Requirement.EnabledPaths).Any(Function(Value) Not Files.Files.Contains(Inspector.FilePathKey(Value))) Then Return False
                    Next
                Next
            Next
            Dim Nodes As New HashSet(Of Inspector.ModInfo)(Tree.Mods.Concat(Tree.Builtins)), Owners As New HashSet(Of Inspector.ModInfo)(Tree.Mods), KnownEdges As New HashSet(Of Inspector.DependencyEdge)(Tree.Edges)
            If Tree.Edges.Any(Function(Edge) Edge Is Nothing OrElse Not Owners.Contains(Edge.Owner) OrElse Not Edge.Owner.Dependencies.Contains(Edge.Requirement) OrElse Edge.Providers.Any(Function(Item) Not Nodes.Contains(Item))) Then Return False
            If Tree.Issues.Any(Function(Issue) Issue Is Nothing OrElse Not Owners.Contains(Issue.Owner) OrElse Issue.RelatedMods.Any(Function(Item) Not Nodes.Contains(Item))) Then Return False
            Return Tree.ReverseDependencies.Values.All(Function(Edges) Edges.All(Function(Edge) KnownEdges.Contains(Edge)))
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is NullReferenceException OrElse TypeOf ex Is IOException
            Return False
        End Try
    End Function
    Private Shared Function ValidRequirement(Value As Inspector.Dependency, Stack As HashSet(Of Inspector.Dependency), Depth As Integer) As Boolean
        If Value Is Nothing OrElse Value.Children Is Nothing OrElse Depth > 32 OrElse Not Stack.Add(Value) Then Return False
        Dim Result = Value.Children.All(Function(Child) ValidRequirement(Child, Stack, Depth + 1)) AndAlso
            (Value.Unless Is Nothing OrElse ValidRequirement(Value.Unless, Stack, Depth + 1))
        Stack.Remove(Value) : Return Result
    End Function
    Private Shared Sub Save(FileName As String, Value As Document, Token As CancellationToken)
        Token.ThrowIfCancellationRequested()
        Value.Tree.IndexPersisted = False
        Dim Content = JsonConvert.SerializeObject(Value, Formatting.None, JsonOptions)
        If Encoding.UTF8.GetByteCount(Content) > MaxBytes Then Return
        Dim Temporary = FileName & "." & Guid.NewGuid().ToString("N") & ".tmp"
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(FileName))
            If (File.GetAttributes(Path.GetDirectoryName(FileName)) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("索引目录是链接目录")
            File.WriteAllText(Temporary, Content, New UTF8Encoding(False))
            Token.ThrowIfCancellationRequested()
            If File.Exists(FileName) Then
                If (File.GetAttributes(FileName) And FileAttributes.ReparsePoint) <> 0 Then Throw New IOException("索引文件是链接文件")
                File.Replace(Temporary, FileName, Nothing)
            Else
                File.Move(Temporary, FileName)
            End If
            Value.Tree.IndexPersisted = True
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            'The graph still works in memory when the persistent cache is temporarily unavailable.
        Finally
            If File.Exists(Temporary) Then
                Try
                    File.Delete(Temporary)
                Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                End Try
            End If
        End Try
        If Memory.Count > 16 Then Memory.Clear()
        Memory(FileName) = New MemoryEntry With {.Json = Content, .DiskStamp = DiskStamp(FileName), .Persisted = Value.Tree.IndexPersisted}
    End Sub
    Private Shared Sub Bind(Tree As Inspector.InspectionReport, Value As Document, FileName As String)
        Tree.IndexFile = FileName : Tree.IndexFingerprint = Value.Fingerprint : Tree.IndexGeneration = Value.Generation
    End Sub

    Friend Shared Function Inspect(Root As String, Context As Inspector.InspectionContext, Token As CancellationToken, Force As Boolean) As Inspector.InspectionReport
        Token.ThrowIfCancellationRequested()
        Dim FileName = IndexPath(Root, Context), Gate = Gates.GetOrAdd(FileName, Function(Key) New SemaphoreSlim(1, 1))
        Gate.Wait(Token)
        Try
            Using Lease As New IndexLease(Digest(FileName.ToUpperInvariant()), Token)
                For Attempt = 0 To 1
                    Token.ThrowIfCancellationRequested()
                    Dim Before As Inventory
                    Try
                        Before = Capture(Root, Context, Token)
                    Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
                        Return Inspector.ScanRaw(Root, Context, Token, Force)
                    End Try
                    Dim Saved = If(Force, Nothing, ReadDocument(FileName))
                    If Valid(Saved, Root, ParseScope(Context), Before) Then
                        Dim Result = Saved.Tree
                        Result.IndexHit = True
                        If Not JToken.DeepEquals(JObject.FromObject(Context), JObject.FromObject(Result.Context)) Then
                            Inspector.Reevaluate(Result, Context, Token)
                            Result.IndexReevaluated = True
                            Save(FileName, Saved, Token)
                        End If
                        If Capture(Root, Context, Token).Signature <> Before.Signature Then Continue For
                        Bind(Result, Saved, FileName) : Return Result
                    End If
                    Dim Tree = Inspector.ScanRaw(Root, Context, Token, Force)
                    If Capture(Root, Context, Token).Signature <> Before.Signature Then Continue For
                    If Tree.IndexReadFailed Then Return Tree
                    Dim Value As New Document With {.Root = RootKey(Root), .Scope = ParseScope(Context), .Fingerprint = Before.Signature,
                        .Generation = Guid.NewGuid().ToString("N"), .Tree = Tree}
                    Save(FileName, Value, Token)
                    Bind(Tree, Value, FileName) : Return Tree
                Next
                Throw New IOException("模组文件在建立依赖索引期间发生变化，请稍后重试。")
            End Using
        Finally
            Gate.Release()
        End Try
    End Function

    Friend Shared Sub RememberOnline(Root As String, Tree As Inspector.InspectionReport, Token As CancellationToken)
        If Token.IsCancellationRequested OrElse String.IsNullOrEmpty(Tree.IndexFile) Then Return
        Dim Gate = Gates.GetOrAdd(Tree.IndexFile, Function(Key) New SemaphoreSlim(1, 1))
        Dim Acquired = False
        Try
            Gate.Wait(Token) : Acquired = True
            Using Lease As New IndexLease(Digest(Tree.IndexFile.ToUpperInvariant()), Token)
                Dim Files = Capture(Root, Tree.Context, Token), Current = ReadDocument(Tree.IndexFile)
                If Not Valid(Current, Root, ParseScope(Tree.Context), Files) OrElse Current.Generation <> Tree.IndexGeneration OrElse Files.Signature <> Tree.IndexFingerprint Then Return
                Current.Tree = Tree : Save(Tree.IndexFile, Current, Token)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse TypeOf ex Is OperationCanceledException
            'An unsuccessful supplement never invalidates the previously committed local tree.
        Finally
            If Acquired Then Gate.Release()
        End Try
    End Sub

    Private NotInheritable Class IndexLease
        Implements IDisposable
        Private ReadOnly Mutex As Mutex
        Private Owned As Boolean
        Public Sub New(Key As String, Token As CancellationToken)
            Mutex = New Mutex(False, "Local\PCLLF.DependencyIndex." & Key)
            Try
                Do
                    Token.ThrowIfCancellationRequested()
                    Try
                        Owned = Mutex.WaitOne(50)
                    Catch ex As AbandonedMutexException
                        Owned = True
                    End Try
                Loop Until Owned
            Catch
                Mutex.Dispose() : Throw
            End Try
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            If Owned Then Mutex.ReleaseMutex()
            Mutex.Dispose()
        End Sub
    End Class
End Class
