param(
 [Parameter(Mandatory=$true)][string]$BridgeBuildScript,
 [Parameter(Mandatory=$true)][string]$NativePayloadPath,
 [Parameter(Mandatory=$true)][string]$ManagedPayloadPath,
 [Parameter(Mandatory=$true)][string]$ForeignManagedPath,
 [Parameter(Mandatory=$true)][string]$UnbundledAppHostPath,
 [Parameter(Mandatory=$true)][string]$NativeInspectorProject,
 [Parameter(Mandatory=$true)][string]$NativeUpdaterSource,
 [Parameter(Mandatory=$true)][string]$EvidenceRoot,
 [string]$DotNetPath='dotnet'
)
# Actual production identity routing only: no bridge/compiler/entry/profile,
# update, or network action. The strict native reader is executed as its real
# isolated NET10 tool, not replaced by a fixture implementation.
$ErrorActionPreference='Stop'
$source=[IO.File]::ReadAllText([IO.Path]::GetFullPath($BridgeBuildScript))
$start=$source.IndexOf(' $identity=$null',[StringComparison]::Ordinal)
$end=$source.IndexOf(' $hash=(Get-FileHash',[StringComparison]::Ordinal)
if($start -lt 0 -or $end -le $start){throw 'Production identity segment contract changed.'}
$identitySegment=[scriptblock]::Create($source.Substring($start,$end-$start))
$errors=$null;$tokens=$null;$ast=[Management.Automation.Language.Parser]::ParseInput($source,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Actual production source does not parse.'}
$catches=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.CatchClauseAst] -and $node.Extent.Text.Contains('$metadataError=$_.Exception')},$true))
if($catches.Count -ne 1){throw 'Production exception boundary changed.'}
$catchText=$catches[0].Body.Extent.Text
$catchBody=$catchText.Substring(1,$catchText.Length-2)
$policy=[scriptblock]::Create('try { throw $synthetic } catch {'+$catchBody+'}')
$runRoot=Join-Path ([IO.Path]::GetFullPath($EvidenceRoot)) ('BridgeIdentity-'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($runRoot)|Out-Null
$results=[Collections.Generic.List[object]]::new()
function Test-Identity([string]$name,[string]$path,[bool]$accept,[bool]$native){
 $ExecutablePath=[IO.Path]::GetFullPath($path);$temporary=Join-Path $runRoot $name
 [IO.Directory]::CreateDirectory($temporary)|Out-Null
 $identity=$null;$parsed=$null;$nativeIdentity=$null;$caught=$null
 try{. $identitySegment}catch{$caught=$_}
 if(($null -eq $caught) -ne $accept){throw ('Unexpected actual identity outcome: '+$name+' / '+$caught)}
 $used=[IO.Directory]::Exists((Join-Path $temporary 'Inspector'))
 if($used -ne $native){throw ('Unexpected native inspector dispatch: '+$name)}
 if($native -and -not [IO.File]::Exists((Join-Path $temporary 'Inspector/NativeUpdateInspector.dll'))){throw 'Actual native inspector did not compile; rejection is not identity proof.'}
 if($native -and -not $accept -and -not $caught.Exception.Message.Contains('Native bridge payload is not a supported genuine LF bundle')){throw ('Native candidate did not reach actual strict reader rejection: '+$name+' / '+$caught)}
 if($accept -and $null -eq $parsed){throw 'No original parsed bridge version.'}
 if($accept -and -not $native -and $parsed.ToString() -cne ([version][Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath).FileVersion).ToString()){throw 'Original managed FileVersion rule changed.'}
 $results.Add([ordered]@{name=$name;accepted=$accept;nativeInspectorUsed=$used;parsedVersion=if($null -ne $parsed){$parsed.ToString()}else{$null};exception=if($null -ne $caught){$caught.Exception.GetType().FullName}else{$null};nativeIdentity=$nativeIdentity})
}
Test-Identity 'genuine-native-main' $NativePayloadPath $true $true
Test-Identity 'genuine-managed-original-rule' $ManagedPayloadPath $true $false
Test-Identity 'foreign-managed-identity' $ForeignManagedPath $false $false
Test-Identity 'unbundled-native-apphost' $UnbundledAppHostPath $false $true
$bad=Join-Path $runRoot 'malformed.exe';[IO.File]::WriteAllText($bad,'not PE or a bundle')
Test-Identity 'invalid-PE' $bad $false $true
Test-Identity 'missing-file-IO' (Join-Path $runRoot 'does-not-exist.exe') $false $false
$directory=Join-Path $runRoot 'not-a-file.exe';[IO.Directory]::CreateDirectory($directory)|Out-Null
Test-Identity 'directory-access-error' $directory $false $false
$locked=Join-Path $runRoot 'locked-managed.exe';[IO.File]::Copy($ManagedPayloadPath,$locked)
$lease=[IO.File]::Open($locked,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
try{Test-Identity 'exclusive-handle-IO' $locked $false $false}finally{$lease.Dispose()}
foreach($item in @(
 @{name='generic-invalid-operation';error=[InvalidOperationException]::new('Unrelated state failure')},
 @{name='IO-never-native';error=[IO.IOException]::new('Synthetic IO failure')},
 @{name='permission-never-native';error=[UnauthorizedAccessException]::new('Synthetic permission failure')}
)){
 $synthetic=$item.error;$caught=$null
 try{. $policy}catch{$caught=$_}
 if($null -eq $caught){throw ('Production catch swallowed a nonmetadata failure: '+$item.name)}
 $results.Add([ordered]@{name=$item.name;rejected=$true;exception=$caught.Exception.GetType().FullName})
}
$summary=[ordered]@{platform=[Runtime.InteropServices.RuntimeInformation]::OSDescription;runtime=[Runtime.InteropServices.RuntimeInformation]::FrameworkDescription;powerShell=$PSVersionTable.PSVersion.ToString();scope='Actual production identity slice and typed exception catch plus real bounded reader; no product execution or Windows acceptance implied';count=$results.Count;results=@($results.ToArray())}
$summary|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $runRoot 'results.json') -Encoding utf8
Write-Output ('PASS '+$results.Count+' actual identity routing/catch contracts; evidence '+(Join-Path $runRoot 'results.json'))
