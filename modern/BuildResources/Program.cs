using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

try
{
var options = new Dictionary<string,string>(StringComparer.Ordinal);
for(int i=0;i<args.Length;i+=2) {
    if(i+1>=args.Length || !args[i].StartsWith("--",StringComparison.Ordinal) || !options.TryAdd(args[i][2..],args[i+1])) throw new ArgumentException("Expected unique --name value pairs.");
}
string Need(string name)=>Path.GetFullPath(options.TryGetValue(name,out string? value)?value:throw new ArgumentException("Missing --"+name));
string lf=Need("lf-project"),output=Need("output"),core=Need("core"),presentation=Need("presentation"),shared=Need("shared"),json=Need("json");
string registry=options.ContainsKey("registry")?Need("registry"):OperatingSystem.IsWindows()?Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),"Microsoft.Win32.Registry.dll"):throw new ArgumentException("On a non-Windows host, --registry must identify the actual Windows .NET10 implementation asset.");
string resxPath=Path.Combine(lf,"My Project","Resources.resx"),designerPath=Path.Combine(lf,"My Project","Resources.Designer.vb"),projectPath=Path.Combine(lf,"PCL-Living-Fork.vbproj");
XDocument original=XDocument.Load(resxPath,LoadOptions.PreserveWhitespace);
string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
string ResolveFile(string path) {
    path=Path.GetFullPath(path.Replace('\\',Path.DirectorySeparatorChar));
    if(File.Exists(path)) return path;
    string current=Path.GetPathRoot(path)!;
    foreach(string part in path[current.Length..].Split(Path.DirectorySeparatorChar,StringSplitOptions.RemoveEmptyEntries)) {
        string[] candidates=Directory.GetFileSystemEntries(current).Where(entry=>string.Equals(Path.GetFileName(entry),part,StringComparison.OrdinalIgnoreCase)).ToArray();
        if(candidates.Length!=1) throw new IOException($"Missing or ambiguous LF path {path} at {part}.");
        current=candidates[0];
    }
    if(!File.Exists(current)) throw new FileNotFoundException("LF resource is not a file.",current);
    return current;
}
(string? Assembly,string? Framework) Metadata(string path) {
    using FileStream stream=File.OpenRead(path);using PEReader pe=new(stream);
    if(!pe.HasMetadata) return(null,null);
    MetadataReader reader=pe.GetMetadataReader();string? framework=null;
    foreach(CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes()) {
        CustomAttribute attribute=reader.GetCustomAttribute(handle);EntityHandle parent;
        if(attribute.Constructor.Kind==HandleKind.MemberReference) parent=reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        else if(attribute.Constructor.Kind==HandleKind.MethodDefinition) parent=reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        else continue;
        string name=parent.Kind==HandleKind.TypeReference?reader.GetString(reader.GetTypeReference((TypeReferenceHandle)parent).Name):parent.Kind==HandleKind.TypeDefinition?reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)parent).Name):"";
        if(name=="ReferenceAssemblyAttribute") throw new InvalidDataException("Do not embed a reference assembly: "+path);
        if(name=="TargetFrameworkAttribute") {BlobReader blob=reader.GetBlobReader(attribute.Value);if(blob.ReadUInt16()!=1) throw new BadImageFormatException("Bad TargetFramework attribute.");framework=blob.ReadSerializedString();}
    }
    return(AssemblyName.GetAssemblyName(path).FullName,framework);
}
var dynamicPaths=new Dictionary<string,string>(StringComparer.Ordinal){{"PCLCS",core},{"PCLLF.Presentation",presentation},{"MeloongCore",shared},{"Newtonsoft.Json",json},{"Microsoft.Win32.Registry",registry}};
var requiredFramework=new Dictionary<string,string>(StringComparer.Ordinal){{"PCLCS",".NETCoreApp,Version=v10.0"},{"PCLLF.Presentation",".NETCoreApp,Version=v10.0"},{"MeloongCore",".NETStandard,Version=v2.0"},{"Microsoft.Win32.Registry",".NETCoreApp,Version=v10.0"}};
var values=new Dictionary<string,byte[]>(StringComparer.Ordinal);
var records=new List<object>();
foreach(XElement data in original.Root!.Elements("data")) {
    string key=(string?)data.Attribute("name")??throw new InvalidDataException("Unnamed LF key.");
    string reference=data.Element("value")?.Value??throw new InvalidDataException("Missing LF value.");
    string[] parts=reference.Split(';');
    if((string?)data.Attribute("type")!="System.Resources.ResXFileRef, System.Windows.Forms" || parts.Length!=2 || !parts[1].StartsWith("System.Byte[]",StringComparison.Ordinal)) throw new InvalidDataException("Unsupported LF resource type: "+key);
    if(key=="MeloongCore.Wpf") {records.Add(new{key,action="excluded-old-framework-only-assembly",reason="No old MeloongCore.Wpf in native modern app; original product resx remains unchanged."});continue;}
    bool generated=dynamicPaths.TryGetValue(key,out string? source);source=ResolveFile(generated?source!:Path.Combine(Path.GetDirectoryName(resxPath)!,parts[0]));
    byte[] bytes=File.ReadAllBytes(source);if(bytes.Length==0) throw new InvalidDataException("Empty resource: "+key);
    if(!values.TryAdd(key,bytes)) throw new InvalidDataException("Duplicate LF key: "+key);
    (string? assembly,string? framework)=Path.GetExtension(source).Equals(".dll",StringComparison.OrdinalIgnoreCase)?Metadata(source):(null,null);
    if(requiredFramework.TryGetValue(key,out string? expected) && framework!=expected) throw new InvalidDataException($"{key} must use {expected}, found {framework}.");
    if(generated && key!="MeloongCore" && AssemblyName.GetAssemblyName(source).Name!=key) throw new InvalidDataException("Wrong actual assembly identity for "+key);
    if(key=="MeloongCore" && AssemblyName.GetAssemblyName(source).Name!="MeloongCore") throw new InvalidDataException("Wrong Shared identity.");
    if(key=="Newtonsoft.Json" && System.Diagnostics.FileVersionInfo.GetVersionInfo(source).FileVersion?.StartsWith("13.0.3.",StringComparison.Ordinal)!=true) throw new InvalidDataException("Modern Newtonsoft must be exact 13.0.3.");
    records.Add(new{key,action=generated?"actual-modern-artifact":"fixed-LF-bytes",source,bytes=bytes.Length,sha256=Hash(bytes),assembly,framework});
}
if(values.Count!=18 || original.Root!.Elements("data").Count()!=19) throw new InvalidDataException("LF resource schema changed: review all keys explicitly.");
Directory.CreateDirectory(output);
using(ResourceWriter writer=new(Path.Combine(output,"PCL.Resources.resources"))) {foreach(var pair in values) writer.AddResource(pair.Key,pair.Value);writer.Generate();}
XDocument selfContained=new(original);
foreach(XElement data in selfContained.Root!.Elements("data").ToArray()) {
    string key=(string)data.Attribute("name")!;
    if(!values.TryGetValue(key,out byte[]? bytes)) {data.Remove();continue;}
    data.SetAttributeValue("type","System.Byte[], mscorlib");data.SetAttributeValue("mimetype",null);data.Element("value")!.Value=Convert.ToBase64String(bytes);
}
selfContained.Save(Path.Combine(output,"Resources.resx"));
var properties=new Dictionary<string,string>(StringComparer.Ordinal);
foreach(Match match in Regex.Matches(File.ReadAllText(designerPath),@"Friend ReadOnly Property\s+(\w+)\(\)\s+As Byte\(\).*?GetObject\(""([^""]+)""",RegexOptions.Singleline)) {
    string name=match.Groups[1].Value,key=match.Groups[2].Value;
    if(key=="MeloongCore.Wpf") continue;
    if(!values.ContainsKey(key) || !properties.TryAdd(name,key)) throw new InvalidDataException("Invalid original typed interface: "+name);
}
foreach(string key in values.Keys) if(!properties.ContainsValue(key)) {string name=key.Replace('.','_');if(!properties.TryAdd(name,key)) throw new InvalidDataException("Conflicting resource property: "+name);}
StringBuilder code=new("// Generated from actual LF Resources.resx/Resources.Designer.vb; repository LICENCE.\n#nullable enable\nnamespace PCL.My.Resources;\n[global::System.CodeDom.Compiler.GeneratedCode(\"PCLLF.ModernResources\",\"1.0\")]\n[global::System.Diagnostics.DebuggerNonUserCode]\n[global::System.Runtime.CompilerServices.CompilerGenerated]\ninternal static class Resources\n{\n    private static global::System.Resources.ResourceManager? resourceMan;\n    private static global::System.Globalization.CultureInfo? resourceCulture;\n    internal static global::System.Resources.ResourceManager ResourceManager\n    {\n        get\n        {\n            if (global::System.Object.ReferenceEquals(resourceMan, null))\n            {\n                var temp = new global::System.Resources.ResourceManager(\"PCL.Resources\", typeof(Resources).Assembly);\n                resourceMan = temp;\n            }\n            return resourceMan!;\n        }\n    }\n    internal static global::System.Globalization.CultureInfo? Culture\n    {\n        get => resourceCulture;\n        set => resourceCulture = value;\n    }\n");
foreach(var pair in properties) code.Append("    internal static byte[] ").Append(pair.Key).Append(" => (byte[])ResourceManager.GetObject(\"").Append(pair.Value).Append("\", resourceCulture)!;\n");
code.Append("}\n");File.WriteAllText(Path.Combine(output,"Resources.Designer.cs"),code.ToString(),new UTF8Encoding(false));
XNamespace ns="http://schemas.microsoft.com/developer/msbuild/2003";XDocument project=XDocument.Load(projectPath);
XElement assetGroup=new("ItemGroup");var assets=new List<object>();
foreach(XElement item in project.Descendants(ns+"Resource")) {
    string logical=(string)item.Attribute("Include")!;string source=ResolveFile(Path.Combine(lf,logical));byte[] bytes=File.ReadAllBytes(source);
    assetGroup.Add(new XElement("Resource",new XAttribute("Include",source),new XElement("Link",logical.Replace('\\','/'))));
    assets.Add(new{logical=logical.Replace('\\','/'),source,bytes=bytes.Length,sha256=Hash(bytes)});
}
new XDocument(new XElement("Project",assetGroup)).Save(Path.Combine(output,"LF.PackResources.props"));
File.WriteAllText(Path.Combine(output,"resource-manifest.json"),JsonSerializer.Serialize(new{basename="PCL.Resources",originalResxSha256=Hash(File.ReadAllBytes(resxPath)),originalDesignerSha256=Hash(File.ReadAllBytes(designerPath)),resourceCount=values.Count,typedProperties=properties,payloads=records,packAssets=assets},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Generated {values.Count} actual byte[] resources and {assets.Count} original WPF pack assets; only old MeloongCore.Wpf excluded.");

return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
    return 1;
}
