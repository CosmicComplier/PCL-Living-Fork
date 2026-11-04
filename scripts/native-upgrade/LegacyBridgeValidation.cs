using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.IO;
// Invoke only the pinned old product's genuine pure validator. No Application
// construction/entry/Startup/Settings is called and no host identity is faked.
internal static class Program {
 static string Hash(string path){using(var file=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
 static int Main(string[] args){if(args.Length!=3)return 2;ResolveEventHandler resolver=null;
  try{Assembly old=Assembly.LoadFrom(Path.GetFullPath(args[0]));ResourceManager resources=new ResourceManager("PCL.Resources",old);Dictionary<string,Assembly> loaded=new Dictionary<string,Assembly>(StringComparer.Ordinal);
   resolver=delegate(object sender,ResolveEventArgs request){string name=new AssemblyName(request.Name).Name;Assembly cached;if(loaded.TryGetValue(name,out cached))return cached;try{byte[] bytes=resources.GetObject(name) as byte[];if(bytes==null)return null;Assembly found=Assembly.Load(bytes);loaded.Add(name,found);return found;}catch{return null;}};
   AppDomain.CurrentDomain.AssemblyResolve+=resolver;
   MethodInfo validate=old.GetType("PCL.ModLauncherUpdate",true).GetMethod("ValidateExecutable",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);if(validate==null)throw new MissingMethodException("Pinned old product has no genuine validator.");
   validate.Invoke(null,new object[]{Path.GetFullPath(args[1]),Hash(args[1])});bool rawRejected=false;try{validate.Invoke(null,new object[]{Path.GetFullPath(args[2]),Hash(args[2])});}catch(TargetInvocationException error){if(!(error.InnerException is BadImageFormatException))throw;rawRejected=true;}
   if(!rawRejected)throw new InvalidOperationException("Pinned old validator unexpectedly accepts native directly; fixture baseline changed.");Console.WriteLine("PASS pinned actual legacy validator accepts canonical managed bridge and rejects raw native; no entry/profile/UI was run.");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}finally{if(resolver!=null)AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
 }
}
