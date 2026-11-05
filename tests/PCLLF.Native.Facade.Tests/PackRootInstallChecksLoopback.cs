using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace PCLLF.Native.Facade.Tests;

// Exact immutable synthetic endpoints only; no proxy, redirect, external DNS or
// supplied path is served. Supports the genuine LF downloader's normal ranges.
internal sealed class PackRootInstallChecksLoopback : IDisposable
{
    private readonly TcpListener listener=new(IPAddress.Loopback,0);
    private readonly CancellationTokenSource stop=new();
    private sealed record Payload(byte[] Bytes,ManualResetEventSlim? Entered,ManualResetEventSlim? Release);
    private readonly ConcurrentDictionary<string,Payload> files=new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string,int> requested=new(StringComparer.Ordinal);
    private readonly ConcurrentBag<TcpClient> clients=new();
    private readonly ConcurrentBag<Task> responses=new();
    private readonly Task accept;
    private readonly string origin;
    private int invalid;
    internal PackRootInstallChecksLoopback(){listener.Start();origin="http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port;accept=Task.Run(Accept);}
    internal string Url(string path)=>origin+path;
    internal void Add(string path,byte[] bytes,ManualResetEventSlim? entered=null,ManualResetEventSlim? release=null){Native.Check(path.StartsWith('/')&&!path.Contains('?')&&files.TryAdd(path,new(bytes.ToArray(),entered,release)),"Duplicate/invalid owned HTTP route.");}
    internal bool Requested(string path)=>requested.ContainsKey(path);
    internal bool Owns(string value)=>Uri.TryCreate(value,UriKind.Absolute,out Uri? uri)&&uri.GetLeftPart(UriPartial.Authority)==origin&&uri.Query.Length==0&&files.ContainsKey(uri.AbsolutePath);
    private async Task Accept(){try{while(true){TcpClient client=await listener.AcceptTcpClientAsync(stop.Token).ConfigureAwait(false);clients.Add(client);responses.Add(Task.Run(()=>Respond(client)));}}catch(OperationCanceledException)when(stop.IsCancellationRequested){}catch(SocketException)when(stop.IsCancellationRequested){}catch(ObjectDisposedException)when(stop.IsCancellationRequested){}}
    private void Respond(TcpClient client)
    {
        using(client)try{
            client.ReceiveTimeout=5000;client.SendTimeout=5000;NetworkStream stream=client.GetStream();List<byte> header=new();int value;
            while((value=stream.ReadByte())>=0){header.Add((byte)value);if(header.Count>16384)throw new InvalidDataException("Owned HTTP header exceeded limit.");if(header.Count>=4&&header.TakeLast(4).SequenceEqual(new byte[]{13,10,13,10}))break;}
            string text=Encoding.ASCII.GetString(header.ToArray());string[] first=text.Split("\r\n",2)[0].Split(' ');
            if(first.Length!=3||first[0]!="GET"||!files.TryGetValue(first[1],out Payload? payload)){Interlocked.Increment(ref invalid);throw new InvalidDataException("Unexpected loopback request.");}
            requested.AddOrUpdate(first[1],1,(_,count)=>count+1);payload.Entered?.Set();if(payload.Release is not null&&!payload.Release.Wait(10000,stop.Token))throw new TimeoutException("Owned HTTP phase was not released.");byte[] bytes=payload.Bytes;
            Match range=Regex.Match(text,@"(?im)^Range:\s*bytes=(\d+)-");long begin=range.Success?long.Parse(range.Groups[1].Value,CultureInfo.InvariantCulture):0;
            if(begin<0||begin>=bytes.Length){Interlocked.Increment(ref invalid);throw new InvalidDataException("Invalid loopback range.");}
            string status=range.Success?"206 Partial Content":"200 OK";
            string reply="HTTP/1.1 "+status+"\r\nContent-Length: "+(bytes.Length-begin)+"\r\nAccept-Ranges: bytes\r\n"+(range.Success?"Content-Range: bytes "+begin+"-"+(bytes.Length-1)+"/"+bytes.Length+"\r\n":"")+"Connection: close\r\n\r\n";
            stream.Write(Encoding.ASCII.GetBytes(reply));stream.Write(bytes,(int)begin,bytes.Length-(int)begin);
        }catch(OperationCanceledException)when(stop.IsCancellationRequested){}catch(IOException){/* Real cancel/resume can close a response. */}catch(SocketException){}catch(ObjectDisposedException)when(stop.IsCancellationRequested){}
    }
    public void Dispose(){stop.Cancel();listener.Stop();foreach(var client in clients)client.Dispose();Native.Check(accept.Wait(2000),"Owned HMCL listener did not stop.");foreach(var client in clients)client.Dispose();Native.Check(Task.WaitAll(responses.ToArray(),2000),"Owned HMCL response did not stop.");Native.Check(Volatile.Read(ref invalid)==0,"Owned HMCL listener saw an unexpected request.");stop.Dispose();}
}
