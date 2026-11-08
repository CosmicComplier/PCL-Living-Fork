using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstanceLifecycleTests {
    internal static IEnumerable<(string Name,Action Run)> Cases() {
        yield return ("MMC path handoff preserves queued preferences and detaches every old provider without recreating its path", PreferenceHandoff);
        yield return ("MMC lifecycle permits require an exact published source and bound durable intent", JournalPermit);
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
