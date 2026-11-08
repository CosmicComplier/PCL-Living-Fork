using System.IO;
using System.IO.Compression;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;
using PCLLF.MMCC.Core;

var selected=args is ["--case",var requested]?requested:null;
foreach(var test in ExportChecks.Cases().Where(test=>selected is null||test.Name.Contains(selected,StringComparison.OrdinalIgnoreCase))) {
    try { test.Run(); Console.WriteLine("PASS "+test.Name); }
    catch(Exception error) {Console.Error.WriteLine("FAIL "+test.Name+": "+error);return 1;}
}
Console.WriteLine("Controlled managed import/export/reimport evidence; no Minecraft/JVM/game or external network.");return 0;

internal static class ExportChecks {
    internal static IEnumerable<(string Name,Action Run)> Cases() {
        yield return ("managed MMC current environment and fixed remote/local sources round-trip for Java8/21",RoundTrip);
        yield return ("explicit player options, separate roots, privacy and unadopted library refusal",OptionsAndRoles);
        yield return ("export cancellation drains writer; stale/mutated/colliding outputs preserve existing data",Failures);
        yield return ("portable preferences preserve empty JVM override and disclose machine-dependent conversions",Preferences);
    }
    private static void RoundTrip() {
        foreach(int major in new[]{8,21}) using(var fixture=new Fixture(major)) {
            var imported=fixture.Import("source");var paths=imported.Paths!;
            using var provider=new InstanceConfigProvider(paths.InstanceRoot);
            MmcIniConfiguration.QueuePreferenceBatch(provider,new Dictionary<string,string?>{["MmcJvmArgsMode"]="Custom",["VersionAdvanceJvm"]=""});
            fixture.Put(paths.GameRoot,"config/personal.cfg","edited current configuration");
            fixture.Put(paths.GameRoot,"libraries/same.jar","game-root library bytes");
            fixture.Put(paths.GameRoot,"saves/world/level.dat","private world");fixture.Put(paths.GameRoot,"screenshots/private.png","private screenshot");fixture.Put(paths.GameRoot,"servers.dat","private server list");
            fixture.Put(paths.GameRoot,"accounts.json","DO NOT EXPORT SECRET");fixture.Put(paths.GameRoot,"config/launcher_accounts.json","DO NOT EXPORT SECRET");
            string global=Path.Combine(fixture.Root,"global-libraries");Directory.CreateDirectory(global);File.WriteAllText(Path.Combine(global,"poison.jar"),"mutable global cache is not an export source");
            var request=fixture.Request(imported,"roundtrip.zip");var exporter=new MmcInstanceExportBackend();
            var preview=exporter.PreviewAsync(request).GetAwaiter().GetResult();
            var result=exporter.ExportAsync(request with {ExpectedPreviewFingerprint=preview.Fingerprint}).GetAwaiter().GetResult();Success(result);
            using(var zip=ZipFile.OpenRead(result.OutputZipPath!)) {
                string[] names=zip.Entries.Select(e=>e.FullName).ToArray();
                Check(!names.Any(n=>n.StartsWith("PCL/",StringComparison.OrdinalIgnoreCase)||n.Contains("/saves/",StringComparison.OrdinalIgnoreCase)||n.Contains("screenshots",StringComparison.OrdinalIgnoreCase)||n.EndsWith("servers.dat",StringComparison.OrdinalIgnoreCase)||n.EndsWith("accounts.json",StringComparison.OrdinalIgnoreCase)),"Private/host data escaped export.");
                Check(Read(zip,"minecraft/config/personal.cfg")=="edited current configuration","Current game config was not exported.");
                Check(Read(zip,"libraries/same.jar")=="private adopted library"&&Read(zip,"minecraft/libraries/same.jar")=="game-root library bytes","Three-root library roles were merged.");
                Check(names.Contains("patches/fixture.remote.json"),"Adopted remote component was not frozen locally.");
                string cfg=Read(zip,"instance.cfg");Check(cfg.Contains("OverrideJavaArgs=\"true\"")&&cfg.Contains("JvmArgs=\"\""),"Explicit empty JVM override became inheritance.");
                Check(!cfg.Contains("JavaPath")&&!cfg.Contains(paths.InstanceRoot),"Host Java/root identity leaked into portable cfg.");
            }
            var reimported=fixture.Import("target",result.OutputZipPath!);Check(reimported.Definition!.InstanceId!=imported.Definition!.InstanceId&&reimported.Definition.ParentInstanceId is null,"Export/reimport reused the LF identity.");
            var a=imported.BoundPlan!.CompiledProfile;var b=reimported.BoundPlan!.CompiledProfile;
            Check(a.MainClass==b.MainClass&&a.JvmArguments.Select(x=>x.Value).SequenceEqual(b.JvmArguments.Select(x=>x.Value))&&a.GameArguments.Select(x=>x.Value).SequenceEqual(b.GameArguments.Select(x=>x.Value))&&a.Tweakers.SequenceEqual(b.Tweakers),"Launch entry/args/tweakers changed.");
            Check(a.Classpath.Select(x=>x.Coordinate!.Name).SequenceEqual(b.Classpath.Select(x=>x.Coordinate!.Name)),"Classpath order changed.");
            Check(a.JavaPolicy.AllowedMajors.SequenceEqual(b.JavaPolicy.AllowedMajors),"Java declaration changed.");
            using var reProvider=new InstanceConfigProvider(reimported.Paths!.InstanceRoot);
            Check(MmcIniConfiguration.CapturePreferences(reProvider).Ini.GetValueOrDefault("MmcJvmArgsMode")=="UsePackage","Empty portable package override did not retain its local role after reimport.");
        }
    }
    private static void OptionsAndRoles() {
        using var fixture=new Fixture(8);var source=fixture.Import("source");
        fixture.Put(source.Paths!.GameRoot,"saves/world/level.dat","world");fixture.Put(source.Paths.GameRoot,"screenshots/p.png","shot");fixture.Put(source.Paths.GameRoot,"servers.dat","servers");
        var all=new MmcInstanceExportBackend().ExportAsync(fixture.Request(source,"with-player.zip",new(true,true,true))).GetAwaiter().GetResult();Success(all);
        using(var zip=ZipFile.OpenRead(all.OutputZipPath!))Check(zip.GetEntry("minecraft/saves/world/level.dat") is not null&&zip.GetEntry("minecraft/screenshots/p.png") is not null&&zip.GetEntry("minecraft/servers.dat") is not null,"Explicit player options were ignored.");
        fixture.Put(source.Paths.InstanceRoot,".minecraft/config/inactive.cfg","separate inactive role");
        Check(new MmcInstanceExportBackend().ExportAsync(fixture.Request(source,"dual.zip")).GetAwaiter().GetResult().Status==MmcInstanceExportStatus.Rejected,"Inactive data silently merged or disappeared.");
        var excluded=new MmcInstanceExportBackend().ExportAsync(fixture.Request(source,"active-only.zip",new(){InactiveGameTree=MmcExportInactiveTree.Exclude})).GetAwaiter().GetResult();Success(excluded);
        fixture.Put(source.Paths.InstanceRoot,"libraries/unadopted.jar","unadopted");
        Check(new MmcInstanceExportBackend().ExportAsync(fixture.Request(source,"unknown-library.zip",new(){InactiveGameTree=MmcExportInactiveTree.Exclude})).GetAwaiter().GetResult().Status==MmcInstanceExportStatus.Rejected,"Unadopted private library was promoted into a pack.");
    }
    private static void Failures() {
        using var fixture=new Fixture(8);var source=fixture.Import("source");
        foreach(string mode in new[]{"cancel","source-change","collision","stale","inside","existing","original","bad-extension","missing-parent"}) {
            var request=fixture.Request(source,mode+".zip");using var cancel=new CancellationTokenSource();
            if(mode=="stale")request=request with {ExpectedSourceGeneration=source.Definition!.Generation+1};
            if(mode=="inside")request=request with {OutputZipPath=Path.Combine(source.Paths!.GameRoot,"nested.zip")};
            if(mode=="existing")File.WriteAllText(request.OutputZipPath,"keep existing");
            if(mode=="original")request=request with {OutputZipPath=fixture.Archive};
            if(mode=="bad-extension")request=request with {OutputZipPath=Path.Combine(fixture.Root,"not-a-zip.dat")};
            if(mode=="missing-parent")request=request with {OutputZipPath=Path.Combine(fixture.Root,"missing-parent","output.zip")};
            request=request with {Checkpoint=(phase,_)=>{
                if(mode=="cancel"&&phase==MmcInstanceExportCheckpoint.EntryWritten)cancel.Cancel();
                if(mode=="source-change"&&phase==MmcInstanceExportCheckpoint.BeforePublication)fixture.Put(source.Paths!.GameRoot,"config/personal.cfg","changed at publication boundary");
                if(mode=="collision"&&phase==MmcInstanceExportCheckpoint.BeforePublication)File.WriteAllText(request.OutputZipPath,"competing creator");
            }};
            var result=new MmcInstanceExportBackend().ExportAsync(request,cancel.Token).GetAwaiter().GetResult();
            Check(result.Status!=MmcInstanceExportStatus.Completed,"Unsafe/cancelled export was published: "+mode);
            if(mode=="cancel") {Check(result.Status==MmcInstanceExportStatus.Cancelled,"Cancellation status lost.");if(result.RetainedPartialFile is not null)using(new FileStream(result.RetainedPartialFile,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){} }
            if(mode=="collision")Check(File.ReadAllText(request.OutputZipPath)=="competing creator","Competing output was overwritten.");
            if(mode=="existing")Check(File.ReadAllText(request.OutputZipPath)=="keep existing","Existing ZIP was overwritten.");
            if(mode=="original")Check(File.Exists(fixture.Archive),"Original archive was removed.");
        }
    }
    private static void Preferences() {
        using var fixture=new Fixture(8);var source=fixture.Import("source");using var provider=new InstanceConfigProvider(source.Paths!.InstanceRoot);
        MmcIniConfiguration.QueuePreferenceBatch(provider,new Dictionary<string,string?>{["MmcJavaLocationMode"]="Custom",["VersionArgumentJavaV2"]="1",["VersionArgumentJavaRange"]="[8,8]",["MmcMemoryMode"]="Custom",["VersionRamType"]="0"});
        var exporter=new MmcInstanceExportBackend();Check(exporter.ExportAsync(fixture.Request(source,"machine.zip")).GetAwaiter().GetResult().Status==MmcInstanceExportStatus.Rejected,"Machine-dependent choices were silently exported.");
        var options=new MmcInstanceExportOptions(){DiscardMachineJavaSelection=true,PortableMinimumMemoryMiB=512,PortableMaximumMemoryMiB=2048};
        var portable=exporter.ExportAsync(fixture.Request(source,"portable.zip",options)).GetAwaiter().GetResult();Success(portable);Check(portable.Preview!.Transformations.Any(x=>x.Contains("Java selection"))&&portable.Preview.Transformations.Any(x=>x.Contains("memory policy")),"Portable losses were not recorded in preview.");
    }
    private static string Read(ZipArchive zip,string path){using var reader=new StreamReader(zip.GetEntry(path)!.Open());return reader.ReadToEnd();}
    private static void Success(MmcInstanceExportResult result){if(result.Status!=MmcInstanceExportStatus.Completed)throw new Exception(result.Status+": "+result.Failure);}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private sealed class Fixture:IDisposable {
        internal string Root{get;}=Path.Combine(Path.GetTempPath(),"PCLLF-MmcExportTests-"+Guid.NewGuid().ToString("N"));
        internal string Archive=>Path.Combine(Root,"original.zip");private readonly HttpClient client;private readonly int major;
        private readonly byte[] index=Encoding.UTF8.GetBytes("{\"objects\":{}}");private readonly byte[] shared=JarBytes();
        internal Fixture(int major) {
            this.major=major;Directory.CreateDirectory(Root);File.WriteAllText(Path.Combine(Root,"export-test-owner.json"),"PCLLF owned export fixture / "+Path.GetFileName(Root));Directory.CreateDirectory(Path.Combine(Root,"outputs"));
            string indexSha=Convert.ToHexStringLower(SHA1.HashData(index));string sharedSha=Convert.ToHexStringLower(SHA1.HashData(shared));
            string remote=new JObject{["formatVersion"]=1,["uid"]="fixture.remote",["version"]="1",["+jvmArgs"]=new JArray("-Dremote=yes"),["libraries"]=new JArray(new JObject{["name"]="fixture:shared:1",["downloads"]=new JObject{["artifact"]=new JObject{["url"]="https://download.invalid/shared.jar",["sha1"]=sharedSha,["size"]=shared.Length}}})}.ToString(Newtonsoft.Json.Formatting.None);
            client=new HttpClient(new Metadata(remote));
            using var zip=ZipFile.Open(Archive,ZipArchiveMode.Create);
            Add("mmc-pack.json","""{"formatVersion":1,"components":[{"uid":"net.minecraft","version":"1.7.10"},{"uid":"fixture.remote","version":"1"}]}""");
            Add("instance.cfg","name=Source\nOverrideJavaArgs=true\nJvmArgs=-Dpackage=yes\nOverrideMemory=true\nMinMemAlloc=512\nMaxMemAlloc=2048\n");
            Add("patches/net.minecraft.json",new JObject{["uid"]="net.minecraft",["version"]="1.7.10",["mainClass"]="fixture.Game",["minecraftArguments"]="--sample value",["+jvmArgs"]=new JArray("-Dcustom=yes"),["+tweakers"]=new JArray("fixture.Tweak"),["compatibleJavaMajors"]=major==8?new JArray(8):new JArray(17,21),["mainJar"]=new JObject{["name"]="fixture:client:1",["MMC-hint"]="local",["MMC-filename"]="client.jar"},["libraries"]=new JArray(new JObject{["name"]="fixture:local:1",["MMC-hint"]="local",["MMC-filename"]="same.jar"}),["assetIndex"]=new JObject{["id"]="fixture",["url"]="https://download.invalid/index.json",["sha1"]=indexSha,["size"]=index.Length}}.ToString(Newtonsoft.Json.Formatting.None));
            AddBytes("libraries/client.jar",JarBytes());Add("libraries/same.jar","private adopted library");Add("libraries/orphan.jar","preserved inactive library");Add("minecraft/config/personal.cfg","author configuration");
            void Add(string path,string value)=>AddBytes(path,Encoding.UTF8.GetBytes(value));
            void AddBytes(string path,byte[] value){using var output=zip.CreateEntry(path).Open();output.Write(value);}
        }
        internal MmcInstanceBackendResult Import(string name,string? archive=null) {
            string root=Path.Combine(Root,name+"-cache");
            var backend=new MmcInstanceBackend<MmcArtifactDownloadSpec>(new MmcMetadataStore(Path.Combine(root,"metadata"),new Uri("https://metadata.invalid/v1/"),client),new(Path.Combine(root,"libraries"),Path.Combine(root,"assets"),Path.Combine(root,"sources")),spec=>spec,
                async(files,token)=>{foreach(var file in files){Directory.CreateDirectory(Path.GetDirectoryName(file.DownloadPath)!);await File.WriteAllBytesAsync(file.DownloadPath,file.Artifact.Role==MmcArtifactRole.AssetIndex?index:shared,token);}});
            var result=backend.ImportAsync(new(archive??Archive,Path.Combine(Root,"game","versions",name),name,new("windows","x86_64",major))).GetAwaiter().GetResult();
            if(result.Status!=MmcInstanceBackendStatus.Succeeded)throw new Exception("Controlled import failed: "+result.Failure);return result;
        }
        internal MmcInstanceExportRequest Request(MmcInstanceBackendResult source,string file,MmcInstanceExportOptions? options=null)=>new(source.Paths!.InstanceRoot,Path.Combine(Root,"outputs",file),"Portable Instance",source.Definition!.InstanceId,source.Definition.Generation,MmcSavedSourceStore.ReadPrimary(source.Paths).Sha256,options??new());
        internal void Put(string root,string relative,string value){string path=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,value);}
        private static byte[] JarBytes(){using var memory=new MemoryStream();using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true)){using var stream=zip.CreateEntry("fixture-owned-resource.txt").Open();stream.Write(Encoding.UTF8.GetBytes("self-authored library payload"));}return memory.ToArray();}
        public void Dispose(){client.Dispose();InstanceConfigProviders.FlushAndDisposeWithin(Root);Console.WriteLine("RETAINED owned export fixture: "+Root);}

    }
    private sealed class Metadata(string json):HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json,Encoding.UTF8,"application/json")});
    }
}
