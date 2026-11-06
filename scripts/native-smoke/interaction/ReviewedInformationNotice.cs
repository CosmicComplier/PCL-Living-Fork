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
            "PCL Living Fork 1.4.0-pre-4 版本说明" => "1.4.0-pre-4",
            "PCL Living Fork 1.4.0 版本说明" => "1.4.0",
            "PCL Living Fork dev_edition 版本说明" => "dev_edition",
            _ => throw new InvalidOperationException("PENDING: only exact reviewed information notices may be dismissed.")
        };
        string highlights = version == "1.3.5-pre-3"
            ? "- 首页可开关模组依赖检查与自动补全\r\n- 整合包更新按分类预览，下载显示实际进度\r\n- 还原点可选设置自动清理上限，手动记录单独保留"
            : (version == "1.4.0-pre-4" ? "- 主产品迁移至 C#/.NET 10，精简版需 .NET 10 Desktop Runtime\r\n- 更新页新增版本搜索、当前版本标记与 Markdown 说明\r\n- 修复图片、中文搜索、资源详情异常及删除版本卡顿" : "- 汇总四个预发布版本，包含依赖、主题、登录隔离与本次实机修复\r\n- C#/.NET 10 与 Windows x64；运行前需 .NET 10 Desktop Runtime\r\n- 完善依赖索引、内嵌前置识别与补全、深浅色主题和面板不透明度\r\n- 原生更新页支持版本搜索、通道选择与 Markdown 更新说明\r\n- 备份、克隆、恢复显示实际进度；存档与资源支持正倒序\r\n- 微软登录缓存使用 _LF 隔离；修复图标、中文搜索与删除卡顿\r\n- 整包更新保护个人修改和世界数据，修复配置误关联与冲突预览");
        return "PCL Living Fork " + version + " 是基于 PCL 官方开源版的第三方版本。\r\n\r\n" + highlights;
    }

    internal static void VerifyPolicy()
    {
        foreach (string version in new[] { "1.3.5-pre-3", "1.4.0-pre-4", "1.4.0", "dev_edition" })
        {
            string caption = CaptionForTitle("PCL Living Fork " + version + " 版本说明");
            string expected = "PCL Living Fork " + version + " 是基于 PCL 官方开源版的第三方版本。\r\n\r\n" +
                (version == "1.3.5-pre-3"
                    ? "- 首页可开关模组依赖检查与自动补全\r\n- 整合包更新按分类预览，下载显示实际进度\r\n- 还原点可选设置自动清理上限，手动记录单独保留"
                    : (version == "1.4.0-pre-4" ? "- 主产品迁移至 C#/.NET 10，精简版需 .NET 10 Desktop Runtime\r\n- 更新页新增版本搜索、当前版本标记与 Markdown 说明\r\n- 修复图片、中文搜索、资源详情异常及删除版本卡顿" : "- 汇总四个预发布版本，包含依赖、主题、登录隔离与本次实机修复\r\n- C#/.NET 10 与 Windows x64；运行前需 .NET 10 Desktop Runtime\r\n- 完善依赖索引、内嵌前置识别与补全、深浅色主题和面板不透明度\r\n- 原生更新页支持版本搜索、通道选择与 Markdown 更新说明\r\n- 备份、克隆、恢复显示实际进度；存档与资源支持正倒序\r\n- 微软登录缓存使用 _LF 隔离；修复图标、中文搜索与删除卡顿\r\n- 整包更新保护个人修改和世界数据，修复配置误关联与冲突预览"));
            if (!string.Equals(caption, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Reviewed information notice body changed.");
        }
        foreach (string title in new[] { "PCL Living Fork 更新失败", "PCL Living Fork 1.4.0-pre-5 版本说明", "PCL Living Fork dev_edition 版本说明 suffix", "PCL Living Fork dev_edition\n版本说明", "" })
        {
            bool refused = false;
            try { _ = CaptionForTitle(title); }
            catch (InvalidOperationException) { refused = true; }
            if (!refused) throw new InvalidOperationException("Unknown information notice was accepted.");
        }
        Console.WriteLine("PASS exact reviewed notice policy: release/development bodies and five unknown/unsafe titles; no UI, product, profile or input action.");
    }
}
