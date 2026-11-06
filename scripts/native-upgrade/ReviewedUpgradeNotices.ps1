#requires -Version 5.1
# Pure read-only log admission policy. This never touches a process/profile/UI.
function Get-ReviewedUpgradeCaption([string]$NativeVersion) {
 if($NativeVersion -cnotin @('dev_edition','1.4.0-pre-4','1.4.0')){throw 'PENDING: Native upgrade notice version has not been reviewed.'}
 $nl=[string][char]13+[char]10
 $highlights=if($NativeVersion -ceq '1.4.0-pre-4'){'- 主产品迁移至 C#/.NET 10，精简版需 .NET 10 Desktop Runtime'+$nl+'- 更新页新增版本搜索、当前版本标记与 Markdown 说明'+$nl+'- 修复图片、中文搜索、资源详情异常及删除版本卡顿'}else{'- 汇总四个预发布版本，包含依赖、主题、登录隔离与本次实机修复'+$nl+'- C#/.NET 10 与 Windows x64；运行前需 .NET 10 Desktop Runtime'+$nl+'- 完善依赖索引、内嵌前置识别与补全、深浅色主题和面板不透明度'+$nl+'- 原生更新页支持版本搜索、通道选择与 Markdown 更新说明'+$nl+'- 备份、克隆、恢复显示实际进度；存档与资源支持正倒序'+$nl+'- 微软登录缓存使用 _LF 隔离；修复图标、中文搜索与删除卡顿'+$nl+'- 整包更新保护个人修改和世界数据，修复配置误关联与冲突预览'}
 return 'PCL Living Fork '+$NativeVersion+$nl+$nl+$highlights
}
function Test-ReviewedStartupNotice([string]$Text,[string]$Notice,[string]$NativeVersion,[switch]$NativePhase) {
 if($Text -match '(?m)^\d{2}:\d{2}:\d{2}\.\d{3} E '){throw 'Actual Main logged an Error.'}
 if($Text -match '主窗体加载完成前出现意料外的等待弹窗|处理等待中的弹窗失败|弹窗初始化失败|弹窗加载失败'){throw 'Actual initialization/dialog failed.'}
 if($NativePhase -and $Notice -cne ('PCL Living Fork '+$NativeVersion+' 版本说明')){throw 'PENDING: Native notice phase/version binding differs.'}
 $ordinarySeen=$false;$upgradeRequested=$false
 foreach($dialog in [regex]::Matches($Text,'(?m)(?<Kind>普通|选择|输入)弹窗：(?<Title>[^\r\n]+)\r?\n')){
  $kind=$dialog.Groups['Kind'].Value;$title=$dialog.Groups['Title'].Value.Trim()
  if($kind -ceq '普通' -and $title -ceq $Notice){$ordinarySeen=$true}
  elseif($NativePhase -and $kind -ceq '普通' -and $title -ceq 'PCL Living Fork 更新说明'){$upgradeRequested=$true}
  else{throw ('PENDING: Unexpected actual dialog; never auto-clicked: '+$title)}
 }
 $upgradeVerified=$false;$upgradeIncomplete=$false
 if($upgradeRequested){
  $expected=@((Get-ReviewedUpgradeCaption $NativeVersion) -split '\r?\n' | Where-Object {$_.Length -gt 0})
  $records=[Collections.Generic.List[object]]::new();$current=$null
  foreach($line in [regex]::Matches($Text,'(?m)^\d{2}:\d{2}:\d{2}\.\d{3} I (?:<[^>\r\n]*> )?\[MessageDialogController\] (?<Content>[^\r\n]*)\r?\n')){
   $content=$line.Groups['Content'].Value.TrimEnd([char]13)
   $header=[regex]::Match($content,'^(?<Kind>普通|选择|输入)弹窗：(?<Title>.+)$')
   if($header.Success){
    if($null -ne $current){$current.Closed=$true}
    $current=[pscustomobject]@{Kind=$header.Groups['Kind'].Value;Title=$header.Groups['Title'].Value;Body=[Collections.Generic.List[string]]::new();Closed=$false}
    $records.Add($current)
   }elseif($null -ne $current -and $content.Length -gt 0){$current.Body.Add($content)}
  }
  $upgrades=@($records | Where-Object {$_.Kind -ceq '普通' -and $_.Title -ceq 'PCL Living Fork 更新说明'})
  if($upgrades.Count -ne 1){throw 'PENDING: Upgrade notice has no unique owned MessageDialogController log record.'}
  $record=$upgrades[0]
  if($record.Body.Count -gt $expected.Count){throw 'PENDING: Reviewed upgrade notice has extra caption lines.'}
  for($i=0;$i -lt $record.Body.Count;$i++){if($record.Body[$i] -cne $expected[$i]){throw 'PENDING: Reviewed upgrade notice caption differs.'}}
  if($record.Body.Count -lt $expected.Count){
   if($record.Closed){throw 'PENDING: Reviewed upgrade notice ended before its full caption.'}
   $upgradeIncomplete=$true
  }else{$upgradeVerified=$true}
 }
 # A partial final write must be completed before it can be a stable checkpoint.
 return ($Text.EndsWith([string][char]10) -and -not $upgradeIncomplete -and ($ordinarySeen -or $upgradeVerified))
}

