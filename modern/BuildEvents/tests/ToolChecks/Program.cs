using System.Collections;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class Program {
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
    static SyntaxToken FileToken(DirectiveTriviaSyntax node) => node switch {
        LineDirectiveTriviaSyntax line => line.File,
        LineSpanDirectiveTriviaSyntax span => span.File,
        PragmaChecksumDirectiveTriviaSyntax checksum => checksum.File,
        _ => default
    };
    static DirectiveTriviaSyntax[] Directives(string text) => CSharpSyntaxTree.ParseText(text).GetRoot()
        .DescendantTrivia(descendIntoTrivia: true).Select(t => t.GetStructure()).OfType<DirectiveTriviaSyntax>().ToArray();
    static string Shape(DirectiveTriviaSyntax node) {
        SyntaxToken file = FileToken(node);
        return file.IsKind(SyntaxKind.StringLiteralToken) ? node.ReplaceToken(file, SyntaxFactory.Literal("SOURCE")).ToString() : node.ToString();
    }
    static (LfFilterWithEvents Task, Log Log, bool Success) Execute(string input, string map, string output, bool generated = true) {
        Log log = new(); TaskItem item = new(input); item.SetMetadata("Link", "preserved-link.g.cs");
        LfFilterWithEvents task = new() { BuildEngine = log, CompileFiles = new ITaskItem[] { item },
            GeneratedFiles = generated ? new ITaskItem[] { item } : Array.Empty<ITaskItem>(), MapFile = map, OutputDirectory = output };
        return (task, log, task.Execute());
    }
    static void Main(string[] args) {
        string root = args[0]; Directory.CreateDirectory(root);
        string source = Path.Combine(root, "Pages", "深层 空格", "Source.xaml"); Directory.CreateDirectory(Path.GetDirectoryName(source)); File.WriteAllText(source, "<Grid/>\n");
        string generated = Path.Combine(root, "obj", "Release", "net10.0-windows", "win-x64", "Pages", "Deep"); Directory.CreateDirectory(generated);
        string map = Path.Combine(root, "WithEvents.map"); File.WriteAllText(map, "Cases.Widget\tFire\tglobal::System.Object\n");
        string relative = Path.GetRelativePath(generated, source);
        string[] operands = { relative, source, @"D:\a\space a\中文\File.xaml", @"\\server\share\File.xaml" };
        foreach (string operand in operands) {
            string input = Path.Combine(generated, "Widget.g.cs"), literal = "\"" + operand + "\"";
            string text = "#pragma checksum " + literal + " \"{ff1816ec-aa5e-4d10-87f7-6f4963833460}\" \"AABB\"\n" +
                "#line 12 " + literal + "\n#line (1, 1) - (2, 3) 4 " + literal + "\n" +
                "namespace Cases { partial class Widget { internal System.Object Fire; string Body = \"unchanged body string\"; } }\n#line default\n#line hidden\n";
            File.WriteAllText(input, text); string before = Hash(input);
            var run = Execute(input, map, Path.Combine(root, "projected")); Require(run.Success, string.Join("\n", run.Log.Errors));
            string projected = run.Task.FilteredCompile.Single().ItemSpec;
            Require(projected == run.Task.FilteredGenerated.Single().ItemSpec, "compiler channels diverged");
            Require(run.Task.FilteredCompile.Single().GetMetadata("Link") == "preserved-link.g.cs", "item metadata lost");
            Require(Hash(input) == before, "original generated input was modified");
            string output = File.ReadAllText(projected);
            Require(!output.Contains("internal System.Object Fire;") && output.Contains("unchanged body string"), "non-field code changed");
            DirectiveTriviaSyntax[] old = Directives(text), current = Directives(output);
            Require(old.Select(Shape).SequenceEqual(current.Select(Shape)), "directive count/line/position/hidden/checksum data changed");
            var files = current.Select(FileToken).Where(f => f.IsKind(SyntaxKind.StringLiteralToken)).Select(f => f.ValueText).ToArray();
            Require(files.Length == 3 && files.Distinct().Count() == 1, "structured directive files diverged");
            if (operand == relative || operand == source) Require(files[0] == source && File.Exists(files[0]), "relative/absolute source rebasing failed");
            else Require(files[0].EndsWith(operand, StringComparison.Ordinal), "Windows directive backslashes were escaped/doubled");
            var again = Execute(projected, map, Path.Combine(root, "second"));
            Require(again.Success && again.Task.FilteredCompile.Single().ItemSpec == projected && File.ReadAllText(projected) == output, "projection not idempotent");
        }
        string handwritten = Path.Combine(root, "Handwritten.cs"); File.WriteAllText(handwritten, "namespace Cases { partial class Widget { internal System.Object Fire; } }");
        var conflict = Execute(handwritten, map, Path.Combine(root, "conflict"), generated: false);
        Require(!conflict.Success && conflict.Log.Errors.Any(e => e.Contains("Handwritten field conflicts with exact LF WithEvents property")), "handwritten conflict not rejected");
        File.WriteAllText(map, ""); var empty = Execute(handwritten, map, Path.Combine(root, "empty"));
        Require(!empty.Success && empty.Log.Errors.Any(e => e.Contains("symbol map is empty or malformed")), "empty map not rejected");
        Console.WriteLine("PASS actual task relative/absolute/Windows drive/UNC directive roundtrip, both channels, source SHA, idempotence, handwritten and empty-map rejection");
    }
    sealed class Log : IBuildEngine {
        public readonly List<string> Errors = new();
        public bool ContinueOnError => false; public int LineNumberOfTaskNode => 0; public int ColumnNumberOfTaskNode => 0; public string ProjectFileOfTaskNode => "build-events-regression";
        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message);
        public void LogWarningEvent(BuildWarningEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => throw new NotSupportedException();
    }
}
