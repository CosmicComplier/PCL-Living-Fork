param(
    [ValidateSet('Debug', 'Release', 'Snapshot')][string]$Configuration = 'Release',
    [string]$OutputPath,
    [string]$DotnetPath = 'dotnet',
    [string]$ProjectPath,
    [switch]$NativePayload,
    [switch]$Slim,
    [switch]$OfficialRelease,
    [switch]$EnableLegacyLink
)
$ErrorActionPreference = 'Stop'
if ($NativePayload -and $Slim) { throw 'Choose either the self-contained payload or the slim payload.' }
$projectRoot = Split-Path $PSScriptRoot -Parent
$projectFile = if ($ProjectPath) { [IO.Path]::GetFullPath($ProjectPath) } else { Join-Path $projectRoot 'modern/Main/PCL-Living-Fork.Modern.csproj' }
$projectFolder = [IO.Path]::GetDirectoryName($projectFile)
if (-not [IO.File]::Exists($projectFile)) { throw 'The modern C# product project is missing.' }
$generatedRoot = [IO.Path]::GetFullPath((Join-Path $projectFolder 'obj/CredentialBuild'))
$generatedFolder = Join-Path $generatedRoot ([Guid]::NewGuid().ToString('N'))
$generatedSource = Join-Path $generatedFolder 'BuildCredentials.g.cs'
$buildLease = $null
try {
    [IO.Directory]::CreateDirectory($generatedRoot) | Out-Null
    try {
        $buildLease = [IO.File]::Open((Join-Path $generatedRoot 'build.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch {
        throw 'Cannot acquire the workspace build lock; another modern release build may be running.'
    }
    [IO.Directory]::CreateDirectory($generatedFolder) | Out-Null
    & (Join-Path $PSScriptRoot 'Write-BuildCredentials.ps1') -OutputPath $generatedSource -SourceLanguage CSharp
    $intermediatePath = Join-Path $generatedFolder 'Intermediate/'
    $publishArgs = @('publish', $projectFile, '--configuration', $Configuration,
        '--disable-build-servers', '-nodeReuse:false', '-m:1',
        "-p:GeneratedCredentialSource=$generatedSource", "-p:CredentialIntermediateOutputPath=$intermediatePath", '--verbosity', 'minimal', '--nologo')
    if ($NativePayload -or $Slim) {
        # Same fixed format as the production bounded native identity reader.
        # Reuse this wrapper's sole GUID/lease/finally for credential material.
        $selfContained = if ($Slim) { 'false' } else { 'true' }
        $publishArgs += @('-r', 'win-x64', '--self-contained', $selfContained,
            '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=false',
            '-p:PublishReadyToRun=false', '-p:PublishTrimmed=false', '-p:DebugType=none', '-p:DebugSymbols=false')
    }
    if ($EnableLegacyLink) { $publishArgs += '-p:EnableLegacyLink=true' }
    if ($OfficialRelease) { $publishArgs += '-p:LFOfficialRelease=true' }
    if ($OutputPath) { $publishArgs += @('--output', $OutputPath) }
    & $DotnetPath @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    # The C# product project owns its apphost/bundle identity and publish layout.
}
finally {
    try {
        if ([IO.Directory]::Exists($generatedFolder)) {
            $resolvedFolder = [IO.Path]::GetFullPath($generatedFolder)
            $rootPrefix = $generatedRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
            if (-not $resolvedFolder.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                [IO.Path]::GetFileName($resolvedFolder) -notmatch '\A[0-9a-f]{32}\z' -or
                (([IO.File]::GetAttributes($resolvedFolder) -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
                throw 'Unsafe modern credential-build cleanup path'
            }
            [IO.Directory]::Delete($resolvedFolder, $true)
        }
    } finally {
        if ($null -ne $buildLease) { $buildLease.Dispose() }
    }
}
