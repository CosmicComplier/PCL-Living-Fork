param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$OutputPath,
    [string]$MSBuildPath = 'msbuild',
    [switch]$ComparisonOnly
)
$ErrorActionPreference = 'Stop'
# Temporary real legacy Main comparison only. Keep original Windows consumer
# coverage until its Native counterpart is accepted; remove this helper with
# product VB retirement. Never a default product publishing/bridge-as-Main path.
if (-not $ComparisonOnly) { throw 'Explicit -ComparisonOnly is required; use scripts/Build-Release.ps1 for the actual Native product.' }
$tools = Split-Path $PSScriptRoot -Parent
$projectRoot = Split-Path $tools -Parent
if (-not [IO.File]::Exists((Join-Path $projectRoot 'Plain Craft Launcher 2/PCL-Living-Fork.vbproj'))) { throw 'The temporary comparison source is retired/missing; do not substitute a managed update bridge or Native apphost.' }
Write-Output 'COMPARISON ONLY: real historical LF Main for preserved original Windows consumers; not the default Native product.'
$generatedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Plain Craft Launcher 2\obj\CredentialBuild'))
$generatedFolder = Join-Path $generatedRoot ([Guid]::NewGuid().ToString('N'))
$generatedSource = Join-Path $generatedFolder 'BuildCredentials.g.vb'
$buildLease = $null
try {
    [IO.Directory]::CreateDirectory($generatedRoot) | Out-Null
    try {
        $buildLease = [IO.File]::Open((Join-Path $generatedRoot 'build.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch {
        throw 'Cannot acquire the workspace build lock; another release build may be running.'
    }
    [IO.Directory]::CreateDirectory($generatedFolder) | Out-Null
    & (Join-Path $tools 'Write-BuildCredentials.ps1') -OutputPath $generatedSource
    $intermediatePath = Join-Path $generatedFolder 'Intermediate\'
    $buildArgs = @((Join-Path $projectRoot 'Plain Craft Launcher 2\PCL-Living-Fork.vbproj'), '-restore', '-t:Rebuild', "-p:Configuration=$Configuration", "-p:GeneratedCredentialSource=$generatedSource", "-p:CredentialIntermediateOutputPath=$intermediatePath", '-verbosity:minimal', '-nologo')
    if ($OutputPath) { $buildArgs += "-p:OutputPath=$OutputPath" }
    & $MSBuildPath @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
    # Keep the legacy assembly identity, but use the public product name on disk.
    $projectFolder=Join-Path $projectRoot 'Plain Craft Launcher 2'
    $outputFolder=if($OutputPath){if([IO.Path]::IsPathRooted($OutputPath)){[IO.Path]::GetFullPath($OutputPath)}else{[IO.Path]::GetFullPath((Join-Path $projectFolder $OutputPath))}}else{Join-Path $projectFolder 'bin'}
    $productVersion=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $outputFolder 'LivingPCL.exe')).ProductVersion
    if($productVersion -notmatch '\A\d+\.\d+\.\d+(?:\.\d+)?\z'){throw 'The built executable has an invalid product version'}
    $executableName='PCL-Living-Fork-'+$productVersion+'.exe'
    foreach($suffix in @('', '.config')){
        $compiled=Join-Path $outputFolder ('LivingPCL.exe'+$suffix)
        if([IO.File]::Exists($compiled)){Move-Item -LiteralPath $compiled -Destination (Join-Path $outputFolder ($executableName+$suffix)) -Force}
    }
}
finally {
    try {
        if ([IO.Directory]::Exists($generatedFolder)) {
            $resolvedFolder = [IO.Path]::GetFullPath($generatedFolder)
            $rootPrefix = $generatedRoot.TrimEnd('\') + '\'
            if (-not $resolvedFolder.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                [IO.Path]::GetFileName($resolvedFolder) -notmatch '\A[0-9a-f]{32}\z' -or
                (([IO.File]::GetAttributes($resolvedFolder) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
                throw 'Unsafe credential-build cleanup path'
            }
            [IO.Directory]::Delete($resolvedFolder, $true)
        }
    } finally {
        if ($null -ne $buildLease) { $buildLease.Dispose() }
    }
}
