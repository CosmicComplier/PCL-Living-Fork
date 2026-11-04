param([Parameter(Mandatory=$true)][string]$ExecutablePath,[Parameter(Mandatory=$true)][string]$RepositoryRoot)
# Windows/.NET Framework/STA only. Actual original owners, no startup, network, credential reads or hardware calls.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Net.Http
$root=[IO.Path]::GetFullPath($RepositoryRoot)
$paths=@('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','presentation/bin/PCLLF.Presentation.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')
foreach($path in $paths){[Reflection.Assembly]::LoadFrom((Join-Path $root $path))|Out-Null}
$asm=[Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ExecutablePath))
$source=@'
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL;
public static class LfCustomEventDpChecks {
 private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run() {
  Type owner=typeof(CustomEventService),evt=typeof(CustomEvent);
  DependencyProperty[] attached={CustomEventService.EventsProperty,CustomEventService.EventTypeProperty,CustomEventService.EventDataProperty};
  string[] names={"Events","EventType","EventData"};
  for(int i=0;i<attached.Length;i++){
   Assert(attached[i].OwnerType==owner,"Attached DP owner changed: "+names[i]);
   var descriptor=DependencyPropertyDescriptor.FromName(names[i],owner,typeof(Button));
   Assert(descriptor!=null&&Object.ReferenceEquals(descriptor.DependencyProperty,attached[i]),"Original attached owner lookup failed: "+names[i]);
  }
  Assert(CustomEventService.EventTypeProperty.PropertyType==typeof(CustomEvent.EventType)&&CustomEventService.EventTypeProperty.DefaultMetadata.DefaultValue.Equals(CustomEvent.EventType.None),"Attached enum/default identity changed");
  Assert(CustomEvent.TypeProperty.OwnerType==evt&&CustomEvent.TypeProperty.PropertyType==typeof(CustomEvent.EventType)&&CustomEvent.TypeProperty.DefaultMetadata.DefaultValue.Equals(CustomEvent.EventType.None),"Object enum/default owner changed");
  Assert(CustomEvent.DataProperty.OwnerType==evt&&CustomEvent.DataProperty.DefaultMetadata.DefaultValue==null&&CustomEventService.EventDataProperty.DefaultMetadata.DefaultValue==null,"Data owner/default changed");
  foreach(var name in new[]{"Type","Data"})Assert(DependencyPropertyDescriptor.FromName(name,evt,evt)!=null,"Original CustomEvent DP lookup failed: "+name);
  Button first=new Button(),second=new Button();CustomEventCollection a=CustomEventService.GetEvents(first),b=CustomEventService.GetEvents(second);
  Assert(Object.ReferenceEquals(a,CustomEventService.GetEvents(first))&&!Object.ReferenceEquals(a,b),"Events collection is shared or replaced");
  var field=typeof(CustomEventCollection).GetField("_Events",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
  Assert(field!=null&&Object.ReferenceEquals(field.GetValue(a),a.Events),"Original private list identity changed");
  var item=new CustomEvent(CustomEvent.EventType.复制文本,"fixture");a.Events.Add(item);Assert(a.Count()==1&&((IEnumerable)a).Cast<CustomEvent>().Single()==item,"Original collection enumerators lost item identity");
  Assert(item.GetValue(CustomEvent.TypeProperty).Equals(CustomEvent.EventType.复制文本)&&(string)item.GetValue(CustomEvent.DataProperty)=="fixture","Real event constructor/property store changed");
  string local="clr-namespace:PCL;assembly="+evt.Assembly.GetName().Name;
  var parsed=(Button)XamlReader.Parse("<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='"+local+"' local:CustomEventService.EventType='复制文本' local:CustomEventService.EventData='raw|tokens' />");
  Assert(CustomEventService.GetEventType(parsed)==CustomEvent.EventType.复制文本&&CustomEventService.GetEventData(parsed)=="raw|tokens","Actual attached XAML enum/data lookup failed");
  var style=(Style)XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='"+local+"' TargetType='Button'><Setter Property='local:CustomEventService.EventType' Value='复制文本'/><Setter Property='local:CustomEventService.EventData' Value='styled'/></Style>");
  var styled=new Button{Style=style};Assert(Object.ReferenceEquals(((Setter)style.Setters[0]).Property,CustomEventService.EventTypeProperty)&&CustomEventService.GetEventType(styled)==CustomEvent.EventType.复制文本&&CustomEventService.GetEventData(styled)=="styled","Actual old-owner Setter lookup failed");
  var collection=(Button)XamlReader.Parse("<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='"+local+"'><local:CustomEventService.Events><local:CustomEventCollection><local:CustomEvent Type='复制文本' Data='first'/><local:CustomEvent Type='刷新帮助' Data='second'/></local:CustomEventCollection></local:CustomEventService.Events></Button>");
  var values=CustomEventService.GetEvents(collection).Events;Assert(values.Count==2&&values[0].GetValue(CustomEvent.TypeProperty).Equals(CustomEvent.EventType.复制文本)&&(string)values[1].GetValue(CustomEvent.DataProperty)=="second","Actual collection ContentProperty XAML failed");
 }
 public static void Headers(Assembly assembly){
  var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;var type=assembly.GetType("PCL.ModSecret",true);var method=type.GetMethod("SecretHeadersSign",flags);var parameters=method.GetParameters();
  Assert(parameters.Length==4&&parameters[1].ParameterType.IsByRef&&parameters[2].DefaultValue.Equals(false)&&parameters[3].DefaultValue.Equals(true),"Original signer ByRef/Optional ABI changed");
  using(var request=new HttpRequestMessage(HttpMethod.Get,"https://api.curseforge.com/v1/mods")){
   object[] args={"https://api.curseforge.com/v1/mods",request,false,false};method.Invoke(null,args);
   Assert(Object.ReferenceEquals(args[1],request)&&!request.Headers.Contains("x-api-key")&&request.Headers.UserAgent.ToString().StartsWith("PCL-Living-Fork/"),"Actual LF identity/ByRef/explicit no-credential behavior changed");
  }
  using(var request=new HttpRequestMessage(HttpMethod.Get,"https://edge.forgecdn.net/files/fixture")){
   request.Headers.Add("User-Agent","Fixture/1");object[] args={"https://api.curseforge.com/v1/mods",request,true,true};method.Invoke(null,args);
   Assert(!request.Headers.Contains("x-api-key")&&!request.Headers.Contains("Referer")&&request.Headers.UserAgent.ToString()=="Fixture/1","Actual URI guard/browser/existing UA changed");
  }
  using(var request=new HttpRequestMessage(HttpMethod.Get,"https://api.curseforge.com/v1/mods")){
   request.Headers.Add("x-api-key","synthetic-existing");object[] args={"https://api.curseforge.com/v1/mods",request,true,true};method.Invoke(null,args);
   Assert(request.Headers.GetValues("x-api-key").Single()=="synthetic-existing","Existing request key was replaced");
  }
  var cdn=type.GetMethod("SecretCdnSign",flags);Assert((string)cdn.Invoke(null,new object[]{"https://fixture.invalid/a b{CDN}"})=="https://fixture.invalid/a%20b","Actual CDN marker behavior changed");
 }
}
'@
$refs=@([IO.Path]::GetFullPath($ExecutablePath),(Join-Path $root 'PCLCS/bin/PCLCS.dll'),(Join-Path $root 'presentation/bin/PCLLF.Presentation.dll'),(Join-Path $root 'MeloongCore/Shared/bin/MeloongCore.dll'),[Windows.DependencyObject].Assembly.Location,[Windows.Controls.Button].Assembly.Location,[Windows.Media.Visual].Assembly.Location,[Windows.Markup.XamlReader].Assembly.Location,[System.Xaml.XamlServices].Assembly.Location,[Net.Http.HttpRequestMessage].Assembly.Location,'System.dll','System.Core.dll')
Add-Type -TypeDefinition $source -ReferencedAssemblies $refs
[LfCustomEventDpChecks]::Run()
[LfCustomEventDpChecks]::Headers($asm)
Write-Output 'PASS actual LF custom-event five DP original owners/default/enum identity, private collection storage/enumerators, attached/Setter/collection XAML lookup; actual signer ByRef/Optional/LF UA/URI guard/explicit no-credentials and CDN marker.'
