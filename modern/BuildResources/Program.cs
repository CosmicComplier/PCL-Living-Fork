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
if(options.Keys.Any(key=>key is not ("lf-project" or "output" or "schema" or "native-project"))) throw new ArgumentException("Unknown resource generator option.");
string lf=Need("lf-project"),output=Need("output");
// --lf-project is retained as the fixed vendor-resource root; no old project/source is read.
string schemaPath=options.ContainsKey("schema")?Need("schema"):Path.Combine(AppContext.BaseDirectory,"LF.Resources.schema.json");
NativeResourceSchema schema=JsonSerializer.Deserialize<NativeResourceSchema>(File.ReadAllText(schemaPath),new JsonSerializerOptions {
    PropertyNameCaseInsensitive=true,UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
})??throw new InvalidDataException("Missing native resource schema.");
if(schema.Version!=2 || schema.Basename!="PCL.Resources" || string.IsNullOrWhiteSpace(schema.NativeMainProject) ||
   schema.Payloads?.Length!=5 || schema.TypedProperties?.Length!=5 || schema.Provenance.ValueKind!=JsonValueKind.Object ||
   schema.ExcludedKeys is null || schema.ExcludedKeys.Length!=14 || !new HashSet<string>(schema.ExcludedKeys,StringComparer.Ordinal).SetEquals(new[]{"MeloongCore.Wpf","CacheCow.Client","CacheCow.Client.FileStore","CacheCow.Common","ThrottleDebounce","Imazen.WebP","MeloongCore","Microsoft.Win32.Registry","NAudio","Newtonsoft.Json","Ookii.Dialogs.Wpf","PCLCS","PCLLF.Presentation","System.Net.Http.Formatting"}))
    throw new InvalidDataException("Native LF resource schema changed: review all keys/interfaces explicitly.");
string projectPath=options.ContainsKey("native-project")?Need("native-project"):Path.GetFullPath(Path.Combine(lf,schema.NativeMainProject));
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
var values=new Dictionary<string,byte[]>(StringComparer.Ordinal);
var records=new List<object>();
foreach(ResourcePayload payload in schema.Payloads) {
    if(payload is null || payload.Key is not ("Custom" or "ForgeInstaller" or "Help" or "libwebp64" or "Transformer")) throw new InvalidDataException("Invalid native LF payload key.");
    string key=payload.Key;
    if(schema.ExcludedKeys.Contains(key,StringComparer.Ordinal) || string.IsNullOrWhiteSpace(payload.Source)) throw new InvalidDataException("Invalid native LF payload key.");
    string source=ResolveFile(Path.Combine(lf,payload.Source!));
    byte[] bytes=File.ReadAllBytes(source);if(bytes.Length==0) throw new InvalidDataException("Empty resource: "+key);
    if(!values.TryAdd(key,bytes)) throw new InvalidDataException("Duplicate LF key: "+key);
    records.Add(new{key,action="fixed-LF-bytes",source,bytes=bytes.Length,sha256=Hash(bytes)});
}
if(values.Count!=5) throw new InvalidDataException("Native LF resource schema changed: review all keys explicitly.");
foreach(string key in schema.ExcludedKeys) records.Add(new{key,action="resolved-by-native-host",reason="Managed dependencies use the normal .NET dependency graph; no duplicate managed payload is embedded."});
Directory.CreateDirectory(output);
using(ResourceWriter writer=new(Path.Combine(output,"PCL.Resources.resources"))) {foreach(var pair in values) writer.AddResource(pair.Key,pair.Value);writer.Generate();}
XDocument selfContained=new(new XElement("root",
    new XElement("resheader",new XAttribute("name","resmimetype"),new XElement("value","text/microsoft-resx")),
    new XElement("resheader",new XAttribute("name","version"),new XElement("value","2.0")),
    new XElement("resheader",new XAttribute("name","reader"),new XElement("value","System.Resources.ResXResourceReader, System.Windows.Forms")),
    new XElement("resheader",new XAttribute("name","writer"),new XElement("value","System.Resources.ResXResourceWriter, System.Windows.Forms"))));
foreach(var pair in values) selfContained.Root!.Add(new XElement("data",new XAttribute("name",pair.Key),new XAttribute("type","System.Byte[], mscorlib"),new XElement("value",Convert.ToBase64String(pair.Value))));
selfContained.Save(Path.Combine(output,"Resources.resx"));
var properties=new Dictionary<string,string>(StringComparer.Ordinal);
foreach(ResourceProperty property in schema.TypedProperties) {
    if(property is null || string.IsNullOrWhiteSpace(property.Name) || !Regex.IsMatch(property.Name,@"^[A-Za-z_][A-Za-z0-9_]*$") ||
       property.Key is null || !values.ContainsKey(property.Key) || !properties.TryAdd(property.Name,property.Key)) throw new InvalidDataException("Invalid native typed resource interface.");
}
if(properties.Values.Distinct(StringComparer.Ordinal).Count()!=values.Count) throw new InvalidDataException("Native typed resource keys must cover every actual payload once.");
StringBuilder code=new("// Generated from explicit native LF.Resources.schema.json; repository LICENCE.\n#nullable enable\nnamespace PCL.My.Resources;\n[global::System.CodeDom.Compiler.GeneratedCode(\"PCLLF.ModernResources\",\"1.0\")]\n[global::System.Diagnostics.DebuggerNonUserCode]\n[global::System.Runtime.CompilerServices.CompilerGenerated]\ninternal static class Resources\n{\n    private static global::System.Resources.ResourceManager? resourceMan;\n    private static global::System.Globalization.CultureInfo? resourceCulture;\n    internal static global::System.Resources.ResourceManager ResourceManager\n    {\n        get\n        {\n            if (global::System.Object.ReferenceEquals(resourceMan, null))\n            {\n                var temp = new global::System.Resources.ResourceManager(\"PCL.Resources\", typeof(Resources).Assembly);\n                resourceMan = temp;\n            }\n            return resourceMan!;\n        }\n    }\n    internal static global::System.Globalization.CultureInfo? Culture\n    {\n        get => resourceCulture;\n        set => resourceCulture = value;\n    }\n");
foreach(var pair in properties) code.Append("    internal static byte[] ").Append(pair.Key).Append(" => (byte[])ResourceManager.GetObject(\"").Append(pair.Value).Append("\", resourceCulture)!;\n");
code.Append("}\n");File.WriteAllText(Path.Combine(output,"Resources.Designer.cs"),code.ToString(),new UTF8Encoding(false));
XDocument project=XDocument.Load(projectPath);
var logicalNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
XElement assetGroup=new("ItemGroup");var assets=new List<object>();
foreach(XElement item in project.Descendants().Where(element=>element.Name.LocalName=="Resource")) {
    string include=(string?)item.Attribute("Include")??throw new InvalidDataException("Native Resource requires an explicit Include.");
    string logical=item.Elements().FirstOrDefault(element=>element.Name.LocalName=="Link")?.Value??include;
    logical=logical.Replace('\\','/');
    if(string.IsNullOrWhiteSpace(include) || include.IndexOfAny(new[]{'*','?','$','%'})>=0 ||
       item.AncestorsAndSelf().Attributes("Condition").Any() || !logicalNames.Add(logical)) throw new InvalidDataException("Ambiguous or conditional native resource inventory: "+include);
    string source=ResolveFile(Path.Combine(Path.GetDirectoryName(projectPath)!,include));byte[] bytes=File.ReadAllBytes(source);
    if(bytes.Length==0) throw new InvalidDataException("Empty native pack asset: "+logical);
    assetGroup.Add(new XElement("Resource",new XAttribute("Include",source),new XElement("Link",logical)));
    assets.Add(new{logical,source,bytes=bytes.Length,sha256=Hash(bytes)});
}
if(assets.Count==0) throw new InvalidDataException("Native resource inventory is empty.");
new XDocument(new XElement("Project",assetGroup)).Save(Path.Combine(output,"LF.PackResources.props"));
File.WriteAllText(Path.Combine(output,"resource-manifest.json"),JsonSerializer.Serialize(new{basename="PCL.Resources",nativeSchemaSha256=Hash(File.ReadAllBytes(schemaPath)),nativeMainProjectSha256=Hash(File.ReadAllBytes(projectPath)),provenance=schema.Provenance,resourceCount=values.Count,typedProperties=properties,payloads=records,packAssets=assets},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Generated {values.Count} actual byte[] resources and {assets.Count} native WPF pack assets; managed dependencies excluded.");

return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
    return 1;
}

sealed record NativeResourceSchema(int Version,string Basename,string NativeMainProject,JsonElement Provenance,ResourcePayload[] Payloads,ResourceProperty[] TypedProperties,string[] ExcludedKeys);
sealed record ResourcePayload(string Key,string? Source);
sealed record ResourceProperty(string Name,string Key);
