param([string]$RepositoryRoot)
$ErrorActionPreference = 'Stop'
$tools = Split-Path $PSScriptRoot -Parent
if (-not $RepositoryRoot) { $RepositoryRoot = Split-Path $tools -Parent }
$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('PclFixedSourceChecks-' + [Guid]::NewGuid().ToString('N'))
function Check([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Reject([scriptblock]$Action, [string]$Message) {
    $caught = $false
    try { & $Action | Out-Null } catch { $caught = $true }
    Check $caught $Message
}
try {
    # A second UTF-8 BOM parses as a command named U+FEFFparam without a
    # ParseError. Check the actual callers' declared parameters, not just parser
    # errors, before exporting anything. FolderScope intentionally has no param.
    $callers = [ordered]@{
        'Test-ToolboxMemory.ps1' = @('ExecutablePath', 'CompilerPath')
        'Test-WorldBackups.ps1' = @('ExecutablePath', 'EngineOnly')
        'Test-WorldBackupGroups.ps1' = @('ExecutablePath')
        'Test-ModpackFolderScope.ps1' = @()
        'Test-IconCompatibility.ps1' = @('ExecutablePath', 'OfficialExecutablePath', 'ProbeOfficialResources')
        'Test-AuditFixes.ps1' = @('ExecutablePath')
        'Test-BuildCredentials.ps1' = @('ExecutablePath', 'KeylessBuild')
        'Test-ModDependencies.ps1' = @('ExecutablePath', 'VbcPath', 'ReadOnlyLegacyModsFolder')
        'Test-ModDependencyOnline.ps1' = @('ExecutablePath', 'VbcPath')
    }
    foreach ($caller in $callers.GetEnumerator()) {
        $parseErrors = $null
        $tokens = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $tools $caller.Key), [ref]$tokens, [ref]$parseErrors)
        Check ($parseErrors.Count -eq 0) ('Caller parse errors: ' + $caller.Key)
        $expected = @($caller.Value)
        if ($expected.Count -eq 0) {
            Check ($null -eq $ast.ParamBlock) ('Parameterless caller unexpectedly declares parameters: ' + $caller.Key)
        } else {
            Check ($null -ne $ast.ParamBlock) ('Caller parameter block is missing; check repeated BOM/prefix commands: ' + $caller.Key)
            $actual = @($ast.ParamBlock.Parameters | ForEach-Object { $_.Name.VariablePath.UserPath })
            Check ($actual.Count -eq $expected.Count -and ($actual -join [char]0) -ceq ($expected -join [char]0)) ('Caller parameter metadata changed: ' + $caller.Key)
        }
    }
    Write-Output 'Fixed LF callers passed: all nine actual parameter contracts; eight declared param blocks and intentionally parameterless folder scope.'
    $isolated = Join-Path $fixture 'repository'
    [IO.Directory]::CreateDirectory($isolated) | Out-Null
    & git -C $isolated init --quiet --initial-branch migration/csharp-modern-dotnet
    if ($LASTEXITCODE -ne 0) { throw 'Cannot initialize the isolated object-reading fixture.' }
    $common = (& git -C $RepositoryRoot rev-parse --git-common-dir).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Actual baseline object database is unavailable.' }
    $commonPath = if ([IO.Path]::IsPathRooted($common)) { $common } else { Join-Path $RepositoryRoot $common }
    $objects = Join-Path ([IO.Path]::GetFullPath($commonPath)) 'objects'
    # Git alternates is a byte-oriented LF-delimited file, including on Windows.
    # Environment.NewLine would append CR to the object directory on that platform.
    $alternatesPath = Join-Path $isolated '.git/objects/info/alternates'
    [IO.File]::WriteAllText($alternatesPath, $objects.Replace('\', '/') + "`n", [Text.UTF8Encoding]::new($false))
    [byte[]]$alternateBytes = [IO.File]::ReadAllBytes($alternatesPath)
    Check ($alternateBytes[-1] -eq 10 -and $alternateBytes -notcontains 13 -and $alternateBytes -notcontains 92 -and
        -not ($alternateBytes.Length -ge 3 -and $alternateBytes[0] -eq 0xef -and $alternateBytes[1] -eq 0xbb -and $alternateBytes[2] -eq 0xbf)) 'Git alternates must have forward slashes, LF and no UTF-8 BOM/CR.'
    [IO.Directory]::CreateDirectory((Join-Path $isolated 'scripts/migration')) | Out-Null
    [IO.File]::Copy((Join-Path $tools 'Get-FixedLfSource.ps1'), (Join-Path $isolated 'scripts/Get-FixedLfSource.ps1'))
    $manifestPath = Join-Path $isolated 'scripts/migration/lf-fixed-source-baselines.json'
    [IO.File]::Copy((Join-Path $tools 'migration/lf-fixed-source-baselines.json'), $manifestPath)
    . (Join-Path $isolated 'scripts/Get-FixedLfSource.ps1')
    $manifestText = [IO.File]::ReadAllText($manifestPath)
    $manifest = $manifestText | ConvertFrom-Json
    $output = Join-Path $fixture 'exports'
    foreach ($entry in $manifest.sources.PSObject.Properties) {
        # A contradictory working file exists. Neither it nor any current bridge
        # may become the historical input after the old product tree is retired.
        $drifted = Join-Path $isolated $entry.Value.path
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($drifted)) | Out-Null
        [IO.File]::WriteAllText($drifted, 'Deliberately changed working source; this must never be read as LF baseline.')
        $path = Export-LfFixedSource -Name $entry.Name -Directory $output
        Check ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $entry.Value.sha256) ('Wrong exact source bytes: ' + $entry.Name)
        $license = Join-Path $output ('licenses/' + $entry.Value.revision + '.LICENCE')
        $expectedLicense = $manifest.revisions.PSObject.Properties[[string]$entry.Value.revision].Value.license.sha256
        Check ((Get-FileHash -LiteralPath $license -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $expectedLicense) 'The fixed governing LICENCE was not exported with the source.'
        Check ([IO.File]::Exists((Join-Path $output ($entry.Name + '.provenance.json')))) 'Missing source/adapter scope provenance.'
    }
    $again = Export-LfFixedSource -Name 'folder-policy-f4' -Directory $output
    Check ([IO.File]::Exists($again)) 'Idempotent exact export failed.'
    $text = Read-LfFixedSourceText -Name 'folder-policy-f4'
    Check ($text.Contains('Class MinecraftFolderPolicy') -and -not $text.Contains('Deliberately changed')) 'Text reader followed the changed working tree.'
    $original = [IO.File]::ReadAllBytes($again)
    $manifest.sources.PSObject.Properties['folder-policy-f4'].Value.sha256 = '0' * 64
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8))
    Reject { Export-LfFixedSource -Name 'folder-policy-f4' -Directory $output } 'Tampered pinned digest was accepted.'
    Check ([Convert]::ToBase64String($original) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes($again))) 'A failed digest check modified an existing source.'
    [IO.File]::WriteAllText($manifestPath, $manifestText)
    Reject { Export-LfFixedSource -Name 'unknown-source' -Directory $output } 'Unknown source key was accepted.'
    Reject { Export-LfFixedSource -Name 'folder-policy-f4' -Directory $RepositoryRoot } 'Exporter admitted a working-tree destination.'
    Reject { Export-LfFixedSource -Name 'folder-policy-f4' -Directory (Join-Path $isolated 'exports') } 'Exporter admitted a source-working-tree destination even inside a GUID temporary root.'
    $occupied = Join-Path $fixture 'occupied'
    $source = Join-Path $occupied 'Plain Craft Launcher 2/Modules/Minecraft/MinecraftFolderPolicy.vb'
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($source)) | Out-Null
    [IO.File]::WriteAllText($source, 'unknown content')
    Reject { Export-LfFixedSource -Name 'folder-policy-f4' -Directory $occupied } 'Unknown temporary file was overwritten.'
    Check ([IO.File]::ReadAllText($source) -ceq 'unknown content') 'Unknown file changed on rejection.'
    Write-Output ('Fixed LF inputs passed: ' + @($manifest.sources.PSObject.Properties).Count + ' exact source/blob/SHA256/LICENCE exports ignore changed working files; idempotence, text path, provenance and five rejection boundaries retained. No product build or network.')
} finally {
    if ([IO.Directory]::Exists($fixture)) {
        $full = [IO.Path]::GetFullPath($fixture)
        $prefix = ([IO.Path]::GetFullPath([IO.Path]::GetTempPath())).TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar
        if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -notmatch '^PclFixedSourceChecks-[a-f0-9]{32}$') { throw 'Unsafe fixed-source fixture cleanup.' }
        [IO.Directory]::Delete($fixture, $true)
    }
}
