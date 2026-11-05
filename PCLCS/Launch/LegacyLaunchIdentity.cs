using System;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS;

public static class LegacyLaunchIdentity {
    public static bool LoginEquals(object self, object? value) => value != null && value.GetHashCode() == self.GetHashCode();
    public static int ServerHash(string? userName, string? password, string? baseUrl, string? token, int kind) =>
        (int)((userName + password + baseUrl + token + Conversions.ToString(kind)).GetStableHashCode() % int.MaxValue);
    public static int MicrosoftHash(string? refreshToken, string? userName) => (int)((refreshToken + userName).GetStableHashCode() % int.MaxValue);
    public static int OfflineHash(string? userName, int skinType, string? skinName, int kind) =>
        (int)((userName + Conversions.ToString(skinType) + skinName + Conversions.ToString(kind)).GetStableHashCode() % int.MaxValue);
}
