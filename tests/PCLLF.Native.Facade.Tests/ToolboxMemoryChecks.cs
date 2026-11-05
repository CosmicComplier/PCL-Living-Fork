#nullable disable // Mechanically retained original C# fixture; intentional null-input ABI cases.
// LF Test-ToolboxMemory.ps1 through the actual Native .NET10 Main, with original assertion line IDs.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security.Principal;

namespace PCLLF.Native.Facade.Tests;

internal static class ToolboxMemoryChecks {
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    static void Check(int line, bool condition, string message) => ToolboxChecks.Check("Test-ToolboxMemory.ps1", line, condition, message);
    static object Call(Type type, string method, params object[] args) {
        MethodInfo target = type.GetMethod(method, Flags);
        Check(61, target != null, "Missing memory test seam: " + method);
        try { return target.Invoke(null, args); }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    static Exception Failure(Action operation) {
        try { operation(); }
        catch (Exception error) { return error; }
        throw new Exception("Expected a synthetic failure");
    }
    static long Execute(Type type, bool elevated, Func<long> available, Action optimize, Func<int> runElevated) {
        return (long)Call(type, "Execute", elevated, available, optimize, runElevated);
    }
    static int Helper(Type type, Func<long> available, Action optimize) {
        return (int)Call(type, "ExecuteHelper", available, optimize);
    }
    static Func<long> Samples(params long[] values) {
        Queue<long> samples = new Queue<long>(values);
        return () => samples.Dequeue();
    }
    public static void Run(Assembly assembly) {
        Type native = assembly.GetType("PCL.NativeMemoryOptimizer", true);
        // This is the same identity opening used by AcquireProfilePrivilege, but never enable it.
        // WindowsPrincipal duplicates a primary token; the pre-fix Query|AdjustPrivileges mask fails here.
        using (WindowsIdentity expectedIdentity = WindowsIdentity.GetCurrent())
        using (WindowsIdentity identity = (WindowsIdentity)Call(native, "OpenPrivilegeIdentity")) {
            bool expected = new WindowsPrincipal(expectedIdentity).IsInRole(WindowsBuiltInRole.Administrator);
            Check(87, new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator) == expected,
                "The native memory token must allow the real administrator role check");
            Check(89, identity.User.Equals(expectedIdentity.User), "The memory token identity changed");
        }

        Type operation = assembly.GetType("PCL.MemoryOptimizationOperation", true);
        int optimizations = 0, reads = 0, elevations = 0;
        Func<long> available = () => { reads++; return reads == 1 ? 4096L : 7168L; };
        Action optimize = () => { optimizations++; };
        Func<int> forbiddenElevation = () => { elevations++; throw new Exception("Unexpected elevation"); };
        Check(97, Execute(operation, true, available, optimize, forbiddenElevation) == 3072L &&
            reads == 2 && optimizations == 1 && elevations == 0, "Elevated execution did not measure one optimization");

        Check(100, Execute(operation, true, Samples(8192, 4096), () => {}, null) == 0,
            "A concurrent memory decrease must not produce a negative release result");
        Func<long> forbiddenRead = () => { throw new Exception("Unexpected parent memory measurement"); };
        Action forbiddenOptimize = () => { throw new Exception("Unexpected parent optimization"); };
        Check(104, Execute(operation, false, forbiddenRead, forbiddenOptimize, () => 7) == 7168L,
            "The parent did not convert the helper result from KiB");
        Check(106, Execute(operation, false, forbiddenRead, forbiddenOptimize, () => 0) == 0,
            "A successful zero-release helper result was rejected");
        Check(108, Execute(operation, false, forbiddenRead, forbiddenOptimize, () => Int32.MaxValue) == (long)Int32.MaxValue * 1024L,
            "A large helper result overflowed");
        foreach (int exitCode in new[] { -1, unchecked((int)0xC0000005) }) {
            Exception error = Failure(() => Execute(operation, false, forbiddenRead, forbiddenOptimize, () => exitCode));
            Check(112, error is InvalidOperationException && error.Message.Contains(exitCode.ToString()),
                "A failed or crashed helper was silently treated as completion");
        }

        Win32Exception cancelled = new Win32Exception(1223, "synthetic UAC cancellation");
        Exception cancellation = Failure(() => Execute(operation, false, forbiddenRead, forbiddenOptimize, () => { throw cancelled; }));
        Check(118, cancellation is OperationCanceledException && Object.ReferenceEquals(cancellation.InnerException, cancelled),
            "UAC cancellation must have a distinct cancellation result");
        Win32Exception denied = new Win32Exception(5, "synthetic elevation denial");
        Check(121, Object.ReferenceEquals(Failure(() => Execute(operation, false, forbiddenRead, forbiddenOptimize, () => { throw denied; })), denied),
            "A genuine elevation failure was swallowed or treated as cancellation");

        Exception commandError = new InvalidOperationException("synthetic optimization denied");
        reads = 0;
        available = () => { reads++; return 4096; };
        Check(127, Object.ReferenceEquals(Failure(() => Execute(operation, true, available, () => { throw commandError; }, forbiddenElevation)), commandError) &&
            reads == 1 && elevations == 0, "A local optimization failure was swallowed, retried, or measured as success");
        Check(129, Object.ReferenceEquals(Failure(() => Helper(operation, Samples(1), () => { throw commandError; })), commandError),
            "The helper swallowed an optimization failure");
        Check(131, Helper(operation, Samples(0, 5 * 1024 + 1023), () => {}) == 5,
            "The helper result must use whole KiB");
        Check(133, Helper(operation, Samples(8192, 4096), () => {}) == 0,
            "The helper must clamp a memory decrease");
        Check(135, Helper(operation, Samples(0, Int64.MaxValue), () => {}) == Int32.MaxValue,
            "The helper must clamp large successful results before returning an exit code");

        Check(138, Failure(() => Execute(operation, false, forbiddenRead, forbiddenOptimize, null)) is ArgumentNullException,
            "A missing elevation delegate was accepted");
        Check(140, Failure(() => Execute(operation, true, null, forbiddenOptimize, null)) is ArgumentNullException,
            "A missing memory reader was accepted");
        Check(142, Failure(() => Execute(operation, true, forbiddenRead, null, null)) is ArgumentNullException,
            "A missing optimization delegate was accepted");
    }
}