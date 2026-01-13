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

        var sb = new StringBuilder();
        sb.Append("""
<!doctype html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width,initial-scale=1" />
    <title>IfcQA Report</title>
    <style>
        :root {
            --bg: #0b0d12;
            --card: #121826;
            --text: #e8eefc;
            --muted: #9aa7bd;
            --line: #23304a;
            --pill: #1b2845;
        }

        body {
            margin: 0;
            font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
            background: var(--bg);
            color: var(--text);
        }

        a {
            color: inherit;
        }

        .wrap {
            max-width: 1200px;
            margin: 0 auto;
            padding: 24px;
        }

        .h1 {
            font-size: 20px;
            font-weight: 700;
            margin: 0 0 6px;
        }

        .meta {
            color: var(--muted);
            font-size: 13px;
            margin: 0 0 16px;
            word-break: break-all;
        }

        .grid {
            display: grid;
            grid-template-columns: repeat(4, minmax(0, 1fr));
            gap: 12px;
            margin: 14px 0 18px;
        }

        .card {
            background: var(--card);
            border: 1px solid var(--line);
            border-radius: 14px;
            padding: 12px 14px;
        }

        .k {
            color: var(--muted);
            font-size: 12px;
            margin-bottom: 8px;
        }

        .v {
            font-size: 20px;
            font-weight: 800;
        }

        .controls {
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
            align-items: center;
            margin: 10px 0 12px;
        }

        select,
        input {
            background: var(--card);
            border: 1px solid var(--line);
            color: var(--text);
            border-radius: 10px;
            padding: 10px 12px;
            font-size: 14px;
        }

        input {
            min-width: 280px;
            flex: 1;
        }

        .pill {
            display: inline-block;
            padding: 4px 10px;
            border-radius: 999px;
            background: var(--pill);
            border: 1px solid var(--line);
            font-size: 12px;
            color: var(--muted);
        }

        .table {
            width: 100%;
            border-collapse: collapse;
            background: var(--card);
            border: 1px solid var(--line);
            border-radius: 14px;
            overflow: hidden;
        }

        th,
        td {
            text-align: left;
            padding: 10px 12px;
            border-bottom: 1px solid var(--line);
            vertical-align: top;
        }

        th {
            font-size: 12px;
            color: var(--muted);
            font-weight: 700;
            position: sticky;
            top: 0;
            background: #0f1523;
            z-index: 1;
        }

        tr:hover td {
            background: rgba(255, 255, 255, 0.03);
        }

        .sev {
            font-weight: 700;
        }

        .sev.Error {
            color: #ff7a7a;
        }

        .sev.Warning {
            color: #ffd27a;
        }

        .sev.Info {
            color: #7ab7ff;
        }

        .small {
            font-size: 12px;
            color: var(--muted);
        }

        .copy {
            cursor: pointer;
            text-decoration: underline;
            text-decoration-color: rgba(232, 238, 252, 0.25);
        }

        .footer {
            margin-top: 14px;
            color: var(--muted);
            font-size: 12px;
        }

        @media (max-width: 900px) {
            .grid {
                grid-template-columns: 1fr 1fr;
            }
        }

        @media (max-width: 560px) {
            .grid {
                grid-template-columns: 1fr;
            }

            input {
                min-width: 0;
            }
        }
    </style>
</head>

<body>
    <div class="wrap">
        <div class="h1">IfcQA Report</div>
        <div id="meta" class="meta"></div>

        <div class="grid">
            <div class="card">
                <div class="k">Total</div>
                <div id="cTotal" class="v">0</div>
            </div>
            <div class="card">
                <div class="k">Errors</div>
                <div id="cErrors" class="v">0</div>
            </div>
            <div class="card">
                <div class="k">Warnings</div>
                <div id="cWarnings" class="v">0</div>
            </div>
            <div class="card">
                <div class="k">Info</div>
                <div id="cInfo" class="v">0</div>
            </div>
        </div>

        <div class="controls">
            <select id="fSeverity"></select>
            <select id="fRule"></select>
            <select id="fClass"></select>
            <input id="fText" type="text" placeholder="Search GlobalId / Name / Message..." />
            <span id="shown" class="pill">0 shown</span>
        </div>

        <table class="table">
            <thead>
                <tr>
                    <th style="width:110px;">Severity</th>
                    <th style="width:120px;">Rule</th>
                    <th style="width:140px;">IfcClass</th>
                    <th style="width:240px;">GlobalId</th>
                    <th style="width:200px;">Name</th>
                    <th>Message</th>
                </tr>
            </thead>
            <tbody id="rows"></tbody>
        </table>

        <div class="footer">Tip: click a GlobalId to copy it.</div>
    </div>

    <script type="application/json" id="data">
""");
        sb.Append(json);
        sb.Append("""
</script>

    <script>
        const data = JSON.parse(document.getElementById('data').textContent);
        const issues = data.issues || [];

        const meta = document.getElementById('meta');
        meta.textContent = `IFC: ${data.ifcPath} • Ruleset: ${data.ruleset.name} (${data.ruleset.version})`;

        document.getElementById('cTotal').textContent = data.counts.total;
        document.getElementById('cErrors').textContent = data.counts.errors;
        document.getElementById('cWarnings').textContent = data.counts.warnings;
        document.getElementById('cInfo').textContent = data.counts.info;

        const fSeverity = document.getElementById('fSeverity');
        const fRule = document.getElementById('fRule');
        const fClass = document.getElementById('fClass');
        const fText = document.getElementById('fText');
        const rows = document.getElementById('rows');
        const shown = document.getElementById('shown');

        function uniq(arr) { return Array.from(new Set(arr)).sort(); }
        function addOptions(select, label, values) {
            select.innerHTML = '';
            const opt0 = document.createElement('option');
            opt0.value = '';
            opt0.textContent = label;
            select.appendChild(opt0);
            values.forEach(v => {
                const o = document.createElement('option');
                o.value = v;
                o.textContent = v;
                select.appendChild(o);
            });
        }

        addOptions(fSeverity, 'Severity (All)', uniq(issues.map(i => i.severity)));
        addOptions(fRule, 'RuleId (All)', uniq(issues.map(i => i.ruleId)));
        addOptions(fClass, 'IfcClass (All)', uniq(issues.map(i => i.ifcClass)));

        function matches(issue) {
            const sev = fSeverity.value;
            const rule = fRule.value;
            const cls = fClass.value;
            const q = (fText.value || '').trim().toLowerCase();

            if (sev && issue.severity !== sev) return false;
            if (rule && issue.ruleId !== rule) return false;
            if (cls && issue.ifcClass !== cls) return false;

            if (!q) return true;
            return (
                (issue.globalId || '').toLowerCase().includes(q) ||
                (issue.name || '').toLowerCase().includes(q) ||
                (issue.message || '').toLowerCase().includes(q)
            );
        }

        function esc(s) {
            return (s || '').replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
        }

        function render() {
            const filtered = issues.filter(matches);
            rows.innerHTML = filtered.map(i => `
    <tr>
      <td class="sev ${esc(i.severity)}">${esc(i.severity)}</td>
      <td>${esc(i.ruleId)}</td>
      <td>${esc(i.ifcClass)}</td>
      <td><span class="copy" data-copy="${esc(i.globalId)}">${esc(i.globalId)}</span></td>
      <td class="small">${esc(i.name)}</td>
      <td>${esc(i.message)}</td>
    </tr>
  `).join('');

            shown.textContent = `${filtered.length} shown`;

            // attach copy handlers
            document.querySelectorAll('.copy').forEach(el => {
                el.onclick = async () => {
                    const txt = el.getAttribute('data-copy') || '';
                    try {
                        await navigator.clipboard.writeText(txt);
                        el.textContent = 'Copied!';
                        setTimeout(() => el.textContent = txt, 600);
                    } catch {
                        // clipboard might be blocked in some contexts; fallback: prompt
                        prompt('Copy GlobalId:', txt);
                    }
                };
            });
        }

        [fSeverity, fRule, fClass].forEach(s => s.addEventListener('change', render));
        fText.addEventListener('input', render);

        render();
    </script>
</body>

</html>
""");

        return sb.ToString();
    }
}