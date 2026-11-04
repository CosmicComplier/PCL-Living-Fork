param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory)
# A separate fixture EXE owns AppContext.BaseDirectory, so real PCL settings are never written.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclSourceSettings-'+[Guid]::NewGuid().ToString('N'))
$framework=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8'
$references=@('System.dll','System.Core.dll','System.Xaml.dll',(Join-Path $framework 'WindowsBase.dll'),(Join-Path $framework 'PresentationFramework.dll'),(Join-Path $framework 'PresentationCore.dll'))
$source=@'
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
public static class SourceSettingsCheck {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static Assembly asm;static Type settings;static IDictionary entries;static PropertyInfo animation;
 static object Call(Type t,string n,params object[] args){return t.GetMethods(F).Single(m=>m.Name==n&&!m.IsGenericMethod&&m.GetParameters().Length==args.Length).Invoke(null,args);}
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static IDictionary Cache(){var entry=entries["ToolLauncherUpdateSource"];return (IDictionary)entry.GetType().GetField("ValueCache",F).GetValue(entry);}
 static object NewPage(){animation.SetValue(null,1,null);try{return Activator.CreateInstance(asm.GetType("PCL.PageSetupSystem"));}finally{animation.SetValue(null,0,null);}}
 static ComboBox Combo(object page){return (ComboBox)((FrameworkElement)page).FindName("ComboLauncherUpdateSource");}
 static string Preference(){return Call(asm.GetType("PCL.ModLauncherUpdate"),"PreferredUpdateSource").ToString();}
 static void Clear(){Cache().Remove("");Call(asm.GetType("PCL.ModBase"),"IniClearCache","Setup");}
 static void Render(FrameworkElement element,string directory,string name){
  if(string.IsNullOrEmpty(directory))return;Directory.CreateDirectory(directory);element.UpdateLayout();
  int width=(int)Math.Ceiling(element.ActualWidth),height=(int)Math.Ceiling(element.ActualHeight);
  var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
  var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(directory,name)))encoder.Save(file);
 }
 [STAThread] public static int Main(string[] args){Application app=null;Window window=null;
  try{
   AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
   string exe=args[0],root=args[1],preview=args.Length>2?args[2]:"";
   string basis=AppContext.BaseDirectory;Require(Path.GetFileName(basis.TrimEnd('\\')).StartsWith("PclSourceSettings-"),"Settings fixture has unsafe base directory");
   foreach(var file in new[]{"MeloongCore/Shared/bin/MeloongCore.dll","MeloongCore/Wpf/bin/MeloongCore.Wpf.dll","PCLCS/bin/PCLCS.dll","Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll","Plain Craft Launcher 2/Resources/ThrottleDebounce.dll"})Assembly.LoadFrom(Path.Combine(root,file));
   asm=Assembly.LoadFrom(exe);settings=asm.GetType("PCL.Settings");entries=(IDictionary)settings.GetField("Entries",F).GetValue(null);
   foreach(DictionaryEntry pair in entries){var entry=pair.Value;var cache=(IDictionary)entry.GetType().GetField("ValueCache",F).GetValue(entry);cache[""]=entry.GetType().GetField("DefaultValue",F).GetValue(entry);}
   var identify=entries["Identify"];((IDictionary)identify.GetType().GetField("ValueCache",F).GetValue(identify))[""]="source-settings-fixture";
   typeof(Application).GetField("_resourceAssembly",F).SetValue(null,asm);app=(Application)Activator.CreateInstance(asm.GetType("PCL.Application"),true);
   app.GetType().GetMethod("InitializeComponent").Invoke(app,null);
   typeof(Application).GetField("_startupUri",F).SetValue(app,null);
   app.Startup-=(StartupEventHandler)Delegate.CreateDelegate(typeof(StartupEventHandler),app,app.GetType().GetMethod("Application_Startup",F));
   app.DispatcherUnhandledException-=(System.Windows.Threading.DispatcherUnhandledExceptionEventHandler)Delegate.CreateDelegate(typeof(System.Windows.Threading.DispatcherUnhandledExceptionEventHandler),app,app.GetType().GetMethod("Application_DispatcherUnhandledException",F));
   app.ShutdownMode=ShutdownMode.OnExplicitShutdown;animation=asm.GetType("PCL.ModAnimation").GetProperty("AniControlEnabled",F);
   Call(asm.GetType("PCL.ModSecret"),"ThemeRefresh",0);
   var entrySource=entries["ToolLauncherUpdateSource"];Require(Convert.ToInt32(entrySource.GetType().GetField("DefaultValue").GetValue(entrySource))==0,"First-use source is not CNB");
   var page=NewPage();var combo=Combo(page);Require(combo.Items.Count==2&&combo.SelectedIndex==0&&Preference()=="Cnb","Default source is not displayed as CNB");
   Require((string)Call(asm.GetType("PCL.SettingService"),"GetKey",combo)=="ToolLauncherUpdateSource","Native source control is not bound to the persistent preference");
   DependencyObject current=combo;while(current!=null&&current.GetType().Name!="MyCard")current=LogicalTreeHelper.GetParent(current);
   var card=(FrameworkElement)current;Require(card!=null,"Launcher card not found");((Panel)LogicalTreeHelper.GetParent(card)).Children.Remove(card);
   card.Margin=new Thickness(15);card.VerticalAlignment=VerticalAlignment.Top;window=new Window{Content=card,Width=760,Height=320,Left=-30000,Top=-30000,ShowInTaskbar=false,ShowActivated=false};window.Show();window.UpdateLayout();
   Render(card,preview,"launcher-source-cnb.png");
   combo.SelectedIndex=1;window.UpdateLayout();
   string ini=Path.Combine(basis,"PCL","Setup.ini");Require(File.Exists(ini)&&File.ReadAllText(ini).Contains("ToolLauncherUpdateSource:1"),"GitHub preference was not persisted in the isolated Setup.ini");
   Clear();Require(Preference()=="GitHub","Saved preference was not reread from disk");
   var reopened=NewPage();Require(Combo(reopened).SelectedIndex==1&&Preference()=="GitHub","Reopening settings overwrote the saved GitHub choice");
   Render(card,preview,"launcher-source-github.png");
   var updater=asm.GetType("PCL.ModLauncherUpdate");var sourceType=asm.GetType("PCL.ModLauncherUpdate+UpdateSource");
   var prompt=(FrameworkElement)Activator.CreateInstance(asm.GetType("PCL.ModLauncherUpdate+UpdatePromptView"),F,null,new object[]{"v7.8.9",Enum.ToObject(sourceType,1)},null);
   var promptPicker=(ComboBox)prompt.GetType().GetField("SourcePicker",F).GetValue(prompt);
   Require(promptPicker.SelectedIndex==1,"Update prompt did not inherit the default source");
   promptPicker.SelectedIndex=0;Require(prompt.GetType().GetProperty("SelectedSource",F).GetValue(prompt,null).ToString()=="Cnb","Temporary update choice was ignored");
   Require(Preference()=="GitHub"&&File.ReadAllText(ini).Contains("ToolLauncherUpdateSource:1"),"Per-attempt update choice overwrote the saved source");
   window.Content=prompt;prompt.Margin=new Thickness(25);window.Height=260;window.UpdateLayout();Require(promptPicker.ActualHeight>=28,"Source picker has no usable click area");Render(prompt,preview,"launcher-update-prompt.png");
   Require(promptPicker.Style!=null&&promptPicker.Template!=null,"Update source picker did not receive the native PCL style");
   window.Content=null;prompt.Margin=new Thickness(0);
   var converter=Activator.CreateInstance(asm.GetType("PCL.ModMain+MyMsgBoxConverter"));
   converter.GetType().GetField("Title",F).SetValue(converter,"发现 PCL Living Fork 新版本");
   converter.GetType().GetField("Text",F).SetValue(converter,"请选择本次更新的下载源。");
   converter.GetType().GetField("Button1",F).SetValue(converter,"下载并更新");converter.GetType().GetField("Button2",F).SetValue(converter,"暂不更新");
   converter.GetType().GetField("ContentFactory",F).SetValue(converter,new Func<FrameworkElement>(()=>prompt));
   var modal=(FrameworkElement)Activator.CreateInstance(asm.GetType("PCL.MyMsgText"),new object[]{converter});
   ((RotateTransform)modal.FindName("TransformRotate")).Angle=0;((TranslateTransform)modal.FindName("TransformPos")).Y=0;
   modal.HorizontalAlignment=HorizontalAlignment.Left;modal.VerticalAlignment=VerticalAlignment.Top;modal.Margin=new Thickness(0);
   modal.Measure(new Size(650,400));modal.Arrange(new Rect(0,0,650,400));Render(modal,preview,"launcher-update-dialog.png");
   var checker=Activator.CreateInstance(updater.GetMethod("CreateUpdateDownloadTask",F).GetParameters()[2].ParameterType);
   var work=Call(updater,"CreateUpdateDownloadTask","https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/v7.8.9/PCL-Living-Fork-7.8.9.exe",Path.Combine(basis,"fixture-update.exe"),checker);
   Require(work.GetType().GetField("Name",F).GetValue(work).ToString().Contains("v7.8.9"),"Native update task lost the selected version");
   var children=(IList)work.GetType().GetField("Loaders",F).GetValue(work);
   Require(children.Count==1&&children[0].GetType().Name=="LoaderDownload","Launcher update was not wired to native download progress");
   Call(asm.GetType("PCL.SettingService"),"ResetSettings",combo);Clear();Call(asm.GetType("PCL.SettingService"),"RefreshSettings",combo);
   Require(combo.SelectedIndex==0&&Preference()=="Cnb","Reset did not restore the CNB default");
   Clear();Require(Combo(NewPage()).SelectedIndex==0,"Reset was not persisted");
   Console.WriteLine("PASS: native CNB/GitHub selector, default CNB, isolated disk persistence, reopening/reset, per-attempt popup selection without persistence, native update download task; no real launcher/settings touched.");return 0;
  }catch(Exception error){Console.Error.WriteLine(error.GetType().FullName+": "+error.Message);return 1;}finally{if(window!=null)window.Close();if(app!=null)app.Shutdown();}
 }
}
'@
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 $path=Join-Path $fixture 'Check.cs';[IO.File]::WriteAllText($path,$source)
 $output=Join-Path $fixture 'SettingsCheck.exe';$compiler=Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
 $arguments=@('/nologo','/target:exe',('/out:'+$output),('/lib:'+$framework))+@($references|ForEach-Object {'/reference:'+$_})
 & $compiler @arguments $path
 if($LASTEXITCODE -ne 0){throw 'Settings fixture compilation failed'}
 Copy-Item -LiteralPath ($ExecutablePath+'.config') -Destination ($output+'.config')
 & $output $ExecutablePath $root $PreviewDirectory
 if($LASTEXITCODE -ne 0){throw 'Launcher update settings validation failed'}
}finally{
 $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclSourceSettings-[a-f0-9]{32}$'){throw 'Unsafe settings fixture cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
