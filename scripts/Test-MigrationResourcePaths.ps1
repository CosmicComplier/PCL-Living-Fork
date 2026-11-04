param(
    [ValidateSet('Diagnostic','RequireEquivalent')][string]$Mode = 'RequireEquivalent',
    [string]$ReportPath = 'artifacts/migration/resource-paths.json'
)
# Windows PowerShell 5.1 compatible. No LF source/algorithm is translated here.
# Fixed LF sources retain repository LICENCE/NOTICE.md; Shared retains its LICENSE.txt.
# Diagnostic records runtime differences; baseline/contract/protocol errors always fail.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'This oracle requires Windows, CLR4/net48 reference assemblies, and .NET 10.' }
$repo = Split-Path $PSScriptRoot -Parent
$lfRevision = 'f4aa0fae62c3ee8af24c81e61844c8fdd13c8983'
$sharedRevision = 'c774ae4c85fd94e0eaa171db21e78a65870df19b'
$utf8 = New-Object Text.UTF8Encoding($false, $true)
$gitCommand = (Get-Command git -ErrorAction Stop).Source
$dotnetCommand = (Get-Command dotnet -ErrorAction Stop).Source
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('PclResourcePathOracle-' + [Guid]::NewGuid().ToString('N'))
$report = if ([IO.Path]::IsPathRooted($ReportPath)) { [IO.Path]::GetFullPath($ReportPath) } else { [IO.Path]::GetFullPath((Join-Path $repo $ReportPath)) }
$hostExecutable = Join-Path $fixture 'host/PCLLF.PathOracle.exe'
$linksMayExist = $false

function Invoke-Checked([string]$FileName, [string[]]$NativeArguments, [string]$WorkingDirectory = $repo) {
    $savedProcessDirectory = [Environment]::CurrentDirectory
    Push-Location -LiteralPath $WorkingDirectory
    try {
        [Environment]::CurrentDirectory = $WorkingDirectory
        & $FileName @NativeArguments
        if ($LASTEXITCODE -ne 0) { throw ($FileName + ' exited with code ' + $LASTEXITCODE) }
    } finally {
        [Environment]::CurrentDirectory = $savedProcessDirectory
        Pop-Location
    }
}

function Export-GitBlob([string]$RevisionPath, [string]$Destination) {
    # Read the process byte stream, not PS 5.1's ANSI/native-output conversion.
    # All revision/path values are fixed literals below, so no command shell is used.
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $gitCommand
    $start.WorkingDirectory = $repo
    $start.Arguments = 'show "' + $RevisionPath + '"'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'git show could not start' }
        $errorText = $process.StandardError.ReadToEndAsync()
        $stream = [IO.File]::Create($Destination)
        try { $process.StandardOutput.BaseStream.CopyTo($stream) } finally { $stream.Dispose() }
        $process.WaitForExit()
        $diagnostic = $errorText.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw ('git show failed: ' + $RevisionPath + ': ' + $diagnostic) }
    } finally { $process.Dispose() }
}

try {
    [IO.Directory]::CreateDirectory($fixture) | Out-Null
    $sharedRepo = Join-Path $repo 'MeloongCore'
    $actualSharedRevision = & $gitCommand -C $sharedRepo rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $actualSharedRevision -cne $sharedRevision) { throw 'Initialize MeloongCore at fixed c774ae4c85fd94e0eaa171db21e78a65870df19b before running.' }
    # An archive isolates build products and compiles the actual pinned tracked Shared source.
    $archive = Join-Path $fixture 'shared.zip'
    Invoke-Checked $gitCommand @('-C', $sharedRepo, 'archive', '--format=zip', ('--output=' + $archive), $sharedRevision, 'Shared', 'LICENSE.txt')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $sharedSource = Join-Path $fixture 'shared-source'
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $sharedSource)
    $sharedOutput = Join-Path $fixture 'shared-bin'
    $sharedOutProperty = $sharedOutput.Replace('\','/') + '/'
    Invoke-Checked $dotnetCommand @('build', (Join-Path $sharedSource 'Shared/MeloongCore.csproj'), '-c', 'Release', '--nologo', ('-p:OutDir=' + $sharedOutProperty), '-p:CopyLocalLockFileAssemblies=true')
    $sharedAssembly = Join-Path $sharedOutput 'MeloongCore.dll'
    if (-not [IO.File]::Exists($sharedAssembly)) { throw 'The fixed real Shared assembly was not produced.' }

    $baselineDirectory = Join-Path $fixture 'baseline'
    [IO.Directory]::CreateDirectory($baselineDirectory) | Out-Null
    $oldSource = Join-Path $baselineDirectory 'InstanceResourceStore.f4aa0fa.vb'
    $manifest = Join-Path $baselineDirectory 'app.manifest'
    $baselineExecutable = Join-Path $baselineDirectory 'baseline.exe'
    $config = $baselineExecutable + '.config'
    $newtonsoft = Join-Path $baselineDirectory 'Newtonsoft.Json.dll'
    Export-GitBlob ($lfRevision + ':Plain Craft Launcher 2/Modules/Minecraft/InstanceResourceStore.vb') $oldSource
    Export-GitBlob ($lfRevision + ':Plain Craft Launcher 2/app.manifest') $manifest
    Export-GitBlob ($lfRevision + ':Plain Craft Launcher 2/App.config') $config
    Export-GitBlob ($lfRevision + ':Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll') $newtonsoft
    if ((Get-FileHash -LiteralPath $newtonsoft -Algorithm SHA256).Hash -cne (Get-FileHash -LiteralPath (Join-Path $repo 'Plain Craft Launcher 2/Resources/Newtonsoft.Json.dll') -Algorithm SHA256).Hash) { throw 'Current LF Newtonsoft differs from the fixed original DLL; review the dependency before comparing.' }

    $sourceText = [IO.File]::ReadAllText($oldSource, $utf8)
    $normalize = [regex]::Matches($sourceText, '(?ms)^    Private Shared Function Normalize\(Value As String\) As String\r?\n.*?^    End Function\r?$')
    $ensure = [regex]::Matches($sourceText, '(?ms)^    Friend Shared Sub EnsureWithin\(Root As String, FileName As String, Optional AllowRoot As Boolean = False\)\r?\n.*?^    End Sub\r?$')
    $formatSize = [regex]::Matches($sourceText, '(?ms)^    Private Shared Function FormatSize\(Size As Long\) As String\r?\n.*?^    End Function\r?$')
    if ($normalize.Count -ne 1 -or $ensure.Count -ne 1 -or $formatSize.Count -ne 1) { throw 'Fixed LF method extraction did not match exactly one Normalize, EnsureWithin and FormatSize.' }
    $packMetadata = [regex]::Matches($sourceText, '(?ms)^            If Metadata IsNot Nothing Then\r?\n.*?^            End If\r?$')
    if ($packMetadata.Count -ne 1) { throw 'Fixed LF source did not contain exactly one pack metadata JSON block.' }
    $driverTemplate = [IO.File]::ReadAllText((Join-Path $repo 'tests/PCLLF.PathOracle/LegacyDriver.vb'), $utf8)
    $methodMarker = '__LF_METHODS__'
    # The explanatory comment also names the marker; replace only its entire line.
    if ([regex]::Matches($driverTemplate, '(?m)^' + $methodMarker + '\r?$').Count -ne 1) { throw 'Baseline driver method marker is missing or repeated.' }
    $driver = [regex]::Replace($driverTemplate, '(?m)^' + $methodMarker + '\r?$', [Text.RegularExpressions.MatchEvaluator]{param($match) $normalize[0].Value + [Environment]::NewLine + $ensure[0].Value + [Environment]::NewLine + $formatSize[0].Value})
    $metadataMarker = '__LF_PACK_METADATA__'
    if ([regex]::Matches($driver, '(?m)^' + $metadataMarker + '\r?$').Count -ne 1) { throw 'Baseline metadata snippet marker is missing or repeated.' }
    $driver = [regex]::Replace($driver, '(?m)^' + $metadataMarker + '\r?$', [Text.RegularExpressions.MatchEvaluator]{param($match) $packMetadata[0].Value})
    $snippetHasher = [Security.Cryptography.SHA256]::Create()
    try { $snippetHash = [BitConverter]::ToString($snippetHasher.ComputeHash($utf8.GetBytes($packMetadata[0].Value))).Replace('-', '') } finally { $snippetHasher.Dispose() }
    $driverPath = Join-Path $baselineDirectory 'LegacyDriver.vb'
    [IO.File]::WriteAllText($driverPath, $driver, $utf8)
    foreach ($dependency in (Get-ChildItem -LiteralPath $sharedOutput -Filter '*.dll' -File)) {
        if ($dependency.Name -ine 'Newtonsoft.Json.dll') { [IO.File]::Copy($dependency.FullName, (Join-Path $baselineDirectory $dependency.Name), $true) }
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not [IO.File]::Exists($vswhere)) { throw 'vswhere.exe is required to locate the installed Visual Basic compiler.' }
    $compilerCandidates = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/Current/Bin/Roslyn/vbc.exe')
    if ($LASTEXITCODE -ne 0 -or $compilerCandidates.Count -eq 0) { throw 'No installed Visual Basic compiler was found.' }
    $compiler = $compilerCandidates[0]
    $references = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8'
    foreach ($required in @('mscorlib.dll','System.dll','System.Core.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Facades/netstandard.dll')) {
        if (-not [IO.File]::Exists((Join-Path $references $required))) { throw ('Missing net48 reference assembly: ' + $required) }
    }
    $compilerArguments = @('/nologo', '/codepage:65001', '/target:exe', '/platform:anycpu', '/optionstrict+', '/optionexplicit+', '/optioninfer+', '/optioncompare:binary',
        '/rootnamespace:PCLLF.PathOracleBaseline', '/main:PCLLF.PathOracleBaseline.OracleProgram', ('/sdkpath:' + $references), ('/out:' + $baselineExecutable), ('/win32manifest:' + $manifest),
        ('/reference:' + (Join-Path $references 'System.dll')), ('/reference:' + (Join-Path $references 'System.Core.dll')), ('/reference:' + (Join-Path $references 'Facades/netstandard.dll')),
        ('/reference:' + (Join-Path $references 'System.IO.Compression.dll')), ('/reference:' + (Join-Path $references 'System.IO.Compression.FileSystem.dll')),
        ('/reference:' + $sharedAssembly), ('/reference:' + $newtonsoft), $driverPath)
    Invoke-Checked $compiler $compilerArguments
    $provenance = [ordered]@{
        lf = $lfRevision; shared = $sharedRevision; platform = 'anycpu'; prefer32Bit = $false
        originalSourceSha256 = (Get-FileHash -LiteralPath $oldSource -Algorithm SHA256).Hash
        extractedDriverSha256 = (Get-FileHash -LiteralPath $driverPath -Algorithm SHA256).Hash
        originalPackMetadataSnippetSha256 = $snippetHash
        packMetadataSnippetScope = 'Only the fixed ReadPackInfo JSON block; filesystem/image work and outer catch remain outside the wrapper.'
        packMetadataLineEndingsHelper = 'MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(text, newValue, mergeMultiple=false), Shared c774ae4c85fd94e0eaa171db21e78a65870df19b'
        appConfigSha256 = (Get-FileHash -LiteralPath $config -Algorithm SHA256).Hash
        applicationManifestSha256 = (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash
        sharedAssemblySha256 = (Get-FileHash -LiteralPath $sharedAssembly -Algorithm SHA256).Hash
        newtonsoftAssemblySha256 = (Get-FileHash -LiteralPath $newtonsoft -Algorithm SHA256).Hash
        modernProductionSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourcePaths.cs') -Algorithm SHA256).Hash
        modernPathTextSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourcePathText.cs') -Algorithm SHA256).Hash
        modernPathCompatibilitySourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourcePathCompatibility.cs') -Algorithm SHA256).Hash
        modernPackageRulesSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourcePackageRules.cs') -Algorithm SHA256).Hash
        modernDetailsRulesSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourceDetailsRules.cs') -Algorithm SHA256).Hash
        modernPackMetadataCompatibilitySourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/InstanceResourcePackMetadataCompatibility.cs') -Algorithm SHA256).Hash
        modernLegacyPathCharactersSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'PCLCS/Minecraft/LegacyPathCharacters.cs') -Algorithm SHA256).Hash
    }
    [IO.File]::WriteAllText((Join-Path $baselineDirectory 'provenance.json'), ($provenance | ConvertTo-Json -Depth 5), $utf8)

    $hostOutput = (Join-Path $fixture 'host').Replace('\','/') + '/'
    $hostIntermediate = (Join-Path $fixture 'host-obj').Replace('\','/') + '/'
    Invoke-Checked $dotnetCommand @('build', (Join-Path $repo 'tests/PCLLF.PathOracle/PCLLF.PathOracle.csproj'), '-c', 'Release', '--nologo',
        ('-p:OutDir=' + $hostOutput), ('-p:BaseIntermediateOutputPath=' + $hostIntermediate), ('-p:MSBuildProjectExtensionsPath=' + $hostIntermediate),
        ('-p:PathOracleSharedAssembly=' + $sharedAssembly), ('-p:PathOracleNewtonsoftAssembly=' + $newtonsoft), ('-p:ApplicationManifest=' + $manifest))
    if (-not [IO.File]::Exists($hostExecutable)) { throw 'Native .NET 10 apphost exe missing; do not substitute dotnet.exe with a different manifest.' }
    # Prepare may partially create links before throwing. Cleanup must still unlink them first.
    $linksMayExist = $true
    Invoke-Checked $hostExecutable @('prepare', $fixture)
    $cases = Join-Path $fixture 'cases.json'
    $baseline = Join-Path $fixture 'baseline.json'
    $cwd = Join-Path $fixture 'work'
    Invoke-Checked $baselineExecutable @($cases, $baseline) $cwd
    Invoke-Checked $hostExecutable @('compare', $cases, $baseline, $report, $Mode) $cwd
    Write-Host ('Resource path oracle report: ' + $report)
} finally {
    $fullFixture = [IO.Path]::GetFullPath($fixture).TrimEnd('\')
    $temporaryPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $fullFixture.StartsWith($temporaryPrefix, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($fullFixture) -cnotmatch '\APclResourcePathOracle-[a-f0-9]{32}\z') { throw 'Unsafe path oracle cleanup.' }
    # Never recurse while any recorded directory/file link remains. Failure retains the fixture.
    if ($linksMayExist) { Invoke-Checked $hostExecutable @('cleanup', $fullFixture) }
    if ([IO.Directory]::Exists($fullFixture)) { [IO.Directory]::Delete($fullFixture, $true) }
}
