using System.Text;
using System.Text.Json;

namespace CFWeaver;

public record Operation(string Name, IEnumerable<Step> Steps)
{
    internal IReadOnlyList<Scenario> Scenarios
    {
        get
        {
            if (field is IReadOnlyList<Scenario> fieldScenarios)
            {
                return fieldScenarios;
            }

            var steps = Steps.ToDictionary(s => s.Name);
            return field = [..Walk(Steps.First(), [], [], [])];

            IEnumerable<Scenario> Walk(Step step, IReadOnlyList<Scenario.Choice> path, IReadOnlyList<string> conditions, Dictionary<string, string> variables)
            {
                foreach (var result in step.Results.OrderBy(r => r is RespondResult))
                {
                    IReadOnlyList<Scenario.Choice> nextPath = [..path, new(step.Name, result.Name)];
                    IReadOnlyList<string> nextConditions = result.Variables.TryGetValue("Condition", out var condition)
                        ? [..conditions, condition]
                        : conditions;
                    var nextVariables = new Dictionary<string, string>(variables);

                    foreach (var (name, value) in result.Variables.Where(v => v.Key != "Condition"))
                    {
                        nextVariables[name] = value;
                    }

                    if (result is RespondResult respond)
                    {
                        yield return new(nextPath, respond.Response, nextConditions, nextVariables);
                    }
                    else if (result is GotoResult gotoResult)
                    {
                        foreach (var scenario in Walk(steps[gotoResult.Goto], nextPath, nextConditions, nextVariables))
                        {
                            yield return scenario;
                        }
                    }
                }
            }
        }
    }

    internal IEnumerable<Warning> Warnings
    {
        get
        {
            var steps = Steps.ToDictionary(s => s.Name);
            HashSet<string> reached = [];
            Reach(Steps.First());

            return Steps
                .Where(s => !reached.Contains(s.Name))
                .Select(s => new Warning("No path reaches this step", s.Line));

            void Reach(Step step)
            {
                if (reached.Add(step.Name))
                {
                    foreach (var gotoResult in step.Results.OfType<GotoResult>())
                    {
                        Reach(steps[gotoResult.Goto]);
                    }
                }
            }
        }
    }

    Table ResultTable
    {
        get
        {
            if (field is Table fieldTable)
            {
                return fieldTable;
            }

            var variableNames = (IEnumerable<string>)[..Steps.SelectMany(s => s.Results).SelectMany(r => r.Variables.Keys).Except(["Condition"])];

            return field = new Table(
                [..Scenarios.Select(scenario => new TableRow([
                    ..Steps.Select(step => scenario.Path.FirstOrDefault(c => c.Step == step.Name)?.Result ?? string.Empty),
                    scenario.Response.ToString(),
                    string.Join(" and ", scenario.Conditions),
                    ..variableNames.Select(v => scenario.Variables.GetValueOrDefault(v, string.Empty))
                ]))],
                [..Steps.Select(s => s.Name), "Respond", "Condition", ..variableNames]
            );
        }
    }

    internal void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("name", Name);

        writer.WriteStartArray("scenarios");
        foreach (var scenario in Scenarios)
        {
            scenario.WriteJson(writer);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
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
