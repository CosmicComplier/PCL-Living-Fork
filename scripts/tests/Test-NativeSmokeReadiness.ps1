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
$policy=Join-Path (Split-Path $PSScriptRoot -Parent) 'native-upgrade/ReviewedUpgradeNotices.ps1'
foreach($path in @($policy,$SmokeScript,$PSCommandPath)){
    $bytes=[IO.File]::ReadAllBytes($path)
    if($bytes[0] -ne 239 -or $bytes[1] -ne 187 -or $bytes[2] -ne 191){throw 'Unicode PS5 source must retain its UTF-8 BOM.'}
}
. $policy
foreach($name in @('Check-Log','Assert-LfNormalClose')){
    $functions=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $name},$true))
    if($functions.Count -ne 1){throw ('Expected one actual smoke function: '+$name)}
    . ([scriptblock]::Create($functions[0].Extent.Text))
}
$titles=@($ast.FindAll({param($node)$node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -ceq '$expectedNotice'},$true))
if($titles.Count -ne 1){throw 'Expected the single metadata-bound smoke notice assignment.'}
$titleExpression=[scriptblock]::Create('param($proof) '+$titles[0].Right.Extent.Text)
$predicate = [scriptblock]::Create('param($text, $installed, $expectedNotice, $proof) ' + $readinessAssignments[0].Right.Extent.Text)
$installed = 'D:\owned-ci\Install with spaces\LivingPCL Native Smoke.exe'
$nl=[string][char]13+[char]10
$fragments=@('第一阶段加载用时：','第二阶段加载用时：','第三阶段加载用时：','PCL 位置：',$installed)
$stageLog=($fragments -join $nl)+$nl
$count=0
function Assert-Value([bool]$expected,[scriptblock]$action,[string]$label){
    $value=& $action
    if($value -isnot [bool] -or $value -ne $expected){throw ('Unexpected actual smoke readiness: '+$label)}
    $script:count++
}
function Assert-Refused([scriptblock]$action,[string]$label){
    $refused=$false;try{& $action | Out-Null}catch{$refused=$true}
    if(-not $refused){throw ('Unsafe smoke receipt accepted: '+$label)}
    $script:count++
}
function Message-Log([string]$title,[string]$caption,[string]$kind='普通'){
    $rows=@($kind+'弹窗：'+$title)+@($caption -split '\r?\n'|Where-Object {$_.Length -gt 0})
    return (($rows|ForEach-Object {'00:00:00.001 I [MessageDialogController] '+$_}) -join $nl)+$nl
}
foreach($route in @(
    @{Version='dev_edition';Style='plain';Source='local';Title='PCL Living Fork dev_edition 版本说明';Caption='legacy fixture'},
    @{Version='1.3.5-pre-3';Style='plain';Source='local';Title='PCL Living Fork 1.3.5-pre-3 版本说明';Caption='legacy fixture'},
    @{Version='dev_edition';Style='markdown-suppress';Source='local';Title='PCL Living Fork dev_edition 更新说明';Caption=('基于 PCL 官方开源版的第三方版本。'+$nl+'以下为当前版本的本地简述。')},
    @{Version='1.4.0';Style='markdown-suppress';Source='published-or-local';Title='PCL Living Fork 1.4.0 更新说明';Caption=('基于 PCL 官方开源版的第三方版本。'+$nl+'以下为当前版本的本地简述。')},
    @{Version='1.4.0';Style='markdown';Source='published-or-local';Title='PCL Living Fork 1.4.0 更新说明';Caption=('基于 PCL 官方开源版的第三方版本。'+$nl+'暂未读取到当前版本的完整发布说明，以下为本地简述。')}
)){
    $script:proof=[pscustomobject]@{displayName='PCL Living Fork';versionBaseName=$route.Version;buildType=50;startupNoticeTitle=$route.Title;startupNoticeStyle=$route.Style;startupNoticeSource=$route.Source}
    $notice=& $titleExpression $proof
    if($notice -cne $route.Title){throw 'Actual smoke notice assignment ignored artifact metadata.'};$count++
    $noticeLog=Message-Log $notice $route.Caption
    $complete=$stageLog+$noticeLog
    Assert-Value $true {& $predicate $complete $installed $notice $proof} 'complete Unicode receipt'
    Check-Log $complete $notice -RequireComplete
    for($missing=0;$missing -lt $fragments.Count;$missing++){
        $incomplete=@();for($index=0;$index -lt $fragments.Count;$index++){if($index -ne $missing){$incomplete+=$fragments[$index]}}
        Assert-Value $false {& $predicate (($incomplete -join $nl)+$nl+$noticeLog) $installed $notice $proof} 'missing original stage/path'
    }
    Assert-Value $false {& $predicate $stageLog $installed $notice $proof} 'missing notice'
    Assert-Value $false {& $predicate $complete ($installed+'.other') $notice $proof} 'foreign executable receipt'
    Assert-Refused {& $predicate $complete $installed ($notice+' changed') $proof} 'unknown expected title'
    foreach($kind in @('选择','输入')){Assert-Refused {Check-Log ($stageLog+(Message-Log $notice $route.Caption $kind)) $notice} 'unsafe same-title kind'}
    Assert-Refused {Check-Log ($complete+(Message-Log '删除所有版本？' 'unsafe')) $notice} 'unknown destructive overlay'
    Assert-Refused {Check-Log ($complete+(Message-Log '' 'unsafe')) $notice} 'empty-title overlay'
    Assert-Refused {Check-Log ($complete+'00:00:01.000 E [Actual] failure'+$nl) $notice} 'Error receipt'
    Assert-Value $false {& $predicate ($complete.TrimEnd([char]13,[char]10)) $installed $notice $proof} 'partial final log write'
    $closing='00:00:01.001 I [Owned] 收到关闭指令'+$nl+'00:00:01.002 I [Owned] 程序已退出，返回值：Success'+$nl
    Assert-LfNormalClose -1 ($complete+$closing) $notice;$count++
    Assert-Refused {Assert-LfNormalClose 0 ($complete+$closing) $notice} 'original self-Kill exit retained'
    if($route.Style -cne 'plain'){
        Assert-Value $false {& $predicate ($stageLog+(Message-Log $notice '')) $installed $notice $proof} 'title-only Markdown pending'
        $partial=$stageLog+(Message-Log $notice '基于 PCL 官方开源版的第三方版本。')
        Assert-Value $false {& $predicate $partial $installed $notice $proof} 'incomplete Markdown caption pending'
        Assert-Refused {Assert-LfNormalClose -1 ($partial+$closing) $notice} 'incomplete caption cannot close'
        Assert-Refused {Check-Log ($stageLog+(Message-Log $notice ($route.Caption+$nl+'extra'))) $notice} 'extra Markdown caption line'
        Assert-Refused {Check-Log ($stageLog+(Message-Log $notice ($route.Caption+' changed'))) $notice} 'changed Markdown caption'
        Assert-Refused {Check-Log ($complete+$noticeLog) $notice} 'duplicate Markdown records'
        Assert-Refused {Check-Log $complete.Replace('[MessageDialogController]','[Unknown]') $notice} 'caption logger ownership'
    }
    $proof.startupNoticeTitle+=' changed'
    Assert-Refused {& $titleExpression $proof} 'changed artifact title'
}
Assert-Refused {Get-ReviewedNativeStartupNotice ([pscustomobject]@{displayName='PCL Living Fork';versionBaseName='1.4.1';startupNoticeStyle='markdown-suppress';startupNoticeTitle='PCL Living Fork 1.4.1 更新说明';startupNoticeSource='local';buildType=50})} 'unreviewed future artifact'
Write-Output ('PASS '+$count+' actual PS5 smoke source/readiness/close regressions: metadata-bound plain and Markdown routes; unknown/kind/body/Error/partial records refused or pending. No product/profile/process/window/input action.')

