using System;
using System.Runtime.CompilerServices;
namespace PCL.My;
// Exact compiler-generated LF MyProject provider contract witnessed in the original
// full assembly: nested ThreadSafeObjectProvider<T>, ThreadStatic shared field,
// get_GetInstance. Modern Computer/User/Log/AssemblyInfo are real WindowsDesktop BCL types.
internal static class MyProject {
    internal sealed class ThreadSafeObjectProvider<T> where T : new() {
        [CompilerGenerated, ThreadStatic] private static T m_ThreadStaticValue;
        internal T GetInstance {
            get { if (m_ThreadStaticValue is null) m_ThreadStaticValue = new T(); return m_ThreadStaticValue; }
        }
    }
}
