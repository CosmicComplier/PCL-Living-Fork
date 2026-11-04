using System;
using System.Collections.Generic;

namespace PCL
{
    // 原内部类型身份保留；实际BCL单例锁/持有/反向释放全部由C#实现。
    internal sealed class LauncherSessionLock : IDisposable
    {
        private readonly PCLCS.Startup.LauncherSessionLease Native;
        private LauncherSessionLock(PCLCS.Startup.LauncherSessionLease Lease)
        {
            Native = Lease;
        }
        internal static LauncherSessionLock TryAcquire(IEnumerable<string> Names, int Timeout)
        {
            var Lease = PCLCS.Startup.LauncherSessionLease.TryAcquire(Names, Timeout);
            return Lease is null ? null : new LauncherSessionLock(Lease);
        }
        public void Dispose()
        {
            Native.Dispose();
        }
    }
}