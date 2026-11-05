param([Parameter(Mandatory=$true)][string]$ExecutablePath,[string]$PreviewDirectory,[switch]$ProgressOnly,[int]$ColorMode=0)
# Native controls and a throttled loopback download; no real game directories or external network.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
$root=Split-Path $PSScriptRoot -Parent
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Net.Http
$references=@($ExecutablePath,'System.dll','System.Core.dll','System.Net.Http.dll','System.Xaml.dll',[Windows.Application].Assembly.Location,[Windows.Media.Brushes].Assembly.Location,[Windows.Threading.DispatcherObject].Assembly.Location)
foreach($path in @('MeloongCore/Shared/bin/MeloongCore.dll','MeloongCore/Wpf/bin/MeloongCore.Wpf.dll','PCLCS/bin/PCLCS.dll','presentation/bin/PCLLF.Presentation.dll','Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll','Plain Craft Launcher 2/Resources/ThrottleDebounce.dll')){
 $file=Join-Path $root $path;[Reflection.Assembly]::LoadFrom($file)|Out-Null;$references+=$file
}
$references+=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'
$asm=[Reflection.Assembly]::LoadFrom($ExecutablePath);$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$settings=$asm.GetType('PCL.Settings').GetField('Entries').GetValue($null)
foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='modpack-update-ux-fixture'
$settings['UiLauncherColorMode'].ValueCache['']=$ColorMode
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application'),$true);$app.InitializeComponent()
$asm.GetType('PCL.ModSecret').GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
if($ColorMode -eq 1 -and $app.Resources['ColorBrushWhite'].Color.R -gt 100){throw 'Dark preview did not apply the dark palette'}
[Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
$app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
$app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
$app.ShutdownMode=[Windows.ShutdownMode]::OnExplicitShutdown
[Windows.Application]::ResourceAssembly=$asm
$fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclPackUpdateUx-'+[Guid]::NewGuid().ToString('N'))
$source=@'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL;
public static class PackUpdateProgressCheck {
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
 public static string Run(Assembly asm,string folder){
  Directory.CreateDirectory(folder);
  var bytes=new byte[2*1024*1024];new Random(731).NextBytes(bytes);
  string hash;using(var sha=SHA1.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
  int port=((IPEndPoint)listener.LocalEndpoint).Port,sent=0;
  var server=Task.Run(()=>{try{while(true){var client=listener.AcceptTcpClient();Task.Run(()=>{
   using(client){try{var stream=client.GetStream();var request=new List<byte>();int current;
    while((current=stream.ReadByte())>=0){request.Add((byte)current);int n=request.Count;if(n>3&&request[n-4]==13&&request[n-3]==10&&request[n-2]==13&&request[n-1]==10)break;}
    string text=Encoding.ASCII.GetString(request.ToArray());long start=0;var range=System.Text.RegularExpressions.Regex.Match(text,@"(?im)^Range:\s*bytes=(\d+)-");if(range.Success)start=long.Parse(range.Groups[1].Value);
    string header="HTTP/1.1 "+(range.Success?"206 Partial Content":"200 OK")+"\r\nContent-Length: "+(bytes.Length-start)+"\r\nAccept-Ranges: bytes\r\n"+(range.Success?"Content-Range: bytes "+start+"-"+(bytes.Length-1)+"/"+bytes.Length+"\r\n":"")+"Connection: close\r\n\r\n";
    var h=Encoding.ASCII.GetBytes(header);stream.Write(h,0,h.Length);
    for(long offset=start;offset<bytes.Length;offset+=16384){int size=(int)Math.Min(16384,bytes.Length-offset);stream.Write(bytes,(int)offset,size);Interlocked.Add(ref sent,size);Thread.Sleep(25);}
   }catch(IOException){}catch(SocketException){}}
  });}}catch(SocketException){}catch(ObjectDisposedException){}});
  var files=new List<ModNet.NetFile>{new ModNet.NetFile(new[]{"http://127.0.0.1:"+port+"/update.jar"},Path.Combine(folder,"update.jar"),new MeloongCore.FileChecker{Hash=hash})};
  var helper=asm.GetType("PCL.ModpackUpdate");var bridge=helper.GetMethod("DownloadFilesWithProgress",Flags);
  var step=new ModLoader.LoaderTask<string,string>("actual pack download",task=>{
   task.Progress=0.15;
   try{
    if(bridge==null)helper.GetMethod("DownloadFiles",Flags).Invoke(null,new object[]{files,CancellationToken.None});
    else bridge.Invoke(null,new object[]{files,CancellationToken.None,task.CreateSyncProgressProvider(0.15,0.65)});
   }catch(TargetInvocationException e){throw e.InnerException;}
  });
  bool advanced=false;double largest=0.15;
  try{
   step.Start("fixture");var until=DateTime.UtcNow.AddSeconds(25);
   while(step.State==ModBase.LoadState.Loading&&DateTime.UtcNow<until){double progress=step.Progress;if(progress>0.15&&progress<0.65){advanced=true;largest=Math.Max(largest,progress);}Thread.Sleep(10);}
   if(step.State!=ModBase.LoadState.Finished)throw new Exception("Loopback download did not finish",step.Error);
   if(!File.Exists(files[0].LocalPath)||new FileInfo(files[0].LocalPath).Length!=bytes.Length)throw new Exception("Fixture download content missing");
   if(!advanced)throw new Exception("Reproduced frozen progress: "+Volatile.Read(ref sent)+" bytes downloaded while the parent stayed at 15%");
   using(var canceled=new CancellationTokenSource()){
    canceled.Cancel();bool rejected=false;
    try{bridge.Invoke(null,new object[]{files,canceled.Token,step.CreateSyncProgressProvider(0.15,0.65)});}
    catch(TargetInvocationException e){rejected=e.InnerException is OperationCanceledException;}
    if(!rejected)throw new Exception("A pre-canceled download was restarted");
   }
   return "PASS: native download advanced the parent through 15% to "+largest.ToString("P1")+" before completion (loopback only).";
  }finally{listener.Stop();server.Wait(2000);}
 }
}
'@
function Call($type,$name,[object[]]$arguments){
 $values=New-Object object[] $arguments.Count;for($i=0;$i -lt $values.Length;$i++){if($null -ne $arguments[$i]){$values[$i]=$arguments[$i].PSObject.BaseObject}}
 if($type -is [Type]){return ,$type.GetMethod($name,$flags).Invoke($null,$values)}
 return ,$type.GetType().GetMethod($name,$flags).Invoke($type,$values)
}
function Read($value,$name){$property=$value.GetType().GetProperty($name,$flags);if($property){return ,$property.GetValue($value,$null)};return ,$value.GetType().GetField($name,$flags).GetValue($value)}
function Construct($name,[object[]]$arguments){$ctor=$asm.GetType('PCL.'+$name,$true).GetConstructors($flags)|Where-Object {$_.GetParameters().Count -eq $arguments.Count}|Select-Object -First 1;$values=New-Object object[] $arguments.Count;for($i=0;$i -lt $values.Length;$i++){$values[$i]=$arguments[$i].PSObject.BaseObject};return ,$ctor.Invoke($values)}
function Render($element,$name,$width=820,$height=640){
 $hostPanel=[Windows.Controls.Grid]::new();$hostPanel.Background=if($ColorMode -eq 1){$app.Resources['ColorBrushBackground']}else{[Windows.Media.BrushConverter]::new().ConvertFromString('#e7effb')};$hostPanel.Children.Add($element)|Out-Null
 $hostPanel.Measure([Windows.Size]::new($width,$height));$hostPanel.Arrange([Windows.Rect]::new(0,0,$width,$height));$hostPanel.UpdateLayout()
 if(-not $PreviewDirectory){$hostPanel.Children.Clear();return}
 [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($PreviewDirectory))|Out-Null
 $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($width,$height,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($hostPanel)
 $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$output=[IO.File]::Create((Join-Path $PreviewDirectory $name))
 try{$encoder.Save($output)}finally{$output.Dispose();$hostPanel.Children.Clear()}
}
function Dialog($view,$title,$button){
 $converter=[Activator]::CreateInstance($asm.GetType('PCL.ModMain+MyMsgBoxConverter'));$converter.Title=$title;$converter.Text='整合包预览';$converter.Button1=$button;$converter.Button2='取消';$converter.IsWarn=$false
 $converter.ContentFactory=[Func[Windows.FrameworkElement]]{return $view}.GetNewClosure()
 $modal=[Activator]::CreateInstance($asm.GetType('PCL.MyMsgText'),@($converter));$modal.FindName('TransformRotate').Angle=0;$modal.FindName('TransformPos').Y=0
 return ,$modal
}
try{
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 $sourcePath=Join-Path $fixture 'Checks.cs';[IO.File]::WriteAllText($sourcePath,$source)
 $checksPath=Join-Path $fixture 'checks.dll';$compiler=Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
 $arguments=@('/nologo','/target:library',('/out:'+$checksPath))+@($references|ForEach-Object {'/reference:'+$_})
 & $compiler @arguments $sourcePath
 if($LASTEXITCODE -ne 0){throw 'Modpack update UX fixture compilation failed'}
 [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($checksPath))|Out-Null
 [PackUpdateProgressCheck]::Run($asm,$fixture)
 if($ProgressOnly){return}
 $versions=New-Object 'System.Collections.Generic.List[PCL.ResourceVersion]'
 foreach($item in @(@('15.0.0-beta.3 for 26.3','Beta','2026-11-01'),@('14.1.2 for 26.2','Release','2026-11-01'),@('14.1.1 for 26.2','Release','2026-10-11'),@('15.0.0-alpha.5 for 26.3','Alpha','2026-10-05'))){
  $v=[PCL.ResourceVersion]::new();$v.Id=$item[0];$v.Display=$item[0];$v.ReleaseType=[Enum]::Parse([PCL.ResourceVersion+ReleaseTypes],$item[1]);$v.ReleaseDate=[DateTime]::Parse($item[2]);$v.GameVersions=New-Object 'System.Collections.Generic.List[string]';$v.GameVersions.Add($(if($item[0] -like '*26.3'){'26.3'}else{'26.2'}));$v.ModLoaders=[PCLCS.ModLoaders]::Fabric;$versions.Add($v)
 }
 $view=Construct 'ModpackVersionSelectView' @($versions,'Fabulously Optimized','13.0.0 for 1.21.11')
 $type=$view.GetType();$selections=Read $view 'Selections'
 if((Read $view 'ActiveChannel').ToString() -ne 'Release' -or $selections.Count -ne 2 -or $selections[0].Title -ne '14.1.2 for 26.2'){throw 'Version channels were mixed or not sorted by release date'}
 $modal=Dialog $view '选择整合包版本' '下载并预览'
 if($modal.FindName('Btn1').IsEnabled){throw 'Download is enabled before choosing a version'}
 $selections[1].Checked=$true
 if((Read $view 'Selected').Id -ne $selections[1].Tag.Id -or -not $modal.FindName('Btn1').IsEnabled){throw 'Grouped version selection returned the wrong version'}
 $selections[0].Checked=$true
 if(@($selections|Where-Object Checked).Count -ne 1 -or (Read $view 'Selected').Id -ne $selections[0].Tag.Id){throw 'Native selection did not remain exclusive'}
 Call $view 'ShowChannel' @([PCL.ResourceVersion+ReleaseTypes]::Beta)|Out-Null
 if((Read $view 'Selected') -or $modal.FindName('Btn1').IsEnabled -or (Read $view 'Selections').Count -ne 1){throw 'Changing channels retained an invisible selection'}
 (Read $view 'Selections')[0].Checked=$true
 if((Read $view 'Selected').Id -ne '15.0.0-beta.3 for 26.3'){throw 'Beta selection used a stale index from the ungrouped list'}
 Render $modal 'modpack-version-selector.png'
 $package=[Activator]::CreateInstance($asm.GetType('PCL.PackUpdatePackage'),$true);$package.Name='Fabulously Optimized';$package.Version='15.0.0-beta.3';$package.Minecraft='26.3';$package.Loaders.Add([PCLCS.ModLoaders]::Fabric,'0.19.3')
 $plan=[Activator]::CreateInstance($asm.GetType('PCL.PackUpdatePlan'),$true);$plan.Package=$package
 $fileType=$asm.GetType('PCL.PackUpdateFile');$changeType=$asm.GetType('PCL.PackUpdateChange')
 foreach($kind in @('Added','Updated','Removed','Preserved')){
  $count=@{Added=35;Updated=46;Removed=50;Preserved=12}[$kind]
  for($i=0;$i -lt $count;$i++){
   $change=[Activator]::CreateInstance($changeType,$true);$change.Relative='mods/'+@('Iris','Sodium','BetterGrassify','ConfigManager','Continuity','DynamicLights')[$i%6]+'-fabric-1.21.11-'+$i+'.jar';$change.Reason='作者文件发生变化'
   if($kind -eq 'Preserved'){$change.Conflict=$true;$change.Reason='保留玩家删除或禁用';if($i%2 -eq 0){$change.Relative='config/'+('MySettings-'+$i+'.toml');$change.Reason='保留个人文件或修改'}}
   if($kind -ne 'Added'){$change.Before='sha1:fixture-before'}
   if($kind -ne 'Removed'){$change.Incoming=[Activator]::CreateInstance($fileType,$true);$change.Incoming.Relative=$change.Relative}
   $plan.Changes.Add($change)
  }
 }
 $loaders=New-Object 'System.Collections.Generic.Dictionary[PCLCS.ModLoaders,string]';$loaders.Add([PCLCS.ModLoaders]::Fabric,'0.19.3')
 $preview=Construct 'ModpackUpdatePreviewView' @($plan,'1.21.11',$loaders);$previewType=$preview.GetType();$counts=Read $preview 'Counts';$kindType=$asm.GetType('PCL.ModpackUpdatePreviewView+ChangeKind')
 if((Read $preview 'ActiveKind').ToString() -ne 'Preserved'){throw 'Personal changes are not visible by default'}
 foreach($kind in @('Added','Updated','Removed','Preserved')){
  $value=[Enum]::Parse($kindType,$kind)
  if($counts[$value] -ne @{Added=35;Updated=46;Removed=50;Preserved=12}[$kind]){throw ('Incorrect change category: '+$kind)}
  Call $preview 'ShowKind' @($value)|Out-Null
  $rows=Read $preview 'Rows';$boxes=@($rows.Children|Where-Object {$_ -is [Windows.Controls.Border]})
  if($boxes.Count -ne $counts[$value] -or @($boxes|Where-Object {$_.BorderThickness.Left -ne 1 -or -not $_.ToolTip}).Count){throw 'Files lost their individual border or complete path tooltip'}
 }
 Call $preview 'ShowKind' @([Enum]::Parse($kindType,'Preserved'))|Out-Null
 $previewModal=Dialog $preview '整合包更新预览' '保留个人修改并更新'
 if($previewModal.FindName('Btn1').ColorType.ToString() -eq 'Red'){throw 'Safe preservation action is styled as destructive'}
 Render $previewModal 'modpack-update-preview.png'
 Call $preview 'ShowKind' @([Enum]::Parse($kindType,'Removed'))|Out-Null
 Render $previewModal 'modpack-removals-preview.png'
 $before=$plan.Changes.Count;$beforeConflicts=@($plan.Changes|Where-Object Conflict).Count
 $previewModal.FindName('PanCaption').Content=$null
 $preview.MaxHeight=240;$preview.Width=460
 Render $preview 'modpack-preview-compact.png' 470 250
 if((Read $preview 'Filters').Columns -ne 2){throw 'Compact preview did not wrap its category buttons'}
 if($plan.Changes.Count -ne $before -or @($plan.Changes|Where-Object Conflict).Count -ne $beforeConflicts){throw 'Preview changed the update plan'}
 Write-Output 'PASS: release channels, exclusive selection, confirm-button state, four operation categories, file borders and read-only preview.'
}finally{
 $app.Shutdown()
 if([IO.Directory]::Exists($fixture)){
  $resolved=[IO.Path]::GetFullPath($fixture);$temp=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
  if(-not $resolved.StartsWith($temp,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notmatch '^PclPackUpdateUx-[0-9a-f]{32}$'){throw 'Unsafe fixture cleanup'}
  [IO.Directory]::Delete($resolved,$true)
 }
}
