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

    private static void VerifyReturnCodeRoutes()
    {
        // Real LF f4/main50 enum: Success=0, Fail=1, Exception=2. Isolate only
        // process/UI primitives; invoke the actual production shutdown engine.
        foreach (var scenario in new[] { (Code: 0, Name: "Success", Shown: false), (Code: 1, Name: "Fail", Shown: false), (Code: 2, Name: "Exception", Shown: false), (Code: 2, Name: "Exception", Shown: true) })
        {
            List<string> order = new();
            var host = Host();
            host.MarkEnding = () => order.Add("ending"); host.DisableAnimations = () => order.Add("animation");
            host.LogShown = () => { order.Add("shown?"); return scenario.Shown; };
            host.Feedback = () => order.Add("feedback"); host.IssuesUrl = () => { order.Add("issues"); return "https://example.invalid/issues"; };
            host.WriteLogShown = value => order.Add("shown:" + value); host.OpenLog = () => order.Add("open-log");
            host.DescribeCode = value => { order.Add("log:" + value); return scenario.Name; };
            host.SaveAll = () => order.Add("save"); host.FlushLog = () => order.Add("flush");
            host.Exit = value => order.Add("exit:" + value); host.Kill = () => order.Add("kill");
            LegacyMainTermination.EndProgramForce(scenario.Code, host);
            List<string> expected = new() { "ending", "animation" };
            if (scenario.Code == 2)
            {
                expected.Add("shown?");
                if (!scenario.Shown) expected.AddRange(new[] { "feedback", "issues", "shown:True", "open-log" });
            }
            expected.AddRange(new[] { "log:" + scenario.Code, "save", "flush" });
            if (scenario.Code != 0) expected.Add("exit:" + scenario.Code);
            expected.Add("kill");
            Assert(order.SequenceEqual(expected), "LF return-code shutdown route mismatch for " + scenario.Name + "/shown=" + scenario.Shown + ": " + string.Join(",", order));
            Assert(Current is null, "healthy return-code route must preserve clear Err");
        }
        Console.WriteLine("PASS actual LF termination routes: Success0 self-Kill only, Fail1 Exit without exception feedback, Exception2 feedback once/Exit2 and save-flush order");
    }

    internal static void Run()
    {
        VerifyReturnCodeRoutes();
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
        LegacyMainTermination.EndProgramForce(2, warning);
        Assert(seen.Count == 6 && seen.All(item => item.Passed), "warning/exit continuation Err mismatch: " + string.Join(", ", seen.Where(item => !item.Passed).Select(item => item.Name)));
        Assert(ReferenceEquals(Current, exit), "method exit must retain failed Exit Err when Kill succeeds");

        LegacyMainTermination.EndProgramForce(0, Host());
        Assert(Current is null && Information.Err().Number == 0, "next On Error entry clears the previous call's retained Err");
        Console.WriteLine("PASS termination Err: entry clear, condition/statement faults, callback-visible last error, Exit-to-Kill continuation and no exit clear");
    }
}
