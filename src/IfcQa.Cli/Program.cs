using System.Text;
using System.Text.Json;
using IfcQa.Core;

var ifcPath = args.Length > 0 ? args[0] : @"C:\Users\Tommy Lee\Documents\Project\IfcQaTool\samples\Building-Architecture.ifc";

var analyzer = new IfcAnalyzer();
var report = analyzer.Analyze(ifcPath);

var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);

File.WriteAllText("report.json", json);
Console.WriteLine("\nWrote report.json.");

var run = analyzer.AnalyzeWithRules(ifcPath);

var issueJson = JsonSerializer.Serialize(run, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("issues.json", json);
Console.WriteLine("\nWrote issues.json.");

File.WriteAllText("issue.csv", BuildIssuesCsv(run.Issues));
Console.WriteLine("Wrote issues.csv");

static string BuildIssuesCsv(List<Issue> issues)
{
    static string Esc(string? s)
    {
        s ??= "";
        return $"\"{s.Replace("\"", "\"\"")}\"";
    }

    var sb = new StringBuilder();
    sb.AppendLine("RuleId,Severity,IfcClass,GlobalId,Name,Message");

    foreach (var i in issues)
    {
        sb.AppendLine(string.Join(",",
            Esc(i.RuleId),
            Esc(i.Severity.ToString()),
            Esc(i.IfcClass),
            Esc(i.GlobalId),
            Esc(i.Name),
            Esc(i.Message)
            ));
    }

    return sb.ToString();
}

