// Exact synthetic transport C# from LF50 Test-ModDependencyOnline.ps1, LF LICENCE.
// This is a fixture, not a production transport or dependency implementation.
#nullable disable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;
public sealed class ModDependencyOnlineFixtureNetwork {
 public readonly List<string> Calls=new List<string>();
 public readonly List<string> Bodies=new List<string>();
 public readonly List<string> Unexpected=new List<string>();
 public readonly List<int> ProgressCounts=new List<int>();
 public void Progress(int done,int total){if(done>total)throw new InvalidOperationException("Invalid indexing progress");ProgressCounts.Add(done);}
 public JToken Versions=new JObject(), Fingerprints=JObject.Parse("{\"data\":{\"exactMatches\":[]}}");
 public JToken Projects=new JArray(), CfProjects=JObject.Parse("{\"data\":[]}"), VersionProjects=new JArray();
 public JToken Search=JObject.Parse("{\"hits\":[]}"), CfSearch=JObject.Parse("{\"data\":[]}");
 public DateTime UtcNow=new DateTime(2026,10,25,12,0,0,DateTimeKind.Utc);
 public bool Fail, Delay;
 public Exception Failure;
 public CancellationTokenSource Cancel;
 public DateTime Clock(){return UtcNow;}
 public JToken Request(string url,HttpMethod method,string body,CancellationToken token){
  token.ThrowIfCancellationRequested();
  Calls.Add(url);Bodies.Add(body);
  if(Cancel!=null){Cancel.Cancel();token.ThrowIfCancellationRequested();}
  if(Delay){token.WaitHandle.WaitOne(2000);token.ThrowIfCancellationRequested();}
  if(Failure!=null)throw Failure;
  if(Fail)throw new IOException("Synthetic offline failure");
  JToken value=null;
  if(url=="https://api.modrinth.com/v2/version_files")value=Versions;
  else if(url.StartsWith("https://api.modrinth.com/v2/projects?"))value=Projects;
  else if(url.StartsWith("https://api.modrinth.com/v2/versions?"))value=VersionProjects;
  else if(url=="https://api.curseforge.com/v1/fingerprints/432")value=Fingerprints;
  else if(url=="https://api.curseforge.com/v1/mods")value=CfProjects;
  else if(url.StartsWith("https://api.modrinth.com/v2/search?"))value=Search;
  else if(url.StartsWith("https://api.curseforge.com/v1/mods/search?"))value=CfSearch;
  else {Unexpected.Add(url);throw new InvalidOperationException("Unexpected mocked API address");}
  return value.DeepClone();
 }
 public static uint Fingerprint(string path){
  byte[] bytes=File.ReadAllBytes(path).Where(b=>b!=9&&b!=10&&b!=13&&b!=32).ToArray();
  unchecked {
   uint h=1u^(uint)bytes.Length;int i=0;
   for(;i+4<=bytes.Length;i+=4){
    uint k=(uint)(bytes[i]|bytes[i+1]<<8|bytes[i+2]<<16|bytes[i+3]<<24);
    k*=0x5bd1e995;k^=k>>24;k*=0x5bd1e995;h*=0x5bd1e995;h^=k;
   }
   switch(bytes.Length-i){case 3:h^=(uint)bytes[i+2]<<16;goto case 2;case 2:h^=(uint)bytes[i+1]<<8;goto case 1;case 1:h^=bytes[i];h*=0x5bd1e995;break;}
   h^=h>>13;h*=0x5bd1e995;return h^(h>>15);
  }
 }
}
