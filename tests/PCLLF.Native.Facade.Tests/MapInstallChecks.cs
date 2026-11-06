using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;
internal static class MapInstallChecks
{
    private static void C(int line,bool value)=>MapInstallSourceChecks.Check("map",line,value);
    private static object Kind(string name)=>Native.Enum("PCL.InstanceResourceKind",name)!;
    private static object? Store(string method,params object?[] args)=>Native.Static("PCL.InstanceResourceStore",method,args);
    private static object? Recycle(string method,params object?[] args)=>Native.Static("PCL.ResourceRecycleStore",method,args);
    private static object? Installer(string method,params object?[] args)=>Native.Static("PCL.MapDownloadInstaller",method,args);
    private static object? Value(object item,string name)=>item.GetType().GetField(name,Native.Flags) is { } field?field.GetValue(item):Native.Property(item.GetType(),name).GetValue(item);
    private static void Put(string file,string text){Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,text);}
    private static void Zip(string file,params(string Name,string Text)[] entries){Directory.CreateDirectory(Path.GetDirectoryName(file)!);using ZipArchive zip=ZipFile.Open(file,ZipArchiveMode.Create);foreach(var item in entries){using StreamWriter writer=new(zip.CreateEntry(item.Name).Open());writer.Write(item.Text);}}
    private static string Hash(string file)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
    private static Exception Rejected(Action work){try{work();}catch(Exception error){return error;}throw new InvalidOperationException("Actual map/recycle unexpectedly accepted invalid work.");}
    private static void Text(int line,string file,string expected)=>C(line,File.Exists(file)&&File.ReadAllText(file)==expected);
    private static T Part<T>(FrameworkElement owner,string name)where T:class=>owner.FindName(name)as T??throw new MissingMemberException(owner.GetType().FullName,name);
    private static void Set(object page,string name,object? value)=>Native.Property(page.GetType(),name).SetValue(page,value);
    private static IList Scan(string folder,object kind)=>(IList)Store("Scan",folder,folder,kind,CancellationToken.None)!;
    internal static void Run(FixtureEnvironment environment)
    {
        MapInstallSourceChecks.Reset("map");using MapInstallResourceScope scope=new(environment,"map-install-recycle");
        string saves=Path.Combine(scope.Root,"game","saves"),archives=(string)Installer("GetDownloadFolder",saves)!;object kind=Kind("Saves");
        C(32,archives==Path.Combine(saves,"PCLLFWorldDownload")&&Directory.Exists(archives));C(33,(string)Installer("GetDownloadFolder",saves)! ==archives);
        string flat=Path.Combine(archives,"Flat map.zip");Zip(flat,("level.dat","flat-world"),("region/data.txt","region-data"));string hash=Hash(flat),world=(string)Installer("Install",flat,saves,CancellationToken.None)!;
        C(36,world==Path.Combine(saves,"Flat map")&&!Directory.Exists(Path.Combine(archives,"Flat map")));Text(37,Path.Combine(world,"level.dat"),"flat-world");Text(37,Path.Combine(world,"region","data.txt"),"region-data");C(38,Hash(flat)==hash);
        string second=(string)Installer("Install",flat,saves,CancellationToken.None)!;C(40,world!=second);Text(40,Path.Combine(world,"level.dat"),"flat-world");
        string wrapped=Path.Combine(archives,"Wrapped.zip");Zip(wrapped,("outer/world/level.dat","wrapped-world"),("outer/world/region/data.txt","wrapped-data"),("README.txt","readme"));
        ModLoader.LoaderBase step=(ModLoader.LoaderBase)Native.Static("PCL.PageDownloadResourceDetail","CreateMapInstallLoader",wrapped,saves)!;Native.Check(step.GetType().Assembly==Native.Main,"Map install loader is not the genuine Main adapter.");scope.Track(step);
        using(CancellationTokenSource deadline=new(TimeSpan.FromSeconds(10)))step.WaitForExit(1,null,false,deadline.Token,null,true);
        Text(44,Path.Combine(saves,"Wrapped","level.dat"),"wrapped-world");
        string bad=Path.Combine(archives,"Bad.zip");Zip(bad,("../outside.txt","bad"),("level.dat","bad"));C(45,Rejected(()=>Installer("Install",bad,saves,CancellationToken.None))is not null);C(46,!File.Exists(Path.Combine(scope.Root,"outside.txt")));
        string multiple=Path.Combine(archives,"Multiple.zip");Zip(multiple,("one/level.dat","one"),("two/level.dat","two"));C(47,Rejected(()=>Installer("Install",multiple,saves,CancellationToken.None))is not null);
        string missing=Path.Combine(archives,"Not a world.zip");Zip(missing,("README.txt","not a world"));C(48,Rejected(()=>Installer("Install",missing,saves,CancellationToken.None))is not null);
        using(CancellationTokenSource canceled=new()){canceled.Cancel();C(49,Rejected(()=>Installer("Install",flat,saves,canceled.Token))is OperationCanceledException);}
        C(50,!Directory.EnumerateDirectories(Path.Combine(scope.Root,"game"),".pcl-import-*").Any());
        string trash=Path.Combine(scope.Root,"game","PCL","ResourceRecycle","Saves");IList items=Scan(saves,kind);C(55,!items.Cast<object>().Any(item=>(string)Value(item,"Name")! =="PCLLFWorldDownload"));
        Type entryType=Native.Type("PCL.InstanceResourceEntry");IList targets=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entryType))!;
        foreach(object item in items)if((bool)Value(item,"IsDirectory")!&&new[]{"Flat map","Flat map (2)","Wrapped"}.Contains((string)Value(item,"Name")!))targets.Add(item);
        Store("RecycleMany",saves,targets,trash,CancellationToken.None);IList deleted=(IList)Recycle("List",saves,trash,kind)!;C(61,deleted.Count==3);
        object first=deleted.Cast<object>().Single(item=>(string)Value(item,"Name")! =="Flat map");Recycle("Restore",saves,trash,new[]{(string)Value(first,"RecycleId")!},CancellationToken.None);Text(63,Path.Combine(saves,"Flat map","level.dat"),"flat-world");
        deleted=(IList)Recycle("List",saves,trash,kind)!;object wrappedRecord=deleted.Cast<object>().Single(item=>(string)Value(item,"Name")! =="Wrapped");Put(Path.Combine(saves,"Wrapped","level.dat"),"new-progress");
        C(66,Rejected(()=>Recycle("Restore",saves,trash,new[]{(string)Value(wrappedRecord,"RecycleId")!},CancellationToken.None))is not null);Recycle("Purge",saves,trash,new[]{(string)Value(wrappedRecord,"RecycleId")!},CancellationToken.None);Text(67,Path.Combine(saves,"Wrapped","level.dat"),"new-progress");
        C(68,Rejected(()=>Recycle("Purge",saves,trash,new[]{"../outside"},CancellationToken.None))is not null);
        string other=Path.Combine(scope.Root,"other","saves");Put(Path.Combine(other,"Other","level.dat"),"other-world");Store("RecycleMany",other,Scan(other,kind),trash,CancellationToken.None);C(71,((IList)Recycle("List",saves,trash,kind)!).Count==1);
        object otherRecord=((IList)Recycle("List",other,trash,kind)!)[0]!;C(73,Rejected(()=>Recycle("Purge",saves,trash,new[]{(string)Value(otherRecord,"RecycleId")!},CancellationToken.None))is not null);Text(74,Path.Combine((string)Value(otherRecord,"FullPath")!,"level.dat"),"other-world");
        string broken=Path.Combine(trash,Guid.NewGuid().ToString("N"));Put(Path.Combine(broken,"resource.json"),new JObject{["root"]=saves}.ToString());Put(Path.Combine(broken,"content","level.dat"),"unreadable-record");C(77,((IList)Recycle("List",saves,trash,kind)!).Count==1);Text(78,Path.Combine(broken,"content","level.dat"),"unreadable-record");
        RecyclePage(scope,saves,trash,kind);
        MapInstallSourceChecks.Complete("map");environment.AssertNoForbiddenIo();Console.WriteLine("PASS native map-install-recycle: real Main loader/install/recycle and asynchronous native page result/empty/return/layout, all original source assertions.");
    }
    private static void RecyclePage(MapInstallResourceScope scope,string saves,string trash,object kind)
    {
        Native.Static("PCL.ModSecret","ThemeRefresh",0);ModMinecraft.McInstance owner=scope.Instance();
        FrameworkElement page=(FrameworkElement)Activator.CreateInstance(Native.Type("PCL.PageInstanceResources"),Native.Flags,null,new[]{kind},null)!;scope.ObservePage(page);
        Set(page,"Owner",owner);Set(page,"RecycleRoot",trash);Set(page,"IsRecycleMode",true);Native.Call(page.GetType(),"ShowResources",page,saves,saves,Recycle("List",saves,trash,kind));
        StackPanel rows=Part<StackPanel>(page,"PanList");C(89,Part<FrameworkElement>(page,"PanWorlds").Visibility==Visibility.Collapsed&&rows.Children.Count==1);
        C(90,Part<FrameworkElement>(page,"BtnDownload").Visibility==Visibility.Collapsed&&Part<FrameworkElement>(page,"BackupSettings").Visibility==Visibility.Collapsed&&Part<FrameworkElement>(page,"BtnBackupAll").Visibility==Visibility.Visible);
        MyIconTextButton back=Part<MyIconTextButton>(page,"BtnRecycleBack");MySearchBox search=Part<MySearchBox>(page,"SearchBox");
        C(92,back.GetType()==typeof(MyIconTextButton)&&back.Visibility==Visibility.Visible&&ReferenceEquals(back.Parent,search.Parent)&&((Panel)back.Parent).Children.IndexOf(back)<((Panel)back.Parent).Children.IndexOf(search));
        C(93,Part<FrameworkElement>(page,"BtnRestore").Visibility==Visibility.Collapsed&&Part<MyButton>(page,"BtnOpenFolder").Text=="打开回收站文件夹");
        Native.Call(page.GetType(),"ChangeAllSelected",page,true);C(95,Part<FrameworkElement>(page,"BtnExport").IsEnabled&&Part<FrameworkElement>(page,"BtnDelete").IsEnabled);C(96,Part<Panel>(page,"PanAllBack").Background is null);
        Part<FrameworkElement>(page,"PanLoad").Visibility=Visibility.Collapsed;Part<FrameworkElement>(page,"PanAllBack").Visibility=Visibility.Visible;Part<FrameworkElement>(page,"PanAllBack").Opacity=1;
        Grid host=new(){Background=System.Windows.Media.Brushes.White};host.Children.Add(page);
        object? last=Native.Field(typeof(ModAnimation),"AniLastTick").GetValue(null);
        try {
            foreach(int width in new[]{540,780}) {
                host.Measure(new Size(width,620));host.Arrange(new Rect(0,0,width,620));host.UpdateLayout();Panel toolbar=Part<Panel>(page,"PanManageButtons");HashSet<(int,int)> occupied=new();FrameworkElement[] buttons=toolbar.Children.OfType<FrameworkElement>().Where(button=>button.Visibility==Visibility.Visible).ToArray();
                foreach(FrameworkElement button in buttons)C(104,occupied.Add((Grid.GetRow(button),Grid.GetColumn(button)))&&button.ActualWidth>=button.MinWidth);
                for(int i=1;i<buttons.Length;i++)if(Grid.GetRow(buttons[i-1])==Grid.GetRow(buttons[i])){double gap=buttons[i].TranslatePoint(new Point(),toolbar).X-buttons[i-1].TranslatePoint(new Point(),toolbar).X-buttons[i-1].ActualWidth;C(111,Math.Abs(gap-15)<=0.1);}
                C(115,back.TranslatePoint(new Point(),page).Y+back.ActualHeight<=search.TranslatePoint(new Point(),page).Y);
            }
            _=Part<MyCard>(page,"CardList").MainTextBlock;
            foreach(string name in new[]{"BtnRecycleBack","BtnExport","BtnDelete","BtnDeselect"}){object button=page.FindName(name)!;Native.Call(typeof(MyIconTextButton).BaseType!,"RefreshColor",button,null,false);}
            for(int i=0;i<45;i++){Native.Field(typeof(ModAnimation),"AniLastTick").SetValue(null,long.MaxValue);Native.Static("PCL.ModAnimation","AniTimer",16d);}
            host.Measure(new Size(780,620));host.Arrange(new Rect(0,0,780,620));host.UpdateLayout();FrameworkElement card=Part<FrameworkElement>(page,"CardList"),title=Part<MyCard>(page,"CardList").MainTextBlock,filter=Part<FrameworkElement>(page,"PanFilter");
            C(125,filter.TranslatePoint(new Point(),card).Y>=title.TranslatePoint(new Point(),card).Y+title.ActualHeight+5);
        }finally{try{host.Children.Clear();}finally{Native.Field(typeof(ModAnimation),"AniLastTick").SetValue(null,last);}}
        object packKind=Kind("ResourcePacks");FrameworkElement packPage=(FrameworkElement)Activator.CreateInstance(page.GetType(),Native.Flags,null,new[]{packKind},null)!;scope.ObservePage(packPage);Set(packPage,"Owner",owner);Set(packPage,"IsRecycleMode",true);
        object entry=Activator.CreateInstance(Native.Type("PCL.InstanceResourceEntry"),true)!;Native.Set(entry,"IsDirectory",false);Native.Set(entry,"FullPath",Path.Combine(scope.Root,"missing-pack.zip"));
        IList entries=(IList)Activator.CreateInstance(Native.Property(page.GetType(),"Entries").PropertyType)!;entries.Add(entry);Set(packPage,"Entries",entries);Native.Call(packPage.GetType(),"CheckUpdates",packPage);
        object packController=Native.Get(packPage,"Controller")!;C(135,!(bool)Native.Get(packController,"Checking")!&&Native.Get(packController,"OnlineCancellation")is null);
        Native.Call(page.GetType(),"RestoreRecycleSelection",page,new object?[]{null});scope.WaitUntil(()=>!(bool)Native.Get(Native.Get(page,"Controller")!,"Busy")!);C(143,!(bool)Native.Get(Native.Get(page,"Controller")!,"Busy")!);Text(144,Path.Combine(saves,"Flat map (2)","level.dat"),"flat-world");
        Array hints=(Array)Native.Call(Native.Field(typeof(ModMain),"HintWaiting").FieldType,"ToArray",Native.Field(typeof(ModMain),"HintWaiting").GetValue(null))!;
        C(146,hints.Cast<object>().Any(hint=>(string)Value(hint,"Text")! =="已还原资源"&&Value(hint,"Type")!.ToString()=="Green")&&Part<FrameworkElement>(page,"LabOperation").Visibility==Visibility.Collapsed);
        IList empty=(IList)Activator.CreateInstance(entries.GetType())!;Native.Call(page.GetType(),"ShowResources",page,saves,saves,empty);C(149,Part<TextBlock>(page,"LabEmpty").Text=="回收站为空。"&&!Part<FrameworkElement>(page,"BtnBackupAll").IsEnabled&&back.Visibility==Visibility.Visible&&Part<FrameworkElement>(page,"CardSelect").Visibility==Visibility.Collapsed);
        FrameworkElement sortHeader=Part<FrameworkElement>(page,"PanSortHeader"),filterPanel=Part<FrameworkElement>(page,"PanFilter");
        Native.Check(ReferenceEquals(filterPanel.Parent,sortHeader)&&sortHeader.Margin.Top==40&&filterPanel.Margin==new Thickness(0)&&!Part<FrameworkElement>(page,"ComboSort").IsEnabled,
            "Recycle mode must keep the shared filter/sort header below its heading and disable sorting.");
        Native.Call(page.GetType(),"ReturnToResources",page);Native.Call(page.GetType(),"ShowResources",page,saves,saves,Scan(saves,kind));
        // The shared header now owns the original outer spacing; the nested filter has no extra margin.
        C(153,back.Visibility==Visibility.Collapsed&&Part<FrameworkElement>(page,"BtnRestore").Visibility==Visibility.Visible&&Part<MyButton>(page,"BtnOpenFolder").Text=="打开文件夹"&&Part<FrameworkElement>(page,"BackupSettings").Visibility==Visibility.Visible&&Part<FrameworkElement>(page,"PanWorlds").Visibility==Visibility.Visible&&sortHeader.Margin.Top==13&&filterPanel.Margin==new Thickness(0));
        Native.Check(Part<FrameworkElement>(page,"ComboSort").IsEnabled,"Returning from recycle mode must restore sorting.");
    }
}
