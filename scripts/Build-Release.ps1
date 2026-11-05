param(
    [ValidateSet('Debug', 'Release', 'Snapshot')][string]$Configuration = 'Release',
    [string]$OutputPath,
    [string]$DotNetPath = 'dotnet',
    [string]$NativeProjectPath,
    [switch]$EnableLegacyLink
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = if ($NativeProjectPath) { [IO.Path]::GetFullPath($NativeProjectPath) } else { Join-Path $projectRoot 'modern/Main/PCL-Living-Fork.Modern.csproj' }
$output = if ($OutputPath) { [IO.Path]::GetFullPath($OutputPath) } else { Join-Path $projectRoot ('artifacts/release/' + $Configuration) }
# The native publisher owns the package/identity checks. Its modern inner
# publisher owns the ONLY C# credential GUID, build lease and finally cleanup.
# Historical credential-failure fixtures have a separate explicit test helper;
# this default entry never selects a business VB/vbproj or the managed bridge as Main.
Write-Output ('Building full LF C#/.NET10 ' + $Configuration + ' product: ' + $project)
& (Join-Path $PSScriptRoot 'Build-NativeRelease.ps1') -NativeProjectPath $project -OutputPath $output -Configuration $Configuration -DotNetPath $DotNetPath -EnableLegacyLink:$EnableLegacyLink
$indexPath = Join-Path $output 'NativeRelease.Artifacts.json'
$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
$expected = @('native-exe', 'native-zip', 'legacy-bridge', 'checksums')
$roles = @($index.artifacts | ForEach-Object { $_.role })
if ($index.schemaVersion -ne 1 -or $index.kind -cne 'PCLLF.NativeRelease.Artifacts' -or $index.bridgeOnly -isnot [bool] -or $index.bridgeOnly -or $roles.Count -ne $expected.Count -or @($expected | Where-Object { $_ -cnotin $roles }).Count -ne 0) {
    throw 'Default release requires the complete actual native EXE/ZIP/legacy bridge/checksums package.'
}
foreach ($item in @($index.artifacts)) { Write-Output ('Release role ' + $item.role + ': ' + $item.fileName + ' (' + $item.bytes + ' bytes, SHA256 ' + $item.sha256 + ')') }
Write-Output ('Build-only release index: ' + $indexPath + '; no release uploaded and no launcher/update entry executed.')
