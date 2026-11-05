using System;
using System.Linq;
using System.Net.Http;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
namespace PCLCS;
public sealed class LegacySecretNetworkHost {
    public Func<string> VersionName=null!,CurseForgeKey=null!;
    public Func<int> BuildType=null!,VersionCode=null!;
    public Func<Uri?,bool> IsCurseForgeRequest=null!;
}
public sealed class LegacySecretNetwork {
    private readonly LegacySecretNetworkHost host;
    public LegacySecretNetwork(LegacySecretNetworkHost host){this.host=host;}
    public static string? CdnSign(string? url)=>!url.EndsWithF("{CDN}")?url:url!.Replace("{CDN}","").Replace(" ","%20");
    public void SignHeaders(string url,ref HttpRequestMessage request,bool browser=false,bool credentials=true) {
        if(!request.Headers.UserAgent.Any()) {
            if(url.Contains("baidupcs.com")||url.Contains("baidu.com"))request.Headers.Add("User-Agent","LogStatistic");
            else if(browser)request.Headers.Add("User-Agent",$"PCL-Living-Fork/{host.VersionName()}.{host.BuildType()} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36");
            else request.Headers.Add("User-Agent",$"PCL-Living-Fork/{host.VersionName()}.{host.BuildType()}");
        }
        if(!browser)request.Headers.Add("Referer",$"http://{host.VersionCode()}.open.pcl2.server/");
        if(credentials&&host.IsCurseForgeRequest(request.RequestUri)&&!request.Headers.Contains("x-api-key")) {string key=host.CurseForgeKey();if(!string.IsNullOrEmpty(key))request.Headers.Add("x-api-key",key);}
    }
}
