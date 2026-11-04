using System;using System.IO;using System.Linq;using System.Collections.Generic;using Microsoft.CodeAnalysis;using Microsoft.CodeAnalysis.CSharp;using Microsoft.CodeAnalysis.CSharp.Syntax;using Microsoft.Build.Framework;
public sealed class LfFilterWithEvents:Microsoft.Build.Utilities.Task {
 [Required] public ITaskItem[] CompileFiles{get;set;}
 public ITaskItem[] GeneratedFiles{get;set;}
 [Required] public string MapFile{get;set;}
 [Required] public string OutputDirectory{get;set;}
 [Output] public ITaskItem[] FilteredCompile{get;set;}
 [Output] public ITaskItem[] FilteredGenerated{get;set;}
 public override bool Execute(){try{

        var rows = File.ReadAllLines(MapFile).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Split('\t')).ToArray();
        if (rows.Length == 0 || rows.Any(r => r.Length != 3)) throw new InvalidDataException("LF WithEvents symbol map is empty or malformed.");
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows) if (!map.TryAdd(row[0] + "." + row[1], row[2])) throw new InvalidDataException("Conflicting LF WithEvents map: " + row[0] + "." + row[1]);
        Directory.CreateDirectory(OutputDirectory);
        var generatedInputs = new HashSet<string>((GeneratedFiles ?? Array.Empty<Microsoft.Build.Framework.ITaskItem>()).Select(i => Path.GetFullPath(i.ItemSpec)), StringComparer.OrdinalIgnoreCase);
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int replaced = 0;
        Func<Microsoft.Build.Framework.ITaskItem, Microsoft.Build.Framework.ITaskItem> project = item => {
          string path = Path.GetFullPath(item.ItemSpec);
          string output;
          if (!cache.TryGetValue(path, out output)) {
            output = path;
            if (File.Exists(path) && path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) {
              string text = File.ReadAllText(path);
              if (!text.StartsWith("// LF typed WithEvents projection", StringComparison.Ordinal)) {
                bool isWpfGenerated = generatedInputs.Contains(path) || (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) && text.Contains("GeneratedCodeAttribute(\"PresentationBuildTasks\"") && text.Contains("System.Windows.Markup.IComponentConnector"));
                var syntax = CSharpSyntaxTree.ParseText(text).GetRoot();
                var fields = new List<FieldDeclarationSyntax>();
                foreach (var field in syntax.DescendantNodes().OfType<FieldDeclarationSyntax>()) {
                  var owners = field.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.ValueText);
                  var spaces = field.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString());
                  string owner = string.Join(".", spaces.Concat(owners));
                  foreach (var variable in field.Declaration.Variables) {
                    string expected;
                    if (!map.TryGetValue(owner + "." + variable.Identifier.ValueText, out expected)) continue;
                    if (!isWpfGenerated) throw new InvalidDataException("Handwritten field conflicts with exact LF WithEvents property: " + owner + "." + variable.Identifier.ValueText);
                    if (field.Declaration.Variables.Count != 1 || field.Declaration.Type.ToString().Replace("global::", "").Replace(" ", "") != expected.Replace("global::", "").Replace(" ", "")) throw new InvalidDataException("LF WithEvents field shape changed: " + owner + "." + variable.Identifier.ValueText);
                    fields.Add(field);
                  }
                }
                if (fields.Count != 0) {
                  string key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(path))).Substring(0, 20);
                  output = Path.Combine(OutputDirectory, key + "." + Path.GetFileName(path));
                  string projected = "// LF typed WithEvents projection; original WPF output retained.\n" + new SourceDirectiveRebaser(path).Visit(syntax.RemoveNodes(fields, SyntaxRemoveOptions.KeepExteriorTrivia)).ToFullString();
                  if (!File.Exists(output) || File.ReadAllText(output) != projected) File.WriteAllText(output, projected);
                  replaced += fields.Count;
                }
              }
            }
            cache.Add(path, output);
          }
          var result = new Microsoft.Build.Utilities.TaskItem(item); result.ItemSpec = output; return result;
        };
        FilteredCompile = CompileFiles.Select(project).ToArray();
        FilteredGenerated = (GeneratedFiles ?? Array.Empty<Microsoft.Build.Framework.ITaskItem>()).Select(project).ToArray();
        Log.LogMessage(Microsoft.Build.Framework.MessageImportance.High, "LF WithEvents projection: {0} exact generated fields; temporary and final inputs both processed.", replaced);
      
return true;}catch(Exception error){Log.LogErrorFromException(error,true);return false;}}
 // Moving a .g.cs changes the base directory of its XAML debug/source directives.
 // Rewrite only their structured file tokens; keep line numbers, hidden/default,
 // checksum algorithm/data and all original SDK output untouched.
 private sealed class SourceDirectiveRebaser : CSharpSyntaxRewriter {
  private readonly string originalDirectory;
  internal SourceDirectiveRebaser(string generatedFile) : base(visitIntoStructuredTrivia: true) {
   originalDirectory = Path.GetDirectoryName(Path.GetFullPath(generatedFile));
  }
  private SyntaxToken SourceFile(SyntaxToken file) {
   if (!file.IsKind(SyntaxKind.StringLiteralToken)) return file;
   string source = Path.GetFullPath(file.ValueText, originalDirectory);
   // Directive filenames keep raw backslashes; they are not escaped C# value strings.
   return SyntaxFactory.Literal(file.LeadingTrivia, "\"" + source + "\"", source, file.TrailingTrivia);
  }
  public override SyntaxNode VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node) => node.WithFile(SourceFile(node.File));
  public override SyntaxNode VisitLineSpanDirectiveTrivia(LineSpanDirectiveTriviaSyntax node) => node.WithFile(SourceFile(node.File));
  public override SyntaxNode VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node) => node.WithFile(SourceFile(node.File));
 }

}
