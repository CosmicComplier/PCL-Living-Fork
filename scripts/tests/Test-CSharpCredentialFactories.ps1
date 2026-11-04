param([string]$DotNetPath='dotnet')
# Actual CSharp product credential foundation + actual build generator, with
# synthetic inputs only. No Main/Core/WPF build, account, Registry or HTTP.
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$tools=Split-Path $PSScriptRoot -Parent
$root=Split-Path $tools -Parent
$temporary=Join-Path ([IO.Path]::GetTempPath()) ('PclCSharpCredentials-'+[Guid]::NewGuid().ToString('N'))
$saved=@{};foreach($name in @('PCL_CURSEFORGE_API_KEY','PCL_MS_CLIENT_ID')){$saved[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
 [IO.Directory]::CreateDirectory($temporary)|Out-Null
 $checkerProject=Join-Path $PSScriptRoot 'native-credentials/NativeCredentialChecks.csproj'
 $checker=Join-Path $temporary 'checker';$checkerObj=Join-Path $temporary 'checker-obj/'
 & $DotNetPath build $checkerProject -c Release --output $checker ("-p:BaseIntermediateOutputPath=$checkerObj") ("-p:MSBuildProjectExtensionsPath=$checkerObj") -warnaserror --disable-build-servers -nodeReuse:false -m:1 -v:q
 if($LASTEXITCODE -ne 0){throw 'Actual credential factory checker failed to compile.'}
 $foundation=Join-Path $root 'modern/Main/Modules/Base/BuildCredentials.cs'
 $foundationHash=(Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash
 $fixture=Join-Path $temporary 'source';[IO.Directory]::CreateDirectory($fixture)|Out-Null
 [IO.File]::Copy($foundation,(Join-Path $fixture 'BuildCredentials.cs'))
 $project=Join-Path $fixture 'CredentialSourceFixture.csproj'
 [IO.File]::WriteAllText($project,'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType><Nullable>disable</Nullable><LangVersion>14.0</LangVersion></PropertyGroup></Project>')
 $generated=Join-Path $fixture 'BuildCredentials.g.cs'
 $fakeCF='synthetic-cf-'+[Guid]::NewGuid().ToString('N')+'-quote"-slash\-unicode-'+[char]0x266B
 $fakeMS='synthetic-ms-'+[Guid]::NewGuid().ToString('N')
 $caseNumber=0
 foreach($case in @(@($null,$null,$false),@($fakeCF,$null,$false),@($null,$fakeMS,$false),@($fakeCF,$fakeMS,$false),@($fakeCF,$fakeMS,$true))) {
  [Environment]::SetEnvironmentVariable('PCL_CURSEFORGE_API_KEY',$case[0]);[Environment]::SetEnvironmentVariable('PCL_MS_CLIENT_ID',$case[1])
  & (Join-Path $tools 'Write-BuildCredentials.ps1') -OutputPath $generated -SourceLanguage CSharp -NoObfuscation:([bool]$case[2])
  $text=[IO.File]::ReadAllText($generated)
  foreach($value in @($fakeCF,$fakeMS)){if($text.Contains($value)){throw 'Generated CSharp factory contains a plaintext synthetic test input.'}}
  $output=Join-Path $temporary ('case'+$caseNumber)
  & $DotNetPath build $project -c Release --output $output -warnaserror --disable-build-servers -nodeReuse:false -m:1 -v:q
  if($LASTEXITCODE -ne 0){throw ('Actual CSharp credential source/generator did not compile for case '+$caseNumber)}
  foreach($repeat in @(1,2)) {
   & $DotNetPath (Join-Path $checker 'NativeCredentialChecks.dll') --synthetic-factory (Join-Path $output 'CredentialSourceFixture.dll')
   if($LASTEXITCODE -ne 0){throw ('Actual CSharp factory readback/crypto/plaintext/buffer contract failed for case '+$caseNumber)}
  }
  $caseNumber++
 }
 if((Get-FileHash -LiteralPath $foundation -Algorithm SHA256).Hash -cne $foundationHash){throw 'Product credential source was changed by a factory test.'}
 Write-Output 'PASS actual CSharp credential source/generator: empty/CF-only/MS-only/both/NoObfuscation, quote/slash/Unicode, repeated readback, binary plaintext/Base64 absence, override, authenticated tamper/UTF8/padding and clear-share failure/success contracts. Synthetic factories only; not Main entry acceptance.'
}
finally {
 foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name])}
 $full=[IO.Path]::GetFullPath($temporary);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
 if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($full) -cnotmatch '\APclCSharpCredentials-[a-f0-9]{32}\z' -or ([IO.Directory]::Exists($full) -and (([IO.File]::GetAttributes($full) -band [IO.FileAttributes]::ReparsePoint) -ne 0))){throw 'Unsafe CSharp factory fixture cleanup path.'}
 if([IO.Directory]::Exists($full)){[IO.Directory]::Delete($full,$true)}
}
