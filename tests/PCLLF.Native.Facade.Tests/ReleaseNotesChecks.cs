using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PCLLF.Native.Facade.Tests;

internal static class ReleaseNotesChecks
{
    internal static void Run()
    {
        const string markdown = "# 更新说明\n\n修复 **粗体**、*斜体*、~~删除~~ 和 `code`。\n\n- 第一项\n- 第二项\n\n3. 第三项\n4. 第四项\n\n> 引用内容\n\n```csharp\nvar value = 1;\n```\n\n[发布页面](https://github.com/DDZmumo/PCL-Living-Fork/releases) 与 [禁止脚本](javascript:alert(1))\n\n| 类型 | 状态 |\n| --- | --- |\n| 正式版 | 可用 |\n\n- [x] 完成\n- [ ] 待办\n";
        var opened = new List<string>();
        var view = (FlowDocumentScrollViewer)Activator.CreateInstance(Native.Type("PCL.ReleaseNotesView"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object?[] { markdown, (Action<string>)opened.Add }, null)!;
        string text = new TextRange(view.Document.ContentStart, view.Document.ContentEnd).Text;
        Native.Check(text.Contains("更新说明") && text.Contains("第一项") && !text.Contains("**粗体**") && !text.Contains("```"), "Release notes still display raw Markdown.");
        Native.Check(view.Document.Blocks.OfType<Paragraph>().Any(block => block.FontSize >= 20) && view.Document.Blocks.OfType<List>().Count() >= 2 &&
            view.Document.Blocks.OfType<Table>().Single().RowGroups[0].Rows.Count == 2 &&
            view.Document.Blocks.OfType<Paragraph>().Any(block => block.FontFamily.Source == "Consolas"), "Release notes lost headings, lists, tables or code formatting.");
        IEnumerable<Inline> Inlines(IEnumerable<Block> blocks)
        {
            foreach (Block block in blocks)
            {
                if (block is Paragraph paragraph)
                    foreach (Inline inline in Walk(paragraph.Inlines)) yield return inline;
                else if (block is Section section)
                    foreach (Inline inline in Inlines(section.Blocks)) yield return inline;
                else if (block is List list)
                    foreach (ListItem item in list.ListItems)
                        foreach (Inline inline in Inlines(item.Blocks)) yield return inline;
                else if (block is Table table)
                    foreach (TableRowGroup group in table.RowGroups)
                        foreach (TableRow row in group.Rows)
                            foreach (TableCell cell in row.Cells)
                                foreach (Inline inline in Inlines(cell.Blocks)) yield return inline;
            }
        }
        IEnumerable<Inline> Walk(InlineCollection collection)
        {
            foreach (Inline inline in collection)
            {
                yield return inline;
                if (inline is Span span) foreach (Inline child in Walk(span.Inlines)) yield return child;
            }
        }
        Inline[] inlines = Inlines(view.Document.Blocks).ToArray();
        Native.Check(inlines.OfType<Span>().Any(span => span.FontWeight == FontWeights.Bold) && inlines.OfType<Span>().Any(span => span.FontStyle == FontStyles.Italic), "Emphasis did not survive Markdown rendering.");
        Hyperlink link = inlines.OfType<Hyperlink>().Single();
        link.RaiseEvent(new RequestNavigateEventArgs(link.NavigateUri, ""));
        Native.Check(opened.SequenceEqual(new[] { "https://github.com/DDZmumo/PCL-Living-Fork/releases" }), "Release-note links did not route through the supplied browser boundary.");
        string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--preview-path");
        if (index >= 0 && index + 1 < args.Length)
        {
            view.Measure(new Size(720, 460)); view.Arrange(new Rect(0, 0, 720, 460)); view.UpdateLayout();
            var bitmap = new RenderTargetBitmap(720, 460, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using FileStream stream = File.Create(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[index + 1]))!, "markdown-preview.png")); png.Save(stream);
        }
        Console.WriteLine("PASS release notes: native WPF Markdown headings/emphasis/lists/code/table/task items and HTTP link routing; no HTTP, browser launch or HTML execution.");
    }
}
