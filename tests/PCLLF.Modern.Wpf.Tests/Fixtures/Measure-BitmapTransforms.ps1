# Compile and execute immutable LF transform excerpts in the independent CLR4 process.
# Only in-memory fixture bitmaps are used; no launcher/user files or network calls.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop'){throw 'The baseline must execute on Windows PowerShell / CLR4.'}
Add-Type -AssemblyName System.Drawing,Microsoft.VisualBasic
$sourcePath=Join-Path $PSScriptRoot 'MyBitmapTransforms.Legacy.vb'
$sha=[Security.Cryptography.SHA256]::Create()
try{$sourceHash=[BitConverter]::ToString($sha.ComputeHash([IO.File]::ReadAllBytes($sourcePath))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
if($sourceHash -cne 'c6c07cb555bfe24ed69053dde74af0ccdfd9822e12da6d6b9109664a5c68a3af'){throw 'The fixed LF bitmap transform baseline changed.'}
$compiler=New-Object Microsoft.VisualBasic.VBCodeProvider
$parameters=New-Object CodeDom.Compiler.CompilerParameters
$parameters.GenerateInMemory=$true
$framework=Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8'
$parameters.CompilerOptions='/optioninfer+ /optionstrict- /sdkpath:"'+$framework+'" /vbruntime:"'+(Join-Path $framework 'Microsoft.VisualBasic.dll')+'"'
$parameters.ReferencedAssemblies.Add('System.dll')|Out-Null
$parameters.ReferencedAssemblies.Add('System.Drawing.dll')|Out-Null
$compiled=$compiler.CompileAssemblyFromSource($parameters,[IO.File]::ReadAllText($sourcePath))
if($compiled.Errors.HasErrors){throw (($compiled.Errors|ForEach-Object {$_.ToString()}) -join [Environment]::NewLine)}
$source=[Drawing.Bitmap]::new(3,2,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop=$null;$rotate=$null
try{
 $source.SetResolution(144,120)
 for($y=0;$y -lt 2;$y++){for($x=0;$x -lt 3;$x++){$source.SetPixel($x,$y,[Drawing.Color]::FromArgb(255,($x*40),($y*50),($x+$y)))}}
 $wrapper=[Activator]::CreateInstance($compiled.CompiledAssembly.GetType('MyBitmap'),[object[]]@($source))
 $crop=$wrapper.Clip(1,0,1,2).Pic
 $rotate=$wrapper.RotateFlip([Drawing.RotateFlipType]::Rotate90FlipNone).Pic
 function Read-Bitmap($bitmap){
  $pixels=New-Object 'System.Collections.Generic.List[int]'
  for($y=0;$y -lt $bitmap.Height;$y++){for($x=0;$x -lt $bitmap.Width;$x++){$pixels.Add($bitmap.GetPixel($x,$y).ToArgb())}}
  return [ordered]@{Width=$bitmap.Width;Height=$bitmap.Height;DpiX=$bitmap.HorizontalResolution;DpiY=$bitmap.VerticalResolution;Pixels=@($pixels.ToArray())}
 }
 [ordered]@{Runtime=[Runtime.InteropServices.RuntimeEnvironment]::GetSystemVersion();SourceHash='c6c07cb555bfe24ed69053dde74af0ccdfd9822e12da6d6b9109664a5c68a3af';Crop=(Read-Bitmap $crop);Rotate=(Read-Bitmap $rotate)}|ConvertTo-Json -Depth 4 -Compress
}finally{
 if($null -ne $crop){$crop.Dispose()};if($null -ne $rotate){$rotate.Dispose()};$source.Dispose();$compiler.Dispose()
}
