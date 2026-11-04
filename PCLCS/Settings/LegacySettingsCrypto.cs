using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MeloongCore.Extensions;

namespace PCLCS;

/// <summary>LF's existing DES wire format. Used only for compatibility with old data.</summary>
public static class LegacySettingsCrypto {
    private static readonly byte[] initialVector = Encoding.UTF8.GetBytes("95168702");

    private static byte[] KeyBytes(string? key) {
        key = key is null ? "@;$ Abv2" : key.GetStableHashCode().ToString().EnsureLength('X', 8).Substring(0, 8);
        return Encoding.UTF8.GetBytes(key);
    }

    private static DES CreateProvider() {
#if NETFRAMEWORK
        return new DESCryptoServiceProvider();
#else
        // DESCryptoServiceProvider's original CBC/PKCS7 64-bit defaults, without
        // depending on the obsolete CSP implementation on modern .NET.
        DES provider = DES.Create();
        provider.Mode = CipherMode.CBC;
        provider.Padding = PaddingMode.PKCS7;
        provider.BlockSize = 64;
        provider.KeySize = 64;
        return provider;
#endif
    }

    public static string Encrypt(string sourceString, string? key = null) {
        byte[] keyBytes = KeyBytes(key);
        using DES des = CreateProvider();
        using var output = new MemoryStream();
        using var crypto = new CryptoStream(output, des.CreateEncryptor(keyBytes, initialVector), CryptoStreamMode.Write);
        byte[] input = Encoding.UTF8.GetBytes(sourceString);
        crypto.Write(input, 0, input.Length);
        crypto.FlushFinalBlock();
        return Convert.ToBase64String(output.ToArray());
    }

    public static string Decrypt(string encryptedString, string? key = null) {
        byte[] keyBytes = KeyBytes(key);
        using DES des = CreateProvider();
        using var output = new MemoryStream();
        using var crypto = new CryptoStream(output, des.CreateDecryptor(keyBytes, initialVector), CryptoStreamMode.Write);
        byte[] input = Convert.FromBase64String(encryptedString);
        crypto.Write(input, 0, input.Length);
        crypto.FlushFinalBlock();
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public static string? SourceIdentify(string? hardwareConfig, string? seed) {
        if (string.IsNullOrEmpty(seed) || seed!.Length < 3) return null;
        string hardware = (hardwareConfig ?? "Unknown").ToUpperInvariant().Trim('{', '}');
        string hash = (hardware + seed).GetStableHashCode().ToString("X").PadLeft(16, '7');
        return hash.Substring(4, 4) + "-" + hash.Substring(12, 4) + "-" + hash.Substring(0, 4) + "-" + hash.Substring(8, 4);
    }
}
