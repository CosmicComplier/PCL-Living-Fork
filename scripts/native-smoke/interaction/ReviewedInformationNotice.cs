using System;

namespace PclLfNativeInteraction;

// Exact reviewed informational dialogs only; no regex or unknown-dialog fallback.
internal static class ReviewedInformationNotice
{
    internal static string CaptionForTitle(string title)
    {
        string version = title switch
        {
            "PCL Living Fork 1.3.5-pre-3 版本说明" => "1.3.5-pre-3",
            "PCL Living Fork dev_edition 版本说明" => "dev_edition",
            _ => throw new InvalidOperationException("PENDING: only exact reviewed information notices may be dismissed.")
        };
        return "PCL Living Fork " + version + " 是基于 PCL 官方开源版的第三方版本。\r\n\r\n- 首页可开关模组依赖检查与自动补全\r\n- 整合包更新按分类预览，下载显示实际进度\r\n- 还原点可选设置自动清理上限，手动记录单独保留";
    }

    internal static void VerifyPolicy()
    {
        foreach (string version in new[] { "1.3.5-pre-3", "dev_edition" })
        {
            string caption = CaptionForTitle("PCL Living Fork " + version + " 版本说明");
            if (!caption.StartsWith("PCL Living Fork " + version + " 是基于 ", StringComparison.Ordinal) ||
                !caption.Contains("\r\n\r\n- 首页可开关模组依赖检查与自动补全", StringComparison.Ordinal))
                throw new InvalidOperationException("Reviewed information notice body changed.");
        }
        foreach (string title in new[] { "PCL Living Fork 更新失败", "PCL Living Fork 1.3.6 版本说明", "PCL Living Fork dev_edition 版本说明 suffix", "PCL Living Fork dev_edition\n版本说明", "" })
        {
            bool refused = false;
            try { _ = CaptionForTitle(title); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused) throw new InvalidOperationException("Unknown information notice was accepted.");
        }
        Console.WriteLine("PASS exact reviewed notice policy: release/development bodies and five unknown/unsafe titles; no UI, product, profile or input action.");
    }
}
