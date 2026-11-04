param(
 [Parameter(Mandatory=$true)][string]$NativeProjectPath,
 [Parameter(Mandatory=$true)][string]$OutputPath,
 [ValidateSet('Debug','Release','Snapshot')][string]$Configuration='Release',
 [string]$DotNetPath='dotnet',
 [string]$NativeInspectorProject,
 [string]$NativeUpdaterSource,
 [switch]$BridgeOnly
)
# Build/package only. This script never publishes a GitHub release, starts a
# launcher, applies updates, reads user data, or supplies build credentials.
# The actual Main project must already carry the LF credential/source contract.
$ErrorActionPreference='Stop'
$NativeProjectPath=[IO.Path]::GetFullPath($NativeProjectPath)
$OutputPath=[IO.Path]::GetFullPath($OutputPath)
if(-not $NativeInspectorProject){$NativeInspectorProject=Join-Path $PSScriptRoot 'native-update-inspector/NativeUpdateInspector.csproj'}
if(-not $NativeUpdaterSource){$NativeUpdaterSource=Join-Path (Split-Path $NativeProjectPath -Parent) 'Modules/ModernBundleIdentity.cs'}
$temporary=Join-Path ([IO.Path]::GetTempPath()) ('PclNativeRelease-'+[Guid]::NewGuid().ToString('N'))
try{
 [IO.Directory]::CreateDirectory($temporary)|Out-Null
 $payloadFolder=Join-Path $temporary 'Payload'
 # Keep the original C# credential generator/lease/cleanup boundary for the
 # actual Main publish. The wrapper owns the one credential GUID lifecycle.
 & (Join-Path $PSScriptRoot 'Build-ModernRelease.ps1') -Configuration $Configuration -OutputPath $payloadFolder -DotnetPath $DotNetPath -ProjectPath $NativeProjectPath -NativePayload
 $payload=Join-Path $payloadFolder 'LivingPCL.exe'
 if(-not [IO.File]::Exists($payload)){throw 'Native publish did not produce LivingPCL.exe'}
 $sidecars=@(Get-ChildItem -LiteralPath $payloadFolder -File|Where-Object Name -ne 'LivingPCL.exe')
 if($sidecars.Count -ne 0){throw 'Single-file publish left sidecar files; do not create an incomplete update payload'}
 $inspectorFolder=Join-Path $temporary 'Inspector'
 & $DotNetPath build $NativeInspectorProject -c Release --output $inspectorFolder ('-p:LFNativeUpdaterSource='+[IO.Path]::GetFullPath($NativeUpdaterSource)) -warnaserror -v:q
 if($LASTEXITCODE -ne 0){throw 'Native payload inspector compilation failed'}
 $identityJson=& $DotNetPath (Join-Path $inspectorFolder 'NativeUpdateInspector.dll') $payload
 if($LASTEXITCODE -ne 0){throw 'Published native payload failed its real entry/bundle validation'}
 $identity=($identityJson -join "`n")|ConvertFrom-Json
 if($identity.AssemblyName -ne 'LivingPCL' -or $identity.Product -ne 'PCL Living Fork'){throw 'Native payload branding/assembly identity mismatch'}
 $productVersion=$identity.ProductVersion
 if($productVersion -notmatch '\A\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z'){throw 'The actual entry has an invalid product version'}
 $canonicalName='PCL-Living-Fork-'+$productVersion+'.exe'
 $canonical=Join-Path $temporary $canonicalName
 & (Join-Path $PSScriptRoot 'Build-UpdateBridge.ps1') -ExecutablePath $payload -OutputPath $canonical -DotNetPath $DotNetPath -NativeInspectorProject $NativeInspectorProject -NativeUpdaterSource $NativeUpdaterSource
 # The normal first-install asset is the real self-contained NET10 product.
 # Only old-client automatic updates require the managed compatibility bridge.
 # This native name matches none of the three canonical updater prefixes.
 $nativeName='LivingPCL.Native-'+$productVersion+'-win-x64.exe'
 $artifacts=@()
 if(-not $BridgeOnly){
  $artifacts+=@{Role='native-exe';Name=$nativeName;Path=$payload}
  # Retain the existing LF/CNB three-asset release contract. First-install ZIP
  # carries the genuine native main product, never the Framework compatibility bridge.
  $archiveName='PCL-Living-Fork-'+$productVersion+'-win-x64.zip'
  $archive=Join-Path $temporary $archiveName
  $archiveFolder=Join-Path $temporary 'NativeArchive'
  [IO.Directory]::CreateDirectory($archiveFolder)|Out-Null
  $archiveExecutable=Join-Path $archiveFolder $canonicalName
  Copy-Item -LiteralPath $payload -Destination $archiveExecutable
  Compress-Archive -LiteralPath $archiveExecutable -DestinationPath $archive -CompressionLevel Optimal
  $artifacts+=@{Role='native-zip';Name=$archiveName;Path=$archive;NativeEntryName=$canonicalName}
 }
 $artifacts+=@{Role='legacy-bridge';Name=$canonicalName;Path=$canonical}
 foreach($artifact in $artifacts){if([IO.File]::Exists((Join-Path $OutputPath $artifact.Name))){throw 'Refusing to overwrite an existing release asset'}}
 $indexPath=Join-Path $OutputPath 'NativeRelease.Artifacts.json'
 if([IO.File]::Exists($indexPath)){throw 'Refusing to overwrite an existing build artifact index'}
 $manifestPath=Join-Path $OutputPath 'SHA256SUMS.txt'
 if([IO.File]::Exists($manifestPath)){throw 'Refusing to overwrite an existing release manifest'}
 [IO.Directory]::CreateDirectory($OutputPath)|Out-Null
 $lines=@()
 foreach($artifact in $artifacts){
  $hash=(Get-FileHash -LiteralPath $artifact.Path -Algorithm SHA256).Hash.ToLowerInvariant()
  Copy-Item -LiteralPath $artifact.Path -Destination (Join-Path $OutputPath $artifact.Name)
  $lines+=($hash+'  '+$artifact.Name)
 }
 [IO.File]::WriteAllText($manifestPath,($lines -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
 # Build-only public artifact roles for CI. This JSON is not an update setting,
 # feed/manifest protocol or an additional CNB release asset. No credential or
 # temporary source/material path is included.
 $payloadHash=(Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToLowerInvariant()
 $indexArtifacts=@()
 foreach($artifact in $artifacts){
  $published=[IO.Path]::GetFullPath((Join-Path $OutputPath $artifact.Name))
  $record=[ordered]@{role=$artifact.Role;fileName=$artifact.Name;path=$published;sha256=(Get-FileHash -LiteralPath $published -Algorithm SHA256).Hash.ToLowerInvariant();bytes=([IO.FileInfo]::new($published)).Length}
  if($artifact.Role -eq 'native-zip'){$record.nativeEntryName=$artifact.NativeEntryName;$record.nativeEntrySha256=$payloadHash}
  $indexArtifacts+=,$record
 }
 $indexArtifacts+=,[ordered]@{role='checksums';fileName='SHA256SUMS.txt';path=$manifestPath;sha256=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant();bytes=([IO.FileInfo]::new($manifestPath)).Length}
 $index=[ordered]@{schemaVersion=1;kind='PCLLF.NativeRelease.Artifacts';configuration=$Configuration;runtimeIdentifier='win-x64';productVersion=$productVersion;assemblyVersion=[string]$identity.Version;bridgeOnly=[bool]$BridgeOnly;nativePayloadSha256=$payloadHash;artifacts=$indexArtifacts}
 [IO.File]::WriteAllText($indexPath,($index|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
 if(-not $BridgeOnly){Write-Output ('Native main product for first install: '+$nativeName+' and '+$archiveName+' (self-contained NET10; no Framework4.8 bridge required).')}
 Write-Output ('Legacy automatic-update compatibility bridge: '+$canonicalName+'; no release was published.')
}finally{
 $full=[IO.Path]::GetFullPath($temporary);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '\APclNativeRelease-[a-f0-9]{32}\z'){throw 'Unsafe native release cleanup'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
