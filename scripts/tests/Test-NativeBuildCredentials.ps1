param(
    [Parameter(Mandatory=$true)][string]$ManagedMainPath,
    [Parameter(Mandatory=$true)][string]$NativeExecutablePath,
    [string]$DotNetPath = 'dotnet',
    [switch]$KeylessBuild
)
# Actual Main's pure credential/bounded-reader methods only. Never start the
# launcher or load a managed update bridge as its full product Application.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('PclNativeCredentials-' + [Guid]::NewGuid().ToString('N'))
try {
    [IO.Directory]::CreateDirectory($temporary) | Out-Null
    $output = Join-Path $temporary 'tools'
    $intermediate = Join-Path $temporary 'obj/'
    $project = Join-Path $PSScriptRoot 'native-credentials/NativeCredentialChecks.csproj'
    & $DotNetPath build $project -c Release --output $output ("-p:BaseIntermediateOutputPath=$intermediate") ("-p:MSBuildProjectExtensionsPath=$intermediate") -warnaserror --disable-build-servers -nodeReuse:false -m:1 -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'The isolated actual Native credential checker did not compile.' }
    $arguments = @((Join-Path $output 'NativeCredentialChecks.dll'), '--main', [IO.Path]::GetFullPath($ManagedMainPath), '--native', [IO.Path]::GetFullPath($NativeExecutablePath))
    if ($KeylessBuild) { $arguments += '--keyless' }
    & $DotNetPath @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Actual Native Main/bundle credential protection failed; no bridge or different Main fallback is permitted.' }
}
finally {
    $full = [IO.Path]::GetFullPath($temporary)
    $prefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -cnotmatch '\APclNativeCredentials-[a-f0-9]{32}\z' -or ([IO.Directory]::Exists($full) -and (([IO.File]::GetAttributes($full) -band [IO.FileAttributes]::ReparsePoint) -ne 0))) { throw 'Unsafe isolated credential-checker cleanup path.' }
    if ([IO.Directory]::Exists($full)) { [IO.Directory]::Delete($full, $true) }
}
