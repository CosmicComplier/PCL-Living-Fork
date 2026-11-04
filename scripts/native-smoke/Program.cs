using System.Buffers.Binary;
using System.Collections;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PCL;

// Trusted CI artifact preflight only. Candidate code, Main, WPF and Settings are
// never loaded or invoked. The production bounded bundle reader is linked once.
if(args.Length!=1)throw new ArgumentException("Provide the actual published native Main EXE.");
try {
 string path=Path.GetFullPath(args[0]);var identity=ModernBundleIdentity.Read(path);
 if(identity.AssemblyName!="LivingPCL" || identity.Product!="PCL Living Fork")throw new InvalidDataException("Not the normal LF native product.");
 using FileStream file=new(path,FileMode.Open,FileAccess.Read,FileShare.Read);
 byte[] bytes=new byte[checked((int)identity.EntrySize)];file.Position=identity.EntryOffset;file.ReadExactly(bytes);
 using MemoryStream image=new(bytes,false);using PEReader pe=new(image);MetadataReader metadata=pe.GetMetadataReader();
 int token=pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress;
 MethodDefinition main=metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(token&0xffffff));
 TypeDefinition entryType=metadata.GetTypeDefinition(main.GetDeclaringType());
 string name=metadata.GetString(entryType.Namespace)+"."+metadata.GetString(entryType.Name),method=metadata.GetString(main.Name);
 if(name!="PCL.Application" || method!="Main")throw new InvalidDataException("Actual entry is "+name+"."+method+"; a fixture/test host is not the product Main.");
 object Constant(string typeName,string fieldName) {
  foreach(TypeDefinitionHandle handle in metadata.TypeDefinitions) {
   TypeDefinition type=metadata.GetTypeDefinition(handle);if(metadata.GetString(type.Namespace)!="PCL" || metadata.GetString(type.Name)!=typeName)continue;
   foreach(FieldDefinitionHandle fieldHandle in type.GetFields()) {
    FieldDefinition field=metadata.GetFieldDefinition(fieldHandle);if(metadata.GetString(field.Name)!=fieldName)continue;
    ConstantHandle value=field.GetDefaultValue();if(value.IsNil)throw new InvalidDataException("Main field is not a constant: "+fieldName);
    Constant constant=metadata.GetConstant(value);byte[] raw=metadata.GetBlobBytes(constant.Value);
    return constant.TypeCode switch {ConstantTypeCode.String=>Encoding.Unicode.GetString(raw),ConstantTypeCode.Int32=>BinaryPrimitives.ReadInt32LittleEndian(raw),_=>throw new InvalidDataException("Unexpected Main constant type.")};
   }
  }
  throw new InvalidDataException("Actual Main constant is missing: "+typeName+"."+fieldName);
 }
 string version=(string)Constant("ModBase","VersionBaseName"),display=(string)Constant("ModBase","LauncherDisplayName"),registry=(string)Constant("ModSecret","RegFolder"),branch=(string)Constant("ModSecret","VersionBranchMain");
 if(version!=identity.ProductVersion || display!="PCL Living Fork" || registry!="PCLDebug" || branch!="OpenSource")throw new InvalidDataException("Actual Main version/profile/source branch differs from this LF smoke contract.");
 var resources=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal);
 foreach(ManifestResourceHandle handle in metadata.ManifestResources) {
  ManifestResource resource=metadata.GetManifestResource(handle);if(!resource.Implementation.IsNil)continue;
  string resourceName=metadata.GetString(resource.Name);if(resourceName!="PCL.Resources.resources" && !resourceName.EndsWith(".g.resources",StringComparison.Ordinal))continue;
  var directory=pe.PEHeaders.CorHeader.ResourcesDirectory;var block=pe.GetSectionData(directory.RelativeVirtualAddress);int offset=checked((int)resource.Offset);
  if(offset<0 || offset>directory.Size-4)throw new InvalidDataException("Invalid managed resource offset.");
  int length=BinaryPrimitives.ReadInt32LittleEndian(block.GetContent(offset,4).AsSpan());
  if(length<0 || checked((long)offset+4+length)>directory.Size)throw new InvalidDataException("Invalid managed resource span.");
  using MemoryStream raw=new(block.GetContent(offset+4,length).ToArray(),false);using ResourceReader reader=new(raw);
  var keys=new HashSet<string>(StringComparer.Ordinal);IDictionaryEnumerator enumerator=reader.GetEnumerator();while(enumerator.MoveNext())keys.Add((string)enumerator.Key);
  resources.Add(resourceName,keys);
 }
 if(!resources.TryGetValue("PCL.Resources.resources",out var payloads))throw new InvalidDataException("Actual LF ResourceManager resource is missing.");
 foreach(string key in new[]{"PCLCS","PCLLF.Presentation","MeloongCore","Newtonsoft.Json"})if(!payloads.Contains(key))throw new InvalidDataException("Actual Main runtime resource missing: "+key);
 var wpf=resources.Where(pair=>pair.Key.EndsWith(".g.resources",StringComparison.Ordinal)).Select(pair=>pair.Value).Single();
 var normalized=new HashSet<string>(wpf.Select(key=>key.ToLowerInvariant()),StringComparer.Ordinal);
 string[] required={"application.baml","formmain.baml","pages/pagelaunch/pagelaunchleft.baml","pages/pagelaunch/pagelaunchright.baml","pages/pagesetup/pagesetupui.baml","resources/font.ttf","images/branding/livingpcl.ico"};
 foreach(string key in required)if(!normalized.Contains(key))throw new InvalidDataException("Actual product BAML/font/brand resource missing: "+key);
 file.Position=0;string hash=Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
 Console.WriteLine(JsonSerializer.Serialize(new{nativeIdentity=identity,entryType=name,entryMethod=method,physicalPath=path,sha256=hash,versionBaseName=version,displayName=display,registryFolder=registry,sourceBranch=branch,versionCode=Constant("ModBase","VersionCode"),buildType=Constant("ModBase","BuildType"),verifiedWpfKeys=required}));
 return 0;
}catch(Exception error){Console.Error.WriteLine(error.GetType().Name+": "+error.Message);return 1;}
