param(
    [Parameter(Mandatory=$true)][string]$ProjectRoot,
    [Parameter(Mandatory=$true)][string]$MSBuildPath
)
# Fixed historical FAILURE/lock oracle only. This is not a product builder and
# cannot default to any working-tree business source or successful old release.
$ErrorActionPreference = 'Stop'
$tools = Split-Path $PSScriptRoot -Parent
. (Join-Path $tools 'Get-FixedLfSource.ps1')
$projectRoot = Assert-LfFixedTemporaryDirectory $ProjectRoot
$owner = [IO.DirectoryInfo]::new($projectRoot).Parent
if ([IO.Path]::GetFileName($projectRoot) -cne 'legacy-build-root' -or $null -eq $owner -or $owner.Name -cnotmatch '\ALivingPclCredentials-[a-f0-9]{32}\z') { throw 'Historical credential failure requires the original GUID-owned test fixture.' }
$compiler = [IO.Path]::GetFullPath($MSBuildPath)
if (-not $compiler.StartsWith($owner.FullName + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($compiler) -cne 'FailBuild.exe' -or -not [IO.File]::Exists($compiler) -or (([IO.File]::GetAttributes($compiler) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) { throw 'Historical fixture requires its own explicit failure executable; normal MSBuild/product builds are forbidden.' }
$null = Export-LfFixedSource -Name 'legacy-modsecret-f4' -Directory $projectRoot
$projectFile = Export-LfFixedSource -Name 'legacy-vbproj-f4' -Directory $projectRoot
$generatedRoot = Join-Path $projectRoot 'Plain Craft Launcher 2/obj/CredentialBuild'
$generatedFolder = Join-Path $generatedRoot ([Guid]::NewGuid().ToString('N'))
$generatedSource = Join-Path $generatedFolder 'BuildCredentials.g.vb'
$buildLease = $null
try {
    [IO.Directory]::CreateDirectory($generatedRoot) | Out-Null
    try { $buildLease = [IO.File]::Open((Join-Path $generatedRoot 'build.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw 'Cannot acquire the workspace build lock; another historical fixture build may be running.' }
    [IO.Directory]::CreateDirectory($generatedFolder) | Out-Null
    & (Join-Path $tools 'Write-BuildCredentials.ps1') -OutputPath $generatedSource -SourceLanguage VisualBasic
    $intermediatePath = Join-Path $generatedFolder 'Intermediate/'
    & $compiler $projectFile -restore -t:Rebuild '-p:Configuration=Release' ("-p:GeneratedCredentialSource=$generatedSource") ("-p:CredentialIntermediateOutputPath=$intermediatePath") -verbosity:minimal -nologo
    if ($LASTEXITCODE -ne 0) { throw "Historical fixture build failed with exit code $LASTEXITCODE" }
    throw 'The explicit historical failure executable unexpectedly succeeded; no product is produced by this fixture.'
}
finally {
    try {
        if ([IO.Directory]::Exists($generatedFolder)) {
            $resolved = [IO.Path]::GetFullPath($generatedFolder)
            if (-not $resolved.StartsWith($generatedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -cnotmatch '\A[0-9a-f]{32}\z' -or (([IO.File]::GetAttributes($resolved) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) { throw 'Unsafe historical credential fixture cleanup path.' }
            [IO.Directory]::Delete($resolved, $true)
        }
    } finally { if ($null -ne $buildLease) { $buildLease.Dispose() } }
}
