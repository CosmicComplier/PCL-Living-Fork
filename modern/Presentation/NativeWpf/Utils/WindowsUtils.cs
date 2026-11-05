// Fixed LF Shared c774ae4c85fd94e0eaa171db21e78a65870df19b Wpf/Utils/WindowsUtils.cs; Apache-2.0 (MeloongCore/LICENSE.txt).
// Modern Presentation source group only; no legacy MeloongCore.Wpf assembly reference.
using System.Security.Principal;

namespace MeloongCore;
public static class WindowsUtils {

    /// <summary>
    /// 当前程序是否拥有管理员权限。
    /// </summary>
    public static bool HasAdminRole() {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

}
