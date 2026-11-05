#requires -Version 5.1
param(
 [Parameter(Mandatory=$true)][string]$ExecutablePath,
 [string]$WorkspaceRoot=$env:GITHUB_WORKSPACE,
 [string]$NativeProjectPath,
 [string]$EvidenceRoot,
 [string]$DotNetPath='dotnet',
 [ValidateRange(10,120)][int]$StartupTimeoutSeconds=60,
 [ValidateRange(3,30)][int]$CloseTimeoutSeconds=15
)
# Separate opt-in entry. The original Smoke remains unchanged unless explicitly
# passed ExerciseInteraction; it still owns profile creation and all cleanup.
$ErrorActionPreference='Stop'
if($env:OS -ne 'Windows_NT' -or $env:CI -ne 'true' -or $env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted'){throw 'PENDING: requires an isolated disposable GitHub-hosted Windows CI machine/profile.'}
if([IntPtr]::Size -ne 8 -or $PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'PENDING: invoke 64-bit Windows PowerShell -NoProfile -STA.'}
if(-not $WorkspaceRoot){throw 'PENDING: dedicated WorkspaceRoot is required.'}
$WorkspaceRoot=[IO.Path]::GetFullPath($WorkspaceRoot).TrimEnd('\')
if(-not [IO.Directory]::Exists($WorkspaceRoot)){throw 'WorkspaceRoot does not exist.'}
if(-not $EvidenceRoot){$EvidenceRoot=Join-Path $WorkspaceRoot 'artifacts/native-interaction'}
$EvidenceRoot=[IO.Path]::GetFullPath($EvidenceRoot)
if(-not $EvidenceRoot.StartsWith($WorkspaceRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'EvidenceRoot must stay in the dedicated CI workspace.'}
$toolRoot=Join-Path $EvidenceRoot ('Observer-'+[Guid]::NewGuid().ToString('N'))
$output=Join-Path $toolRoot 'bin';$intermediate=Join-Path $toolRoot 'obj'
$project=Join-Path $PSScriptRoot 'NativeInteractionProbe.csproj'
& $DotNetPath build $project -c Release --output $output ('-p:BaseIntermediateOutputPath='+$intermediate+'\') ('-p:MSBuildProjectExtensionsPath='+$intermediate+'\') -warnaserror -v:q
if($LASTEXITCODE -ne 0){throw 'External UIAutomation observer failed to compile; product was not started.'}
$smoke=@{ExecutablePath=$ExecutablePath;WorkspaceRoot=$WorkspaceRoot;EvidenceRoot=$EvidenceRoot;DotNetPath=$DotNetPath;StartupTimeoutSeconds=$StartupTimeoutSeconds;CloseTimeoutSeconds=$CloseTimeoutSeconds;ExerciseInteraction=$true;InteractionProbePath=(Join-Path $output 'NativeInteractionProbe.dll')}
if($NativeProjectPath){$smoke.NativeProjectPath=$NativeProjectPath}
& (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Test-NativeLauncherSmoke.ps1') @smoke
