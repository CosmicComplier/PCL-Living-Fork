#requires -Version 5.1
param(
 [Parameter(Mandatory=$true)][string]$ExecutablePath,
 [string]$WorkspaceRoot=$env:GITHUB_WORKSPACE,
 [string]$NativeProjectPath,
 [string]$EvidenceRoot,
 [string]$DotNetPath='dotnet',
 [string]$FixtureProjectPath,
 [string]$NativeUpdaterSource,
 [ValidateRange(10,120)][int]$StartupTimeoutSeconds=60,
 [ValidateRange(3,30)][int]$CloseTimeoutSeconds=15
)
# CI-only, actual native Main entry. No product Assembly.Load/entry reflection,
# Startup bypass, update acceptance, dialog clicking, account credential, UAC or
# external release action. Requires fresh disposable Windows CI profile/machine.
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
if($env:OS -ne 'Windows_NT' -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted'){throw 'PENDING: requires an isolated disposable GitHub-hosted Windows CI machine/profile.'}
if([IntPtr]::Size -ne 8 -or $PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'PENDING: invoke 64-bit Windows PowerShell -NoProfile -STA; no product runtime is hosted by PowerShell.'}
if(-not $WorkspaceRoot){throw 'PENDING: WorkspaceRoot must identify the dedicated CI workspace.'}
$WorkspaceRoot=[IO.Path]::GetFullPath($WorkspaceRoot).TrimEnd('\')
if(-not [IO.Directory]::Exists($WorkspaceRoot)){throw 'WorkspaceRoot does not exist.'}
if([string]::Equals($WorkspaceRoot,[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase) -or $WorkspaceRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or $WorkspaceRoot.IndexOf('AppData\Local\Temp\',[StringComparison]::OrdinalIgnoreCase) -ge 0){throw 'PENDING: original LF refuses TEMP installs; use the CI workspace without altering that protection.'}
if($WorkspaceRoot -match '[^\x00-\x7f]'){throw 'PENDING: this bounded fixture requires an ASCII CI workspace so original PurePath remains in its own install directory.'}
if(-not $NativeProjectPath){$NativeProjectPath=Join-Path $repoRoot 'modern/Main/PCL-Living-Fork.Modern.csproj'}
if(-not [IO.File]::Exists($NativeProjectPath)){throw 'The formal complete Native Main project is missing.'}
if(-not $FixtureProjectPath){$FixtureProjectPath=Join-Path $PSScriptRoot 'native-smoke/NativeSmokeFixture.csproj'}
if(-not $NativeUpdaterSource){$NativeUpdaterSource=Join-Path (Split-Path $NativeProjectPath -Parent) 'Modules/ModernBundleIdentity.cs'}
if(-not $EvidenceRoot){$EvidenceRoot=Join-Path $WorkspaceRoot 'artifacts/native-smoke'}
$EvidenceRoot=[IO.Path]::GetFullPath($EvidenceRoot)
if(-not $EvidenceRoot.StartsWith($WorkspaceRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'EvidenceRoot must stay inside the dedicated CI workspace.'}
$ExecutablePath=[IO.Path]::GetFullPath($ExecutablePath)
$id=[Guid]::NewGuid().ToString('N');$fixture=Join-Path $EvidenceRoot ('NativeSmoke-'+$id)
$install=Join-Path $fixture 'Install with spaces';$differentCwd=Join-Path $fixture 'Different cwd';$cache=Join-Path $fixture 'Cache'
$installed=Join-Path $install 'LivingPCL Native Smoke.exe';$log=Join-Path $install 'PCL/Log1.txt';$marker='__PclLfNativeSmokeOwner'
$ownedRegistry=[Collections.Generic.List[string]]::new();$ownedAppData=$null;$nativeProcess=$null;$ownProcessStopped=$true
$evidence=[ordered]@{Result='pending';Scope='Actual product native cold start/WM_CLOSE/reopen, not complete functional regression';FixtureId=$id;InputExe=$ExecutablePath;MainProject=[IO.Path]::GetFullPath($NativeProjectPath);Runs=@();ForcedCleanup=$false;CleanupErrors=@();UserInteractive=[Environment]::UserInteractive;ObserverSessionId=[Diagnostics.Process]::GetCurrentProcess().SessionId}
function Pending([string]$reason){$evidence.Result='pending';throw ('PENDING: '+$reason)}
function Existing-Key([string]$name){foreach($view in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)){$base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view);try{$node=$base.OpenSubKey($name,$false);if($null -ne $node){$node.Dispose();return $true}}finally{$base.Dispose()}};return $false}
function Existing-Mutex([string]$name){try{$gate=[Threading.Mutex]::OpenExisting($name);$gate.Dispose();return $true}catch [Threading.WaitHandleCannotBeOpenedException]{return $false}}
function Check-Mutex([string]$name,[bool]$expectHeld){
 $gate=$null;$acquired=$false
 try{try{$gate=[Threading.Mutex]::OpenExisting($name)}catch [Threading.WaitHandleCannotBeOpenedException]{if($expectHeld){throw ('Actual Main did not create '+$name)};return}
  try{$acquired=$gate.WaitOne(0)}catch [Threading.AbandonedMutexException]{$acquired=$true;if($expectHeld){throw 'Actual running Main abandoned its session lock.'}}
  if($expectHeld -eq $acquired){throw ('Unexpected actual Main session lock state: '+$name)}
 }finally{if($null -ne $gate){if($acquired){$gate.ReleaseMutex()};$gate.Dispose()}}
}
function Hash-OwnedFile([string]$fileName){$stream=[IO.File]::Open($fileName,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete));$sha=[Security.Cryptography.SHA256]::Create();try{return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$sha.Dispose();$stream.Dispose()}}
function Read-OwnedLog {if(-not [IO.File]::Exists($log)){return ''};$reader=[IO.StreamReader]::new([IO.File]::Open($log,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)));try{return $reader.ReadToEnd()}finally{$reader.Dispose()}}
function Check-Log([string]$text,[string]$expectedNotice){
 if($text -match '(?m)^\d{2}:\d{2}:\d{2}\.\d{3} E '){throw 'Actual Main emitted an Error log; preserve evidence instead of lowering assertions.'}
 $titles=@([regex]::Matches($text,'(?:普通|选择|输入)弹窗：([^\r\n]+)')|ForEach-Object {$_.Groups[1].Value.Trim()})
 foreach($title in $titles){if($title -cne $expectedNotice){Pending ('Unexpected actual product dialog, never auto-clicked: '+$title)}}
 if($text -match '主窗体加载完成前出现意料外的等待弹窗|处理等待中的弹窗失败|弹窗初始化失败|弹窗加载失败'){throw 'Actual product modal/initialization failed.'}
}
try{
 foreach($directory in @($fixture,$install,$differentCwd,$cache)){[IO.Directory]::CreateDirectory($directory)|Out-Null}
 [IO.File]::WriteAllText((Join-Path $fixture 'owner.txt'),$id)
 $tools=Join-Path $fixture 'Tools';$intermediate=Join-Path $fixture 'ToolObj'
 & $DotNetPath build $FixtureProjectPath -c Release --output $tools ('-p:BaseIntermediateOutputPath='+$intermediate+'\') ('-p:MSBuildProjectExtensionsPath='+$intermediate+'\') ('-p:LFNativeUpdaterSource='+[IO.Path]::GetFullPath($NativeUpdaterSource)) -warnaserror -v:q
 if($LASTEXITCODE -ne 0){throw 'Candidate metadata inspector failed to compile.'}
 $proofJson=& $DotNetPath (Join-Path $tools 'NativeSmokeFixture.dll') $ExecutablePath
 if($LASTEXITCODE -ne 0){throw 'Not the genuine full native Main entry/BAML/resource/profile contract; no profile was created.'}
 $proof=($proofJson -join "`n")|ConvertFrom-Json;$evidence.MainProof=$proof
 if($proof.buildType -notin @(0,50)){Pending 'Debug/unknown builds may invoke original developer tests; smoke requires actual Release or Snapshot product.'}
 $expectedNotice=$proof.displayName+' '+$proof.versionBaseName+' 版本说明'
 # Existence only: do not read any pre-existing settings/accounts/game files.
 foreach($keyName in @('Software\PCL','Software\PCLDebug')){if(Existing-Key $keyName){Pending ('Pre-existing user/product store: HKCU\'+$keyName)}}
 $appData=[Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
 if([string]::IsNullOrEmpty($appData) -or -not [IO.Path]::IsPathRooted($appData)){Pending 'Actual Windows profile AppData path is unavailable.'}
 foreach($directory in @((Join-Path $appData 'PCL'),(Join-Path $appData '.minecraft'))){if([IO.Directory]::Exists($directory) -or [IO.File]::Exists($directory)){Pending ('Pre-existing user/game path: '+$directory)}}
 $osDrive=@([Environment]::GetLogicalDrives()|Where-Object {[IO.Directory]::Exists($_)})[0]
 $globalTaskTemp=Join-Path $osDrive 'ProgramData/PCL/TaskTemp'
 if([IO.Directory]::Exists($globalTaskTemp) -or [IO.File]::Exists($globalTaskTemp)){Pending 'Original startup clears ProgramData/PCL/TaskTemp; this is not a pristine disposable machine.'}
 $mutexNames=@('PCL_SingletonMutex','LivingPCL_SingletonMutex')
 foreach($name in $mutexNames){if(Existing-Mutex $name){Pending ('Existing launcher lock: '+$name)}}
 foreach($keyName in @('Software\PCL','Software\PCLDebug')){
  $node=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($keyName)
  try{if($node.ValueCount -ne 0 -or $node.SubKeyCount -ne 0){Pending 'Profile store appeared concurrently; refusing to modify it.'};$node.SetValue($marker,$id,[Microsoft.Win32.RegistryValueKind]::String);$ownedRegistry.Add($keyName)}finally{$node.Dispose()}
 }
 $node=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\PCL',$true)
 try{$node.SetValue('SystemSystemCache',$cache+'\',[Microsoft.Win32.RegistryValueKind]::String);$node.SetValue('LaunchFolders','Smoke Fixture>'+[IO.Path]::Combine($install,'.minecraft')+'\',[Microsoft.Win32.RegistryValueKind]::String)}finally{$node.Dispose()}
 $node=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\PCLDebug',$true)
 try{$node.SetValue('SystemEulaVersion','2',[Microsoft.Win32.RegistryValueKind]::String)}finally{$node.Dispose()}
 # A synthetic already-accepted local test state, not a click accepting terms.
 # No encrypted/online account value is seeded: original default is empty Legacy.
 $ownedAppData=Join-Path $appData 'PCL';[IO.Directory]::CreateDirectory($ownedAppData)|Out-Null;[IO.File]::WriteAllText((Join-Path $ownedAppData $marker),$id)
 [IO.Directory]::CreateDirectory((Join-Path $install '.minecraft/versions'))|Out-Null
 [IO.Directory]::CreateDirectory((Join-Path $install 'PCL'))|Out-Null
 [IO.File]::WriteAllText((Join-Path $install 'PCL/Setup.ini'),('NativeSmokeSentinel:'+$id+"`r`nLaunchFolderSelect:"+(Join-Path $install '.minecraft')+"\`r`nUiLauncherLogo:False`r`nUiCustomType:0`r`nLivingModpackMode:False`r`nToolLauncherUpdateSource:0`r`nToolLauncherUpdateChannel:0`r`n"),[Text.UTF8Encoding]::new($false))
 $gameSentinel=Join-Path $install '.minecraft/NativeSmoke.sentinel';[IO.File]::WriteAllText($gameSentinel,$id)
 Copy-Item -LiteralPath $ExecutablePath -Destination $installed
 if((Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash.ToLowerInvariant() -cne $proof.sha256){throw 'Installed fixture bytes differ from actual candidate preflight.'}
 Add-Type -Path (Join-Path $PSScriptRoot 'native-smoke/NativeSmokeWin32.cs') -ReferencedAssemblies System.Drawing.dll
 $previousLogHash=$null
 foreach($pass in @(1,2)){
  $run=[ordered]@{Pass=$pass;ActualExe=$installed;WorkingDirectory=$differentCwd;NormalExit=$false;ExpectedInformationNoticeVisible=$false};$evidence.Runs+=,$run
  $info=[Diagnostics.ProcessStartInfo]::new($installed);$info.UseShellExecute=$false;$info.WorkingDirectory=$differentCwd
  $info.EnvironmentVariables['DOTNET_BUNDLE_EXTRACT_BASE_DIR']=Join-Path $fixture 'BundleExtract'
  $nativeProcess=[Diagnostics.Process]::Start($info);if($null -eq $nativeProcess){throw 'Actual native Main did not start.'};$run.ProcessId=$nativeProcess.Id
  $watch=[Diagnostics.Stopwatch]::StartNew();$readyAt=-1;$mainWindow=$null
  while($watch.Elapsed.TotalSeconds -lt $StartupTimeoutSeconds){
   $nativeProcess.Refresh();if($nativeProcess.HasExited){throw ('Actual Main exited before ready: '+$nativeProcess.ExitCode)}
   if(-not [string]::Equals($nativeProcess.MainModule.FileName,$installed,[StringComparison]::OrdinalIgnoreCase)){throw 'Observed process is not the copied actual native Main.'}
   $windows=@([PclLfNativeSmoke.Win32]::Windows($nativeProcess.Id))
   foreach($window in $windows){if($window.Title.Length -gt 0 -and ($window.ClassName -notlike 'HwndWrapper*' -or $window.Title.Trim() -cne 'LivingPCL')){Pending ('Unexpected native product window: '+$window.Title+' / '+$window.ClassName)}}
   $mainWindow=@($windows|Where-Object {$_.Title.Trim() -ceq 'LivingPCL' -and $_.ClassName -like 'HwndWrapper*' -and $_.Right -gt $_.Left -and $_.Bottom -gt $_.Top})|Select-Object -First 1
   $text=Read-OwnedLog;Check-Log $text $expectedNotice
   $allStages=$text.Contains('第一阶段加载用时：') -and $text.Contains('第二阶段加载用时：') -and $text.Contains('第三阶段加载用时：') -and $text.Contains('PCL 位置：') -and $text.Contains($installed) -and $text.Contains('普通弹窗：'+$expectedNotice)
   $fresh=$pass -eq 1 -or ([IO.File]::Exists($log) -and (Hash-OwnedFile $log) -cne $previousLogHash)
   if($null -ne $mainWindow -and $allStages -and $fresh){if($readyAt -lt 0){$readyAt=$watch.ElapsedMilliseconds};if($watch.ElapsedMilliseconds-$readyAt -ge 1500){break}}else{$readyAt=-1}
   Start-Sleep -Milliseconds 150
  }
  if($null -eq $mainWindow -or $readyAt -lt 0 -or $watch.ElapsedMilliseconds-$readyAt -lt 1500){Pending 'Actual Main/animation/log stages did not reach a stable visible checkpoint within the bound.'}
  $run.ReadyMilliseconds=$watch.ElapsedMilliseconds;$run.Window=@{Title=$mainWindow.Title;Class=$mainWindow.ClassName;Handle=$mainWindow.Handle.ToInt64();Left=$mainWindow.Left;Top=$mainWindow.Top;Right=$mainWindow.Right;Bottom=$mainWindow.Bottom}
  $run.ExpectedInformationNoticeVisible=$text.Contains('普通弹窗：'+$expectedNotice)
  foreach($name in $mutexNames){Check-Mutex $name $true}
  $run.Screenshot=[PclLfNativeSmoke.Win32]::CaptureOwnWindow($mainWindow.Handle,(Join-Path $fixture ('main-'+$pass+'.png')))
  if($pass -eq 2){$rotated=Join-Path $install 'PCL/Log2.txt';if(-not [IO.File]::Exists($rotated) -or (Hash-OwnedFile $rotated) -cne $previousLogHash){throw 'Reopen did not preserve original LF log rotation.'}}
  if(-not [PclLfNativeSmoke.Win32]::PostClose($mainWindow.Handle)){throw 'Actual selected Main HWND did not accept WM_CLOSE.'}
  if(-not $nativeProcess.WaitForExit($CloseTimeoutSeconds*1000)){throw 'Actual Main failed normal bounded close; forced cleanup is not success.'}
  $run.ExitCode=$nativeProcess.ExitCode;if($nativeProcess.ExitCode -ne 0){throw 'Actual Main normal exit code was not zero.'}
  $text=Read-OwnedLog;Check-Log $text $expectedNotice
  if(-not $text.Contains('收到关闭指令')){throw 'WM_CLOSE did not execute the real LF closing/controller path.'}
  $run.NormalExit=$true;$nativeProcess.Dispose();$nativeProcess=$null
  foreach($name in $mutexNames){Check-Mutex $name $false}
  $exclusive=[IO.File]::Open($installed,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$exclusive.Dispose()
  if([IO.File]::ReadAllText($gameSentinel) -cne $id -or -not [IO.File]::ReadAllText((Join-Path $install 'PCL/Setup.ini')).Contains('NativeSmokeSentinel:'+$id)){throw 'Actual startup/close lost fixture settings/game sentinel.'}
  $updateFolder=Join-Path $install 'PCL/Updates';if([IO.Directory]::Exists($updateFolder) -and @([IO.Directory]::EnumerateFiles($updateFolder,'*.exe',[IO.SearchOption]::AllDirectories)).Count -gt 0){throw 'An update payload appeared without any acceptance action.'}
  Copy-Item -LiteralPath $log -Destination (Join-Path $fixture ('startup-'+$pass+'.log'))
  $previousLogHash=Hash-OwnedFile $log
 }
 $evidence.Result='passed';$evidence.Limit='Cold actual Main HWND visible in the CI windowstation/normal close/reopen only; noninteractive sessions are recorded, not claimed as human desktop interaction. Known OpenSource version information is observed, never clicked; no navigation/font/interaction/update-bridge completion inferred.'
}catch{if($evidence.Result -ne 'pending' -or $_.Exception.Message -notlike 'PENDING:*'){$evidence.Result='failed'};$evidence.Error=$_.Exception.Message;throw}
finally{
 if($null -ne $nativeProcess){try{if(-not $nativeProcess.HasExited){[void]$nativeProcess.CloseMainWindow();if(-not $nativeProcess.WaitForExit(3000)){$evidence.ForcedCleanup=$true;$nativeProcess.Kill();[void]$nativeProcess.WaitForExit(5000)}};$ownProcessStopped=$nativeProcess.HasExited;if(-not $ownProcessStopped){$evidence.CleanupErrors+='Own launcher is still alive; preserve fixture profile.'}}catch{$ownProcessStopped=$false;$evidence.CleanupErrors+=('Own process cleanup: '+$_.Exception.Message)}finally{$nativeProcess.Dispose()}}
 # Delete only initially absent stores owned by this fixture marker. Never back
 # up/read/restore a real user's profile or delete an unmarked changed store.
 if($ownProcessStopped){foreach($keyName in $ownedRegistry){try{$node=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($keyName,$false);$matches=$false;try{$matches=$null -ne $node -and $node.GetValue($marker) -ceq $id}finally{if($null -ne $node){$node.Dispose()}};if($matches){[Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($keyName)}else{$evidence.CleanupErrors+=('Registry owner marker changed: '+$keyName)}}catch{$evidence.CleanupErrors+=('Registry cleanup: '+$keyName+' / '+$_.Exception.Message)}}
 if($null -ne $ownedAppData){try{if([IO.File]::ReadAllText((Join-Path $ownedAppData $marker)) -ceq $id){[IO.Directory]::Delete($ownedAppData,$true)}else{$evidence.CleanupErrors+='AppData owner marker changed.'}}catch{$evidence.CleanupErrors+=('Owned AppData cleanup: '+$_.Exception.Message)}}
 }else{$evidence.CleanupErrors+='Owned profile retained because process exit could not be confirmed.'}
 if($evidence.ForcedCleanup -or $evidence.CleanupErrors.Count -gt 0){if($evidence.Result -eq 'passed'){$evidence.Result='failed'}}
 if([IO.Directory]::Exists($fixture)){[IO.File]::WriteAllText((Join-Path $fixture 'evidence.json'),($evidence|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Native Main smoke evidence: '+(Join-Path $fixture 'evidence.json'))}
}
if($evidence.Result -ne 'passed'){throw ('Native Main smoke '+$evidence.Result+'; see evidence.json')}
Write-Output 'PASS actual native PCL.Application Main: real startup/BAML/visible HWND in CI windowstation/log stages, WM_CLOSE normal controller exit, session/file locks, reopen/log rotation and synthetic data preservation. No update accepted.'
