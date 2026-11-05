# Historical test inputs only. No current VB business source is loaded here.
# Each exported byte sequence and its governing LF LICENCE are pinned together.
$script:LfFixedSourceRepository = Split-Path $PSScriptRoot -Parent
$script:LfFixedSourceManifest = Join-Path $PSScriptRoot 'migration/lf-fixed-source-baselines.json'

function Get-LfFixedBytesHash([byte[]]$Bytes, [string]$Algorithm = 'SHA256') {
    $hasher = [Security.Cryptography.HashAlgorithm]::Create($Algorithm)
    try { return ([BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant() }
    finally { $hasher.Dispose() }
}
function Read-LfFixedGitBlob([string]$Revision, [string]$Path) {
    $allowed = @('f4aa0fae62c3ee8af24c81e61844c8fdd13c8983', 'a1d2ee1ed215647ab7e38f11c9e45859fbb4fcf7', '50b028da5fcad2226d74d901a41d8c43a249bfb2')
    if ($allowed -cnotcontains $Revision -or $Path.Contains('"') -or $Path.Contains("`n") -or $Path.Contains("`r")) { throw 'Invalid fixed LF source object.' }
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
    $start.WorkingDirectory = $script:LfFixedSourceRepository
    $start.Arguments = 'show "' + $Revision + ':' + $Path + '"'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    $stream = New-Object IO.MemoryStream
    try {
        if (-not $process.Start()) { throw 'Could not start fixed LF Git object extraction.' }
        $errors = $process.StandardError.ReadToEndAsync()
        $process.StandardOutput.BaseStream.CopyTo($stream)
        $process.WaitForExit()
        $diagnostic = $errors.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw ('Missing fixed LF Git object ' + $Revision + ':' + $Path + '. Checkout full history (fetch-depth:0); no working-tree fallback. ' + $diagnostic) }
        return ,$stream.ToArray()
    } finally { $stream.Dispose(); $process.Dispose() }
}
function Assert-LfFixedPayload($Record, [byte[]]$Bytes) {
    if ($Bytes.Length -ne $Record.length -or (Get-LfFixedBytesHash $Bytes) -cne $Record.sha256) { throw ('Fixed LF SHA256/length mismatch: ' + $Record.path) }
    [byte[]]$header = [Text.Encoding]::ASCII.GetBytes('blob ' + $Bytes.Length + [char]0)
    [byte[]]$object = New-Object byte[] ($header.Length + $Bytes.Length)
    [Buffer]::BlockCopy($header, 0, $object, 0, $header.Length)
    [Buffer]::BlockCopy($Bytes, 0, $object, $header.Length, $Bytes.Length)
    if ((Get-LfFixedBytesHash $object 'SHA1') -cne $Record.blob) { throw ('Fixed LF Git blob identity mismatch: ' + $Record.path) }
}
function Assert-LfFixedTemporaryDirectory([string]$Directory) {
    $full = [IO.Path]::GetFullPath($Directory)
    $prefix = ([IO.Path]::GetFullPath([IO.Path]::GetTempPath())).TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or $full -notmatch '[\\/][A-Za-z][A-Za-z0-9_-]*-[a-f0-9]{32}([\\/]|$)') { throw 'Fixed LF exports require a GUID-owned temporary fixture.' }
    $repository = ([IO.Path]::GetFullPath($script:LfFixedSourceRepository)).TrimEnd([char[]]'\/')
    if ($full.Equals($repository, [StringComparison]::OrdinalIgnoreCase) -or $full.StartsWith($repository + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixed LF exports cannot target the source working tree.' }
    $current = New-Object IO.DirectoryInfo($full)
    while ($current -and $current.FullName.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        if ($current.Exists -and ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Fixed LF fixture contains a directory link.' }
        $current = $current.Parent
    }
    return $full
}
function Write-LfFixedPayload([string]$Path, [byte[]]$Bytes) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    if ([IO.File]::Exists($Path)) {
        if (([IO.File]::GetAttributes($Path) -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or (Get-LfFixedBytesHash ([IO.File]::ReadAllBytes($Path))) -cne (Get-LfFixedBytesHash $Bytes)) { throw ('Refusing to replace an unknown fixed-source fixture file: ' + $Path) }
        return
    }
    $output = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $output.Write($Bytes, 0, $Bytes.Length) } finally { $output.Dispose() }
}
function Export-LfFixedSource([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$true)][string]$Directory) {
    $directory = Assert-LfFixedTemporaryDirectory $Directory
    $manifest = [IO.File]::ReadAllText($script:LfFixedSourceManifest) | ConvertFrom-Json
    $property = $manifest.sources.PSObject.Properties[$Name]
    if ($manifest.version -ne 1 -or $null -eq $property) { throw ('Unknown fixed LF source key: ' + $Name) }
    $source = $property.Value
    $revision = $manifest.revisions.PSObject.Properties[[string]$source.revision]
    if ($null -eq $revision) { throw 'Fixed LF source licence revision is missing.' }
    [byte[]]$bytes = Read-LfFixedGitBlob $source.revision $source.path
    Assert-LfFixedPayload $source $bytes
    [byte[]]$license = Read-LfFixedGitBlob $source.revision $revision.Value.license.path
    Assert-LfFixedPayload $revision.Value.license $license
    $destination = [IO.Path]::GetFullPath((Join-Path $directory $source.path))
    if (-not $destination.StartsWith($directory.TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixed LF source path escaped its fixture.' }
    Assert-LfFixedTemporaryDirectory ([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Write-LfFixedPayload $destination $bytes
    $licensePath = Join-Path $directory ('licenses/' + $source.revision + '.LICENCE')
    Assert-LfFixedTemporaryDirectory ([IO.Path]::GetDirectoryName($licensePath)) | Out-Null
    Write-LfFixedPayload $licensePath $license
    $record = [ordered]@{ name=$Name; revision=$source.revision; source=$source.path; blob=$source.blob; sha256=$source.sha256; scope=$source.scope; licenseSha256=$revision.Value.license.sha256 }
    $provenance = [Text.Encoding]::UTF8.GetBytes(($record | ConvertTo-Json -Depth 4))
    Write-LfFixedPayload (Join-Path $directory ($Name + '.provenance.json')) $provenance
    Write-Verbose ('Fixed LF source: ' + $source.revision + ':' + $source.path + ' SHA256=' + $source.sha256 + ' scope=' + $source.scope)
    return $destination
}
function Read-LfFixedSourceText([Parameter(Mandatory=$true)][string]$Name) {
    $directory = Join-Path ([IO.Path]::GetTempPath()) ('PclLfFixedSource-' + [Guid]::NewGuid().ToString('N'))
    try {
        $path = Export-LfFixedSource -Name $Name -Directory $directory
        return [IO.File]::ReadAllText($path, (New-Object Text.UTF8Encoding($false, $true)))
    } finally {
        if ([IO.Directory]::Exists($directory)) {
            Assert-LfFixedTemporaryDirectory $directory | Out-Null
            foreach ($entry in [IO.Directory]::EnumerateFileSystemEntries($directory, '*', [IO.SearchOption]::AllDirectories)) {
                if (([IO.File]::GetAttributes($entry) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Unexpected link in fixed LF source cleanup.' }
            }
            [IO.Directory]::Delete($directory, $true)
        }
    }
}
