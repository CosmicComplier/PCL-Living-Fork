# Independent historical source-mode gate only. Never build/load a product
# Application, a native apphost or the managed update bridge as CLR4 Main.
# The original Test-BuildCredentials owns the failure/invalid-input/lock samples;
# this entry supplies only its fixed LF credential foundation in an owned DLL.
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -cne 'Desktop' -or $PSVersionTable.PSVersion.Major -ne 5 -or [Runtime.InteropServices.RuntimeEnvironment]::GetSystemVersion() -cnotmatch '\Av4\.0\.') {
    throw 'Historical credential failure contracts require genuine Windows PowerShell5 Desktop/CLR4; no PowerShell7 or Linux success fallback.'
}
$tools = Split-Path $PSScriptRoot -Parent
. (Join-Path $tools 'Get-FixedLfSource.ps1')
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('PclHistoricalCredentialGate-' + [Guid]::NewGuid().ToString('N'))
$credentials = @('PCL_CURSEFORGE_API_KEY', 'PCL_MS_CLIENT_ID')
$saved = @{}
foreach ($name in ($credentials + @('TEMP', 'TMP'))) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    [IO.Directory]::CreateDirectory($temporary) | Out-Null
    $null = Assert-LfFixedTemporaryDirectory $temporary
    $foundationPath = Export-LfFixedSource -Name 'credentials-foundation-f4' -Directory $temporary
    $foundationBefore = [IO.File]::ReadAllBytes($foundationPath)
    $foundation = [IO.File]::ReadAllText($foundationPath, [Text.UTF8Encoding]::new($false, $true))
    $assemblyPath = Join-Path $temporary 'LF.Historical.CredentialFixture.dll'
    # Both compiler-generated helper files and the unchanged historical child
    # must stay inside this GUID root; restore the caller's values in finally.
    [Environment]::SetEnvironmentVariable('TEMP', $temporary)
    [Environment]::SetEnvironmentVariable('TMP', $temporary)
    Add-Type -TypeDefinition @'
using System;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic;
public static class LfHistoricalCredentialFixtureCompiler {
    public static void Compile(string source, string output) {
        using (var provider = new VBCodeProvider()) {
            var parameters = new CompilerParameters(new[] { "System.dll", "System.Core.dll", "System.Security.dll" });
            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = false;
            parameters.OutputAssembly = output;
            parameters.TempFiles = new TempFileCollection(System.IO.Path.GetDirectoryName(output), false);
            parameters.CompilerOptions = "/rootnamespace:PCL /optioninfer+";
            var result = provider.CompileAssemblyFromSource(parameters, source);
            foreach (CompilerError error in result.Errors)
                if (!error.IsWarning) throw new InvalidOperationException("Historical credential-only compilation failed: " + error.ErrorNumber + " line " + error.Line);
            // Do not read CompiledAssembly: only the child may load this DLL.
        }
    }
}
'@
    [LfHistoricalCredentialFixtureCompiler]::Compile($foundation, $assemblyPath)
    # Byte-load metadata only. Loading from the file in this parent would keep
    # its CLR4 image locked until process exit and prevent owned-root cleanup.
    $metadata = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($assemblyPath))
    if ($metadata.GetName().Name -cne 'LF.Historical.CredentialFixture' -or $metadata.ImageRuntimeVersion -cne 'v4.0.30319' -or $null -eq $metadata.GetType('PCL.BuildCredentials', $false)) { throw 'Historical fixture is not its explicit credential-only CLR4 module.' }
    foreach ($name in @('PCL.Application', 'PCL.FormMain', 'PCL.ModMain', 'PCL.ModSecret', 'PCL.ModLaunch')) {
        if ($null -ne $metadata.GetType($name, $false)) { throw 'A product/Main type entered the credential-only historical fixture.' }
    }
    foreach ($reference in $metadata.GetReferencedAssemblies()) {
        if ($reference.Name -in @('LivingPCL', 'PCLCS', 'PCLLF.Presentation', 'PresentationFramework', 'PresentationCore')) { throw 'Credential-only fixture unexpectedly references a product or WPF assembly.' }
    }
    # No release credentials enter this historical child. The original script
    # creates its own synthetic inputs and restores this empty child environment.
    foreach ($name in $credentials) { [Environment]::SetEnvironmentVariable($name, $null) }
    $desktop = Join-Path $PSHOME 'powershell.exe'
    if (-not [IO.File]::Exists($desktop)) { throw 'The current genuine Desktop PowerShell executable is unavailable.' }
    Write-Output 'Historical credential source-mode scope: owned fixed-f4 credential-only DLL; no product Main, bridge, startup, profile, Registry or network.'
    & $desktop -NoProfile -NonInteractive -File (Join-Path $tools 'Test-BuildCredentials.ps1') -ExecutablePath $assemblyPath -KeylessBuild
    $childExitCode = $LASTEXITCODE
    if ($childExitCode -ne 0) { throw ('Original historical credential source-mode contracts failed with exit code ' + $childExitCode) }
    if ((Get-LfFixedBytesHash $foundationBefore) -cne (Get-LfFixedBytesHash ([IO.File]::ReadAllBytes($foundationPath)))) { throw 'The fixed credential foundation changed during the original contracts.' }
    Write-Output 'PASS historical credential source-mode: original FailBuild23, valid/invalid input cleanup, fixed-source immutability and exclusive build.lock samples executed unchanged; no product/Main build or Native acceptance implied.'
}
finally {
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    $saved.Clear()
    if ([IO.Directory]::Exists($temporary)) {
        $full = Assert-LfFixedTemporaryDirectory $temporary
        if ([IO.Path]::GetFileName($full) -cnotmatch '\APclHistoricalCredentialGate-[a-f0-9]{32}\z') { throw 'Unsafe historical credential-only fixture cleanup.' }
        foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($full, '*', [IO.SearchOption]::AllDirectories)) {
            if (([IO.File]::GetAttributes($entry) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Historical credential-only cleanup refuses linked entries.' }
        }
        [IO.Directory]::Delete($full, $true)
    }
}
