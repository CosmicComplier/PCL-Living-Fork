using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstanceLifecycleTests {
    internal static IEnumerable<(string Name,Action Run)> Cases() {
        yield return ("MMC path handoff preserves queued preferences and detaches every old provider without recreating its path", PreferenceHandoff);
        yield return ("MMC lifecycle permits require an exact published source and bound durable intent", JournalPermit);
        yield return ("MMC rename and same-volume move preserve identity/worlds and advance name projections without an installed Java", Relocation);
        yield return ("MMC lifecycle predecision cancellation rolls back while decided target conflicts recover forward", CancellationAndConflict);
    }
    private static void Relocation(){
        using Fixture fixture=new();var imported=fixture.Import();var service=new MmcInstanceLifecycleBackend();
        using var old=new InstanceConfigProvider(fixture.Source);old.Set("absolute",fixture.Source+"/minecraft/saves/world",false);old.Save();
        string renamed=Path.Combine(Path.GetDirectoryName(fixture.Source)!,"renamed");
        var first=service.RelocateAsync(new(fixture.Source,renamed,imported.Definition!.InstanceId,1,MmcSavedSourceStore.ReadPrimary(imported.Paths!).Sha256,MmcInstanceRelocationKind.Rename)).GetAwaiter().GetResult();
        if(first.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception(first.Failure);
        Equal(imported.Definition.InstanceId,first.Definition!.InstanceId);Equal(2L,first.Definition.Generation);Equal(false,Directory.Exists(fixture.Source));
        Equal(true,File.Exists(Path.Combine(renamed,"renamed.json")));Equal(false,File.Exists(Path.Combine(renamed,"source.json")));Denied(old.Save);
        string moved=Path.Combine(Path.GetDirectoryName(fixture.Target)!,"renamed");
        var second=service.RelocateAsync(new(renamed,moved,first.Definition.InstanceId,2,MmcSavedSourceStore.ReadPrimary(MmcInstancePaths.Resolve(renamed)).Sha256)).GetAwaiter().GetResult();
        if(second.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception(second.Failure);
        Equal(3L,second.Definition!.Generation);Equal(imported.Definition.InstanceId,second.Definition.InstanceId);
        Equal("world bytes",File.ReadAllText(Path.Combine(moved,"minecraft","saves","world","level.dat")));
        using var current=new InstanceConfigProvider(moved);Equal(moved.Replace('\\','/')+"/minecraft/saves/world",current.Read("absolute","",false));
        Equal(MmcManagementState.ManagedMmc,MmcManagementProbe.Read(moved).State);
        Equal(MmcInstanceBackendStatus.Succeeded,service.RecoverAsync(second.OperationDirectory!).GetAwaiter().GetResult().Status);
        Equal(false,Directory.Exists(renamed));Equal(false,Directory.Exists(fixture.Source));
    }
    private static void CancellationAndConflict(){
        foreach(string mode in new[]{"before","after","conflict"}){
            using Fixture fixture=new();var imported=fixture.Import();using CancellationTokenSource cancel=new();var service=new MmcInstanceLifecycleBackend();
            var result=service.RelocateAsync(new(fixture.Source,fixture.Target,imported.Definition!.InstanceId,1,MmcSavedSourceStore.ReadPrimary(imported.Paths!).Sha256){
                Checkpoint=(phase,_)=>{if(mode=="before"&&phase==MmcLifecycleCheckpoint.StructurePrepared)cancel.Cancel();
                    if(mode=="after"&&phase==MmcLifecycleCheckpoint.CommitDecided)cancel.Cancel();
                    if(mode=="conflict"&&phase==MmcLifecycleCheckpoint.BeforeDirectoryMove){Directory.CreateDirectory(fixture.Target);File.WriteAllText(Path.Combine(fixture.Target,"foreign.txt"),"keep");}}
            },cancel.Token).GetAwaiter().GetResult();
            if(mode=="before"){Equal(MmcInstanceBackendStatus.Cancelled,result.Status);Equal(1L,MmcSavedSourceStore.ReadPrimary(MmcInstancePaths.Resolve(fixture.Source)).Definition.Generation);Equal(false,Directory.Exists(fixture.Target));Equal(MmcManagementState.ManagedMmc,MmcManagementProbe.Read(fixture.Source).State);}
            else if(mode=="after"){if(result.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception(result.Failure);Equal(true,cancel.IsCancellationRequested);Equal(2L,result.Definition!.Generation);}
            else{Equal(MmcInstanceBackendStatus.NeedsRecovery,result.Status);Equal("keep",File.ReadAllText(Path.Combine(fixture.Target,"foreign.txt")));Equal(MmcManagementState.PendingTransaction,MmcManagementProbe.Read(fixture.Source).State);
                File.Delete(Path.Combine(fixture.Target,"foreign.txt"));Directory.Delete(fixture.Target,false);
                var recovered=service.RecoverAsync(result.OperationDirectory!).GetAwaiter().GetResult();if(recovered.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception(recovered.Failure);Equal(false,Directory.Exists(fixture.Source));Equal(2L,recovered.Definition!.Generation);}
        }
    }
    private static void PreferenceHandoff() {
        using Fixture fixture=new(); var imported=fixture.Import(); Guid id=imported.Definition!.InstanceId;
        using var old=new InstanceConfigProvider(fixture.Source);
        var roots=new[]{fixture.Source,fixture.Target}.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var pauses=new List<MmcConfigurationQuiescence>();var leases=new List<MmcAdmissionLease>();
        try {
            foreach(var root in roots) pauses.Add(MmcConfigurationCoordinator.Quiesce(root));
            foreach(var root in roots) leases.Add(MmcAdmissionGate.Acquire(root,id,TimeSpan.Zero));
            old.Set("late-user-path",fixture.Source+"/minecraft/config/personal.cfg",false);
            MmcIniConfiguration.QueuePreferenceBatch(old,new Dictionary<string,string?>{["MmcJvmArgsMode"]="Custom",["VersionAdvanceJvm"]=""});
            string hash=MmcSavedSourceStore.ReadPrimary(imported.Paths!).Sha256;
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.Target)!);Directory.Move(fixture.Source,fixture.Target);
            MmcConfigurationCoordinator.CompletePathRelocation(pauses[Array.IndexOf(roots,fixture.Source)],pauses[Array.IndexOf(roots,fixture.Target)],
                leases[Array.IndexOf(roots,fixture.Source)],leases[Array.IndexOf(roots,fixture.Target)],id,hash);
            Denied(()=>old.Set("late","must not return",false));Denied(old.Save);Denied(()=>old.Read("late","",false));
            Equal(false,Directory.Exists(fixture.Source));
        } finally {for(int index=leases.Count-1;index>=0;index--)leases[index].Dispose();for(int index=pauses.Count-1;index>=0;index--)pauses[index].Dispose();}
        using var current=new InstanceConfigProvider(fixture.Target);
        Equal(fixture.Target.Replace('\\','/')+"/minecraft/config/personal.cfg",current.Read("late-user-path","",false));
        var prefs=MmcIniConfiguration.CapturePreferences(current);Equal("Custom",prefs.Ini["MmcJvmArgsMode"]);Equal("",prefs.Ini["VersionAdvanceJvm"]);
        current.Save();Equal(false,Directory.Exists(fixture.Source));Equal("world bytes",File.ReadAllText(Path.Combine(fixture.Target,"minecraft","saves","world","level.dat")));
    }
    private static void JournalPermit() {
        using Fixture fixture=new();var imported=fixture.Import();
        using var lease=MmcAdmissionGate.Acquire(fixture.Source,imported.Definition!.InstanceId,TimeSpan.Zero);
        var primary=MmcSavedSourceStore.ReadPrimary(imported.Paths!);
        var intent=new MmcLifecycleIntent(1,Guid.NewGuid(),"Move",imported.Definition.InstanceId,fixture.Source,fixture.Target,1,2,primary.Sha256,new string('a',64),"minecraft");
        Denied(()=>MmcInstanceLifecycleJournal.Create(intent with{InstanceId=Guid.NewGuid()},lease));
        var journal=MmcInstanceLifecycleJournal.Create(intent,lease);journal.MarkPending(fixture.Source);
        Equal(MmcManagementState.PendingTransaction,MmcManagementProbe.Read(fixture.Source).State);
        Denied(()=>MmcFileTransactions.AssertNoPending(lease));
        using var permit=journal.Enter(lease);MmcFileTransactions.AssertNoPending(lease);
        string other=Path.Combine(fixture.Source,"PCL","MmcImportPending.json");File.WriteAllText(other,"{}");Denied(()=>MmcFileTransactions.AssertNoPending(lease));File.Delete(other);
        string intentFile=Path.Combine(journal.DirectoryPath,"intent.record");File.AppendAllText(intentFile,"damaged");Denied(()=>journal.Enter(lease));
    }
    private static void Denied(Action work){try{work();}catch(IOException){return;}throw new Exception("Expected exact ownership/context rejection.");}
    private static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, got {actual}.");}
    private sealed class Fixture:IDisposable {
        internal readonly string Root=Path.Combine(Path.GetTempPath(),"pcllf-mmc-lifecycle-"+Guid.NewGuid().ToString("N"));
        internal string Source=>Path.Combine(Root,"first","versions","source");
        internal string Target=>Path.Combine(Root,"second","versions","source");
        private readonly HttpClient client=new(new NoNetwork());
        internal Fixture(){
            Directory.CreateDirectory(Root);byte[] index=Encoding.UTF8.GetBytes("{\"objects\":{}}");string hash=Convert.ToHexStringLower(SHA1.HashData(index));
            using var zip=ZipFile.Open(Path.Combine(Root,"pack.zip"),ZipArchiveMode.Create);
            Add("pack/mmc-pack.json","""{"formatVersion":1,"components":[{"uid":"net.minecraft","version":"1.7.10"}]}""");
            Add("pack/patches/net.minecraft.json",$$"""{"formatVersion":1,"uid":"net.minecraft","version":"1.7.10","mainClass":"fixture.Game","mainJar":{"name":"fixture:game:1","MMC-hint":"local","MMC-filename":"main.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/index.json","sha1":"{{hash}}","size":{{index.Length}}},"compatibleJavaMajors":[8]}""");
            Add("pack/libraries/main.jar","local runtime bytes");Add("pack/minecraft/saves/world/level.dat","world bytes");
            void Add(string name,string text){using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(text);}
        }
        internal MmcInstanceBackendResult Import(){
            var backend=new MmcInstanceBackend<MmcArtifactDownloadSpec>(new(Path.Combine(Root,"metadata"),new Uri("https://metadata.invalid/"),client),
                new(Path.Combine(Root,"libs"),Path.Combine(Root,"assets"),Path.Combine(Root,"cache")),value=>value,
                async(files,token)=>{foreach(var file in files)await File.WriteAllTextAsync(file.DownloadPath,"{\"objects\":{}}",new UTF8Encoding(false),token);});
            var value=backend.ImportAsync(new(Path.Combine(Root,"pack.zip"),Source,"source",new("windows","x86_64",8))).GetAwaiter().GetResult();
            if(value.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception(value.Failure);return value;
        }
        public void Dispose(){client.Dispose();InstanceConfigProviders.FlushAndDisposeWithin(Root);if(Directory.Exists(Root))Directory.Delete(Root,true);}
    }
    private sealed class NoNetwork:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>throw new IOException("Lifecycle fixture has no network.");}
}
