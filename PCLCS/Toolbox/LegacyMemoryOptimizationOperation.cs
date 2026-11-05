// Behavior source: LF Modules/Base tooling helpers; repository LICENCE.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; upstream PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// Existing LF policies are preserved; no new native/performance/MMC behavior.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace PCLCS
{
    // 父进程和 --memory 辅助进程共用结果约定；测试只需注入委托，不启动提权进程。
    public static class LegacyMemoryOptimizationOperation
    {
        public const int FailedExitCode = -1;
        public static void ExecuteCommands(Func<IDisposable> Acquire, Func<int, int> Apply)
        {
            if (Acquire is null)
                throw new ArgumentNullException(nameof(Acquire), "权限获取函数不能为空。");
            if (Apply is null)
                throw new ArgumentNullException(nameof(Apply), "内存优化命令函数不能为空。");
            using (IDisposable Lease = Acquire())
            {
                if (Lease is null)
                    throw new InvalidOperationException("未能获取内存优化权限。");
                foreach (int Command in new[]
                {
                    2,
                    3,
                    4
                }

                )
                {
                    int Status = Apply(Command);
                    if (Status != 0)
                        throw new InvalidOperationException($"内存优化操作 {Command} 失败（NTSTATUS：0x{Status:X8}）。");
                }
            }
        }

        public static long Execute(bool IsElevated, Func<long> ReadAvailable, Action Optimize, Func<int> RunElevated)
        {
            if (IsElevated)
                return MeasureReleased(ReadAvailable, Optimize);
            if (RunElevated is null)
                throw new ArgumentNullException(nameof(RunElevated));
            int ExitCode;
            try
            {
                ExitCode = RunElevated();
            }
            catch (Win32Exception ex)when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED：用户取消 UAC。
            {
                throw new OperationCanceledException("已取消授予内存优化所需的管理员权限。", ex);
            }

            if (ExitCode < 0)
            {
                throw new InvalidOperationException($"内存优化辅助进程执行失败（退出代码：{ExitCode}）。");
            }

            return ExitCode * 1024L;
        }

        public static int ExecuteHelper(Func<long> ReadAvailable, Action Optimize)
        {
            // 进程退出码保留非负 Int32 表示 KiB，避免大内存机器上的溢出被误判为失败。
            return (int)Math.Min(int.MaxValue, MeasureReleased(ReadAvailable, Optimize) / 1024L);
        }

        private static long MeasureReleased(Func<long> ReadAvailable, Action Optimize)
        {
            if (ReadAvailable is null)
                throw new ArgumentNullException(nameof(ReadAvailable));
            if (Optimize is null)
                throw new ArgumentNullException(nameof(Optimize));
            long Before = ReadAvailable();
            Optimize();
            return Math.Max(0L, checked(ReadAvailable() - Before));
        }
    }
}
