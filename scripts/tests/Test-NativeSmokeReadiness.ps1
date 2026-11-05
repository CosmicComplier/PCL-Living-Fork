#requires -Version 5.1
param([string]$SmokeScript = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Test-NativeLauncherSmoke.ps1'))
$ErrorActionPreference = 'Stop'
# Parse the same on-disk source in the same Windows PowerShell 5.1 host as
# -File, without executing the product, profiles, native calls or CI guards.
if ($PSVersionTable.PSEdition -ne 'Desktop') { throw 'Use Windows PowerShell 5.1 to verify the actual source decoding contract.' }
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($SmokeScript), [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'The actual smoke script did not parse.' }
$readinessAssignments = @($ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -ceq '$allStages'
}, $true))
if ($readinessAssignments.Count -ne 1) { throw 'Expected the single actual cold-entry log readiness expression.' }
$predicate = [scriptblock]::Create('param($text, $installed, $expectedNotice) ' + $readinessAssignments[0].Right.Extent.Text)
$installed = 'D:\owned-ci\Install with spaces\LivingPCL Native Smoke.exe'
$notice = 'PCL Living Fork dev_edition ' + [regex]::Unescape('\u7248\u672c\u8bf4\u660e')
$fragments = @(
    [regex]::Unescape('\u7b2c\u4e00\u9636\u6bb5\u52a0\u8f7d\u7528\u65f6\uff1a'),
    [regex]::Unescape('\u7b2c\u4e8c\u9636\u6bb5\u52a0\u8f7d\u7528\u65f6\uff1a'),
    [regex]::Unescape('\u7b2c\u4e09\u9636\u6bb5\u52a0\u8f7d\u7528\u65f6\uff1a'),
    [regex]::Unescape('PCL \u4f4d\u7f6e\uff1a'),
    $installed,
    ([regex]::Unescape('\u666e\u901a\u5f39\u7a97\uff1a') + $notice)
)
$complete = $fragments -join [Environment]::NewLine
if ((& $predicate $complete $installed $notice) -cne $true) { throw 'Actual PS5 -File decoding lost complete Unicode startup receipts (check UTF-8 BOM).' }
for ($missing = 0; $missing -lt $fragments.Count; $missing++) {
    $incomplete = @(); for ($index = 0; $index -lt $fragments.Count; $index++) { if ($index -ne $missing) { $incomplete += $fragments[$index] } }
    if ((& $predicate ($incomplete -join [Environment]::NewLine) $installed $notice) -cne $false) { throw ('Incomplete receipt accepted: ' + $missing) }
}
if ((& $predicate $complete ($installed + '.other') $notice) -cne $false) { throw 'Foreign executable receipt accepted.' }
if ((& $predicate $complete $installed ($notice + ' changed')) -cne $false) { throw 'Unknown notice receipt accepted.' }
Write-Output 'PASS actual PS5 smoke source decoding/readiness: complete Unicode receipt accepted, six missing components and foreign path/notice rejected; no product/profile/input actions.'

