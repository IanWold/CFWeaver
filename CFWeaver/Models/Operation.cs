using System.Text;

namespace CFWeaver;

public record Operation(string Name, IEnumerable<Step> Steps)
{
    readonly Table ResultTable =
        Table.From(
            Steps.First().ResultTable,
            [..Steps.Select(s => s.Name), "Respond", ..Steps.SelectMany(s => s.VariableNames).Distinct()]
        );

    internal void AppendMermaid(StringBuilder sb) => sb
        .AppendLine("stateDiagram-v2")
        .AppendLine("direction LR")
        .AppendLine($"[*] --> {Steps.First().Name}")
        .AppendDelegate(Steps.Select(s => (Action<StringBuilder>)s.AppendMermaid));

    internal void AppendHtml(StringBuilder sb) => sb
        .AppendLine($"<section id=\"{Name.Replace(" ", "-")}\"><header><h2>{Name}</h2></header>")
        .AppendLine("<div style=\"display: flex; justify-content: center; width: 100%;margin-bottom:32px;\" class=\"mermaid\">")
        .AppendDelegate(AppendMermaid)
        .AppendLine("</div>")
        .AppendDelegate(ResultTable.AppendHtml)
        .Append("</section><main style=\"padding:0;\"><details><summary>Mermaid</summary><pre style=\"max-width:100%;\"><code style=\"max-width:100%;\">")
        .AppendDelegate(AppendMermaid)
        .Append("</pre></code></details><details><summary>CSV</summary><pre style=\"max-width:100%;\"><code style=\"max-width:100%;\">")
        .AppendDelegate(ResultTable.AppendCsv)
        .AppendLine("</pre></code></details></main><hr/>");

    internal void AppendMarkdown(StringBuilder sb) => sb
        .AppendLine($"# {Name}")
        .AppendLine()
        .AppendLine("```mermaid")
        .AppendDelegate(AppendMermaid)
        .AppendLine("```")
        .AppendLine()
        .AppendDelegate(ResultTable.AppendMarkdown)
        .AppendLine()
        .AppendLine("```csv")
        .AppendDelegate(ResultTable.AppendCsv)
        .AppendLine("```");
}
