using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using IfcQa.Core;

internal static class HtmlReportWriter
{
    private static JsonSerializerOptions JsonOpts() => new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void WriteReport(string outDir, object payload)
    {
        Directory.CreateDirectory(outDir);

        var templateDir = LocateTemplateDir();

        var htmlTpl = File.ReadAllText(Path.Combine(templateDir, "report.template.html"));
        var css = File.ReadAllText(Path.Combine(templateDir, "report.css"));
        var js = File.ReadAllText(Path.Combine(templateDir, "report.js"));

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Prevent edge-case HTML breakage if JSON ever contains </script>
        json = json.Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);

        var html = htmlTpl
            .Replace("/* { { CSS } } */", css)
            .Replace("{ { JS } }", js)
            .Replace("{{DATA_JSON}}", json);

        File.WriteAllText(Path.Combine(outDir, "report.html"), html, Encoding.UTF8);
    }

    private static string LocateTemplateDir()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, "ReportTemplates");
        if (Directory.Exists(candidate)) return candidate;

        candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "ReportTemplates"));
        if (Directory.Exists(candidate)) return candidate;

        throw new DirectoryNotFoundException(
            $"ReportTemplates folder not found. Tried:\n- {Path.Combine(baseDir, "ReportTemplates")}\n- {candidate}");
    }




    public static string Build(IfcQaRunResult run, string rulesetName, string rulesetVersion)
    {
        var issues = run.Issues ?? new List<Issue>();

        int errors = issues.Count(i => i.Severity == Severity.Error);
        int warnings = issues.Count(i => i.Severity == Severity.Warning);
        int info = issues.Count(i => i.Severity == Severity.Info);

        var payload = new
        {
            ifcPath = run.IfcPath,
            ruleset = new { name = rulesetName, version = rulesetVersion },
            counts = new { total = issues.Count, errors, warnings, info },
            issues = issues.Select(i => new
            {
                ruleId = i.RuleId,
                severity = i.Severity.ToString(),
                ifcClass = i.IfcClass,
                globalId = i.GlobalId,
                name = i.Name ?? "",
                message = i.Message
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts());
        json = json.Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);

        var templateDir = LocateTemplateDir();

        var htmlTpl = File.ReadAllText(Path.Combine(templateDir, "report.template.html"));
        var css = File.ReadAllText(Path.Combine(templateDir, "report.css"));
        var js = File.ReadAllText(Path.Combine(templateDir, "report.js"));

        htmlTpl = htmlTpl.Replace("/* { { CSS } } */", css);
        htmlTpl = htmlTpl.Replace("// { { JS } }", js);
        htmlTpl = htmlTpl.Replace("{{DATA_JSON}}", json);

        return htmlTpl;
    }
}