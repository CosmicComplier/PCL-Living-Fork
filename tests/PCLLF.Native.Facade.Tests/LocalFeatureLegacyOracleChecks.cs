using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PCLLF.Native.Facade.Tests;

// Same independent CLR4-process approach as BitmapFoundationAcceptance.
// This driver measures only the exact old Double/Path BCL APIs used by the
// original script. It does not load modern Main into CLR4 or implement LF rules.
internal static class LocalFeatureLegacyOracleChecks
{
    private const string Driver = """
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object Text.UTF8Encoding($false)
if([Environment]::Version.Major -ne 4 -or $PSVersionTable.PSEdition -ne 'Desktop'){throw 'A real Windows PowerShell CLR4 oracle is mandatory.'}
$fov=[Collections.Generic.List[object]]::new()
$nbsp=[string][char]0xa0
foreach($raw in @('1e400','-1e400','Infinity','-Infinity','+Infinity','infinity','INFINITY','-infinity','NaN','nan','NAN','+NaN','-NaN',"`t Infinity ",($nbsp+'Infinity'+$nbsp),($nbsp+'NaN'+$nbsp),"`t-Infinity`t",'0','-1','1','1.0000001')){
 foreach($fallback in @($false,$true)){
  [double]$numeric=0;$parsed=[double]::TryParse($raw,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$numeric)
  $usesDegrees=$fallback;if($parsed){$usesDegrees=[Math]::Abs($numeric) -gt 1}
  $expected=if($usesDegrees){'90'}else{'0.5'}
  $fov.Add([pscustomobject]@{Raw=$raw;Fallback=$fallback;Expected=$expected})
 }
}
$paths=[Collections.Generic.List[object]]::new()
foreach($character in [IO.Path]::GetInvalidFileNameChars()){
 $expected=[IO.IOException].FullName
 try{[IO.Path]::GetFileName(('A'+[string]$character+'B'))|Out-Null}catch{$errorValue=$_.Exception;while($null -ne $errorValue.InnerException){$errorValue=$errorValue.InnerException};$expected=$errorValue.GetType().FullName}
 $paths.Add([pscustomobject]@{Code=[int]$character;Error=$expected})
}
[pscustomobject]@{RuntimeMajor=[Environment]::Version.Major;PSEdition=$PSVersionTable.PSEdition;Fov=$fov;Paths=$paths}|ConvertTo-Json -Depth 5 -Compress
""";
    internal static JObject Measure()
    {
        string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        Native.Check(File.Exists(shell), "The mandatory independent CLR4 path/FOV oracle is unavailable.");
        ProcessStartInfo start = new(shell) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = new UTF8Encoding(false, true), StandardErrorEncoding = new UTF8Encoding(false, true) };
        foreach (string value in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(Driver)) }) start.ArgumentList.Add(value);
        using Process process = Process.Start(start) ?? throw new IOException("The independent CLR4 oracle did not start.");
        Stopwatch deadline = Stopwatch.StartNew();
        int Remaining() => Math.Max(0, 15000 - (int)deadline.ElapsedMilliseconds);
        Task<string> output = process.StandardOutput.ReadToEndAsync(), errors = process.StandardError.ReadToEndAsync();
        Task[] drains = [output, errors];
        foreach (Task drain in drains) _ = drain.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try {
            if (!process.WaitForExit(Remaining()) || !Task.WaitAll(drains, Remaining()))
                throw new TimeoutException("CLR4 FOV/path API oracle process/output exceeded the same 15-second deadline.");
            string text = output.GetAwaiter().GetResult(), error = errors.GetAwaiter().GetResult();
            Native.Check(process.ExitCode == 0 && string.IsNullOrWhiteSpace(error), "Independent CLR4 oracle failed: " + error + text);
            JObject result = JObject.Parse(text);
            Native.Check((int?)result["RuntimeMajor"] == 4 && (string?)result["PSEdition"] == "Desktop" && result["Fov"] is JArray { Count: 42 } && result["Paths"] is JArray { Count: > 0 }, "The CLR4 oracle did not measure the complete original sample set.");
            Native.Check(((JArray)result["Paths"]!).Select(value => (int)value["Code"]!).Distinct().Count() == ((JArray)result["Paths"]!).Count, "Duplicate path oracle samples.");
            return result;
        }
        finally {
            try {
                if (!process.HasExited) { process.Kill(true); Native.Check(process.WaitForExit(5000), "Owned CLR4 oracle did not stop after termination."); }
            }
            finally {
                // Close both owned read streams even if termination failed, so a
                // descendant retaining a pipe cannot leave an unobserved drain.
                try { process.StandardOutput.Dispose(); }
                finally {
                    try { process.StandardError.Dispose(); }
                    finally {
                        try { Native.Check(Task.WaitAll(drains, 5000), "Owned CLR4 output drains did not settle after closure."); }
                        finally { foreach (Task drain in drains) if (drain.IsFaulted) _ = drain.Exception; }
                    }
                }
            }
        }
    }
}
