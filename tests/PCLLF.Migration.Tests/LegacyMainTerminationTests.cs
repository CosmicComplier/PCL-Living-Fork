using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PCLCS.Startup;

internal static class LegacyMainTerminationTests
{
    private static Exception? Current => Information.Err().GetException();
    private static void Assert(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static LegacyMainTerminationHost Host() => new()
    {
        MarkEnding = () => { }, DisableAnimations = () => { }, Feedback = () => { }, OpenLog = () => { },
        SaveAll = () => { }, FlushLog = () => { }, Kill = () => { }, LogShown = () => false,
        WriteLogShown = _ => { }, IssuesUrl = () => "https://example.invalid/issues",
        Exit = _ => { }, DescribeCode = _ => "Success"
    };

    internal static void Run()
    {
        // Entry is the original On Error statement. A healthy call clears an earlier Err.
        ProjectData.SetProjectError(new IOException("prior"));
        bool entryClear = false;
        var healthy = Host();
        healthy.MarkEnding = () => entryClear = Current is null && Information.Err().Number == 0;
        LegacyMainTermination.EndProgramForce(0, healthy);
        Assert(entryClear && Current is null && Information.Err().Number == 0, "healthy termination must clear prior Err at entry");

        // A helper is one original statement, not a new On Error entry. Success preserves
        // an earlier failure; a failing If condition records Err and enters the first body.
        var conditionError = new InvalidOperationException("condition");
        ProjectData.SetProjectError(conditionError);
        Assert(!LegacyMainTermination.ResumeNextCondition(() => false) && ReferenceEquals(Current, conditionError), "successful false condition must not clear Err");
        var failedCondition = new IOException("failed-condition");
        Assert(LegacyMainTermination.ResumeNextCondition(() => throw failedCondition) && ReferenceEquals(Current, failedCondition), "failed condition must record actual exception and enter first body");
        Assert(LegacyMainTermination.ResumeNextCondition(() => true) && ReferenceEquals(Current, failedCondition), "successful later condition must retain last Err");

        // The assertions inside isolated host primitives record their observations rather
        // than throwing test failures into the deliberately swallowing production engine.
        List<(string Name, bool Passed)> seen = new();
        var mark = new IOException("ending");
        var suppress = new InvalidOperationException("animation");
        var save = new UnauthorizedAccessException("save");
        var flush = new IOException("flush");
        var kill = new InvalidOperationException("kill");
        var faulted = Host();
        faulted.MarkEnding = () => { seen.Add(("entry clear", Current is null)); throw mark; };
        faulted.DisableAnimations = () => { seen.Add(("animation sees ending failure", ReferenceEquals(Current, mark))); throw suppress; };
        faulted.DescribeCode = _ => { seen.Add(("log sees animation failure", ReferenceEquals(Current, suppress))); return "Success"; };
        faulted.SaveAll = () => { seen.Add(("save sees same failure after successful log", ReferenceEquals(Current, suppress))); throw save; };
        faulted.FlushLog = () => { seen.Add(("flush sees save failure", ReferenceEquals(Current, save))); throw flush; };
        faulted.Kill = () => { seen.Add(("kill sees flush failure", ReferenceEquals(Current, flush))); throw kill; };
        LegacyMainTermination.EndProgramForce(0, faulted);
        Assert(seen.Count == 6 && seen.All(item => item.Passed), "statement continuation Err mismatch: " + string.Join(", ", seen.Where(item => !item.Passed).Select(item => item.Name)));
        Assert(ReferenceEquals(Current, kill), "normal method exit must retain final swallowed exception");

        seen.Clear();
        var condition = new IOException("log-condition");
        var open = new IOException("open-log");
        var exit = new InvalidOperationException("exit");
        var warning = Host();
        warning.LogShown = () => throw condition;
        warning.Feedback = () => seen.Add(("condition failure enters feedback", ReferenceEquals(Current, condition)));
        warning.WriteLogShown = value => seen.Add(("mark shown preserves condition Err", value && ReferenceEquals(Current, condition)));
        warning.OpenLog = () => throw open;
        warning.DescribeCode = _ => { seen.Add(("open log failure reaches final log", ReferenceEquals(Current, open))); return "Exception"; };
        warning.SaveAll = () => seen.Add(("save after warning", ReferenceEquals(Current, open)));
        warning.FlushLog = () => seen.Add(("flush after warning", ReferenceEquals(Current, open)));
        warning.Exit = _ => throw exit;
        warning.Kill = () => seen.Add(("failed Exit still reaches Kill", ReferenceEquals(Current, exit)));
        LegacyMainTermination.EndProgramForce(1, warning);
        Assert(seen.Count == 6 && seen.All(item => item.Passed), "warning/exit continuation Err mismatch: " + string.Join(", ", seen.Where(item => !item.Passed).Select(item => item.Name)));
        Assert(ReferenceEquals(Current, exit), "method exit must retain failed Exit Err when Kill succeeds");

        LegacyMainTermination.EndProgramForce(0, Host());
        Assert(Current is null && Information.Err().Number == 0, "next On Error entry clears the previous call's retained Err");
        Console.WriteLine("PASS termination Err: entry clear, condition/statement faults, callback-visible last error, Exit-to-Kill continuation and no exit clear");
    }
}
