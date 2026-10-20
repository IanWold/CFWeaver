using System.Text;

namespace CFWeaver;

public record Operation(string Name, IEnumerable<Step> Steps)
{
    Table ResultTable
    {
        get
        {
            if (field is Table fieldTable)
            {
                return fieldTable;
            }
            
            var steps = Steps.ToDictionary(s => s.Name);
            var scenarios = Walk(Steps.First(), [], []);
            var variableNames = (IEnumerable<string>)["Condition", ..Steps.SelectMany(s => s.Results).SelectMany(r => r.Variables.Keys).Except(["Condition"])];

            return field = new Table(
                [..scenarios.Select(scenario => new TableRow([
                    ..Steps.Select(step => scenario.Path.FirstOrDefault(c => c.Step == step.Name)?.Result ?? string.Empty),
                    scenario.Response.ToString(),
                    ..variableNames.Select(v => scenario.Variables.GetValueOrDefault(v, string.Empty))
                ]))],
                [..Steps.Select(s => s.Name), "Respond", ..variableNames]
            );

            IEnumerable<Scenario> Walk(Step step, IReadOnlyList<Scenario.Choice> path, Dictionary<string, string> variables)
            {
                foreach (var result in step.Results.OrderBy(r => r is RespondResult))
                {
                    IReadOnlyList<Scenario.Choice> nextPath = [..path, new(step.Name, result.Name)];
                    var nextVariables = new Dictionary<string, string>(variables);

                    foreach (var (name, value) in result.Variables)
                    {
                        nextVariables[name] = name == "Condition" && variables.TryGetValue(name, out var prior)
                            ? $"{prior} and {value}"
                            : value;
                    }

                    if (result is RespondResult respond)
                    {
                        yield return new(nextPath, respond.Response, nextVariables);
                    }
                    else if (result is GotoResult gotoResult)
                    {
                        foreach (var scenario in Walk(steps[gotoResult.Goto], nextPath, nextVariables))
                        {
                            yield return scenario;
                        }
                    }
                }
            }
        }
    }

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
