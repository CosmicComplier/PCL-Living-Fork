using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using Microsoft.VisualBasic;

namespace PCL
{
    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If _MyType <> "Empty" Then
    */
    namespace My
    {
        /// <summary>
    /// 用于定义“我的 WPF 命名空间”中的可用属性的模块
    /// </summary>
    /// <remarks></remarks>
        [HideModuleName()]
        static class MyWpfExtension
        {
            private static MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.Devices.Computer> s_Computer = new MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.Devices.Computer>();
            private static MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User> s_User = new MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User>();
            private static MyProject.ThreadSafeObjectProvider<MyWindows> s_Windows = new MyProject.ThreadSafeObjectProvider<MyWindows>();
            private static MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.Logging.Log> s_Log = new MyProject.ThreadSafeObjectProvider<Microsoft.VisualBasic.Logging.Log>();
            /// <summary>
        /// 返回正在运行的应用程序的应用程序对象
        /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Application Application
            {
                get
                {
                    return (Application)System.Windows.Application.Current;
                }
            }
            /// <summary>
        /// 返回有关主机计算机的信息。
        /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.Devices.Computer Computer
            {
                get
                {
                    return s_Computer.GetInstance;
                }
            }
            /// <summary>
        /// 返回当前用户的信息。  如果希望使用当前的 
        /// Windows 用户凭据来运行应用程序，请调用 My.User.InitializeWithWindowsUser()。
        /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.ApplicationServices.User User
            {
                get
                {
                    return s_User.GetInstance;
                }
            }
            /// <summary>
        /// 返回应用程序日志。可以使用应用程序的配置文件配置侦听器。
        /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.Logging.Log Log
            {
                get
                {
                    return s_Log.GetInstance;
                }
            }

            /// <summary>
        /// 返回项目中定义的 Windows 集合。
        /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static MyWindows Windows
            {
                [DebuggerHidden()]
                get
                {
                    return s_Windows.GetInstance;
                }
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [MyGroupCollection("System.Windows.Window", "Create__Instance__", "Dispose__Instance__", "My.MyWpfExtenstionModule.Windows")]
            internal sealed class MyWindows
            {
                [DebuggerHidden()]
                private static T Create__Instance__<T>(T Instance) where T : Window, new()
                {
                    if (Instance is null)
                    {
                        if (s_WindowBeingCreated is not null)
                        {
                            if (s_WindowBeingCreated.ContainsKey(typeof(T)) == true)
                            {
                                throw new InvalidOperationException("The window cannot be accessed via My.Windows from the Window constructor.");
                            }
                        }
                        else
                        {
                            s_WindowBeingCreated = new Hashtable();
                        }
                        s_WindowBeingCreated.Add(typeof(T), null);
                        return new T();
                        s_WindowBeingCreated.Remove(typeof(T));
                    }
                    else
                    {
                        return Instance;
                    }
                }
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
                [DebuggerHidden()]
                private void Dispose__Instance__<T>(ref T instance) where T : Window
                {
                    instance = null;
                }
                [DebuggerHidden()]
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public MyWindows() : base()
                {
                }
                [ThreadStatic()]
                private static Hashtable s_WindowBeingCreated;
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override bool Equals(object o)
                {
                    return base.Equals(o);
                }
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal new Type GetType()
                {
                    return typeof(MyWindows);
                }
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override string ToString()
                {
                    return base.ToString();
                }
            }
        }
    }
    public partial class Application : System.Windows.Application
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal Microsoft.VisualBasic.ApplicationServices.AssemblyInfo Info
        {
            [DebuggerHidden()]
            get
            {
                return new Microsoft.VisualBasic.ApplicationServices.AssemblyInfo(System.Reflection.Assembly.GetExecutingAssembly());
            }
        }
    }
}
/* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If*/