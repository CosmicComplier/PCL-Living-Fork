using System.Reflection;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;
internal static partial class ModpackModeChecks {
    private static void CheckFolders(Scope scope) {
        object service = Native.Field(typeof(ModMinecraft),"Service").GetValue(null)!; object loader = Native.Get(service,"folderLoader")!;
        Type hostType = typeof(LegacyMinecraftFolderLoaderHost<ModMinecraft.McFolder>);
        FieldInfo[] fields = loader.GetType().GetFields(Native.Flags).Where(field => field.FieldType == hostType).ToArray(); Native.Check(fields.Length == 1 && loader.GetType().Assembly.GetName().Name == "PCLCS", "Actual Main folder loader/platform host not found.");
        var host = (LegacyMinecraftFolderLoaderHost<ModMinecraft.McFolder>)fields[0].GetValue(loader)!;
        string deleted = Path.Combine(scope.Root,"deleted-pack",".minecraft"), keep = Path.Combine(scope.Root,"keep",".minecraft"); Directory.CreateDirectory(deleted); Directory.Delete(deleted,false); Directory.CreateDirectory(keep);
        string catalog = "Pack>" + deleted + Path.DirectorySeparatorChar + "|Keep>" + keep + Path.DirectorySeparatorChar; scope.SetCache("LaunchFolders",catalog);
        int reads = 0, writes = 0, messages = 0; Func<string,string?> read = host.ReadString;
        scope.Replace(host,"BasePath",new Func<string>(() => scope.Launcher)); scope.Replace(host,"AppDataPath",new Func<string>(() => scope.AppData));
        scope.Replace(host,"ReadString",new Func<string,string?>(key => { if (key == "LaunchFolders") reads++; return read(key); }));
        scope.Replace(host,"WriteString",new Action<string,string?>((key,value) => { Native.Check(key == "LaunchFolders", "Unexpected folder setting write."); writes++; scope.SetCache(key,value ?? ""); }));
        scope.Replace(host,"Message",new Action<string,string,bool>((_,_,_) => messages++));
        scope.Replace(host,"CheckPermission",new Action<string>(path => { Native.Check(Native.Full(path).StartsWith(Native.Full(scope.Root) + Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase), "Folder permission fixture escaped GUID root."); if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path); }));
        scope.Replace(host,"Hint",new Action<string,bool>((_,_) => { })); scope.Replace(host,"Warn",new Action<Exception,string>((_,_) => { })); scope.Replace(host,"Error",new Action<Exception,string>((error,text) => throw new InvalidOperationException(text,error)));
        scope.Replace(host,"RandomDelay",new Action<int,int>((_,_) => throw new InvalidOperationException("Unexpected folder debug delay.")));
        scope.SetCache("LivingModpackExported",true); scope.SetCache("LivingModpackMode",true);
        for (int startup = 0; startup < 2; startup++) {
            Native.Call(typeof(ModMinecraft),"McFolderListLoadSub",null);
            F(181,messages == 0); F(182,reads == 0 && writes == 0); F(183,Equals(Cached("LaunchFolders"),catalog));
            List<ModMinecraft.McFolder> folders = ModMinecraft.McFolderList; string expected = Path.Combine(scope.Launcher,".minecraft") + Path.DirectorySeparatorChar;
            F(185,folders.Count == 1 && folders[0].Location == expected); Native.Check(folders[0].GetType().Assembly == Native.Main && Directory.Exists(Path.Combine(expected,"versions")), "Actual Main pack loader did not create its scoped versions directory.");
        }
        scope.SetCache("LivingModpackMode",false); Native.Call(typeof(ModMinecraft),"McFolderListLoadSub",null); F(190,messages == 1);
        Native.Check(reads == 1 && writes == 1 && ModMinecraft.McFolderList.Any(folder => Native.Full(folder.Location) == Native.Full(keep)), "Ordinary mode lost the actual shared-catalog read/write or surviving folder.");
        Console.WriteLine("INFO modpack folder scope uses the real Main/Core loader. The independent fixed-f4 folder-policy CodeDom oracle remains the original legacy test, not a modern oracle substitution.");
    }
}
