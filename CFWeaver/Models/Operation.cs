using System.Text;
using System.Text.Json;

namespace CFWeaver;

public record Operation(string Name, IEnumerable<Step> Steps, string Description = "")
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
                .Select(s => Diagnostics.Warnings.StepUnreachable.Warning(s.Line))
                .Concat(Steps
                    .SelectMany(s => s.Results)
                    .OfType<RespondResult>()
                    .Where(r => steps.ContainsKey(r.Response))
                    .Select(r => Diagnostics.Warnings.ReturnNamesStep.Warning(r.Line))
                )
                .Concat(SharedConditionWarnings())
                .OrderBy(w => w.Line.Number);

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

            IEnumerable<Warning> SharedConditionWarnings()
            {
                var splits = new Dictionary<string, HashSet<string>>();

                foreach (var paths in Scenarios.GroupBy(s => string.Join(" and ", s.Conditions)).Select(g => g.Select(s => s.Path).ToList()))
                {
                    for (var i = 0; i < paths.Count; i++)
                    {
                        for (var j = i + 1; j < paths.Count; j++)
                        {
                            var (first, second) = paths[i].Zip(paths[j]).First(choices => choices.First != choices.Second);

                            if (!splits.TryGetValue(first.Step, out var results))
                            {
                                splits[first.Step] = results = [];
                            }

                            results.UnionWith([first.Result, second.Result]);
                        }
                    }
                }

                return splits.Select(split => Diagnostics.Warnings.ScenariosShareConditions.Warning(
                    steps[split.Key].Line,
                    steps[split.Key].Results.Select(r => r.Name).Where(split.Value.Contains).Distinct().Select(name => $"`{name}`").JoinAnd()
                ));
            }
        }
    }

    internal IEnumerable<(Step Step, Operation Operation)> Calls(IEnumerable<Operation> operations) =>
        Steps.SelectMany(step => operations
            .Where(o => o.Name == (step.SubOperation ?? step.Name) && !ReferenceEquals(o, this))
            .Take(1)
            .Select(o => (step, o))
        );

    internal IEnumerable<Operation> CalledBy(IEnumerable<Operation> operations) =>
        operations.Where(o => o.Calls(operations).Any(call => ReferenceEquals(call.Operation, this)));

    internal IEnumerable<Warning> SubOperationWarnings(IEnumerable<Operation> operations)
    {
        foreach (var (step, subOperation) in Calls(operations))
        {
            var returns = subOperation.Scenarios.Select(s => s.Response).Distinct().ToList();

            foreach (var unhandled in returns.Except(step.Results.Select(r => r.Name)))
            {
                yield return Diagnostics.Warnings.SubOperationReturnUnhandled.Warning(step.Line, subOperation.Name, unhandled);
            }

            foreach (var result in step.Results.Where(r => !returns.Contains(r.Name)))
            {
                yield return Diagnostics.Warnings.SubOperationNeverReturns.Warning(result.Line, subOperation.Name, result.Name);
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
                    scenario.Response,
                    string.Join(" and ", scenario.Conditions),
                    ..variableNames.Select(v => scenario.Variables.GetValueOrDefault(v, string.Empty))
                ]))],
                [..Steps.Select(s => s.Name), "Respond", "Condition", ..variableNames]
            );
        }
    }

    IEnumerable<string> SubOperationNotes(IEnumerable<Operation> operations, Func<Operation, string> link)
    {
        var calls = Calls(operations).Select(call => call.Operation).Distinct().ToList();
        if (calls.Count > 0)
        {
            yield return $"Calls {calls.Select(link).JoinAnd()} as {(calls.Count == 1 ? "a sub-operation" : "sub-operations")}. This table shows only the value each call returns; the paths that lead to it are in that operation's own table.";
        }

        var callers = CalledBy(operations).ToList();
        if (callers.Count > 0)
        {
            yield return $"Called as a sub-operation by {callers.Select(link).JoinAnd()}, whose {(callers.Count == 1 ? "table shows" : "tables show")} only the value it returns.";
        }
    }

    internal void WriteJson(Utf8JsonWriter writer, IEnumerable<Operation> operations)
    {
        writer.WriteStartObject();
        writer.WriteString("name", Name);
        writer.WriteString("description", Description);

        writer.WriteStartArray("calls");
        foreach (var (step, operation) in Calls(operations))
        {
            writer.WriteStartObject();
            writer.WriteString("step", step.Name);
            writer.WriteString("operation", operation.Name);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteStartArray("calledBy");
        foreach (var caller in CalledBy(operations))
        {
            writer.WriteStringValue(caller.Name);
        }
        writer.WriteEndArray();

        writer.WriteStartArray("scenarios");
        foreach (var scenario in Scenarios)
        {
            scenario.WriteJson(writer);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    Dictionary<string, string> ResponseStates
    {
        get
        {
            if (field is Dictionary<string, string> fieldStates)
            {
                return fieldStates;
            }

            HashSet<string> states = [..Steps.Select(s => s.Name)];

            return field = Steps
                .SelectMany(s => s.Results)
                .OfType<RespondResult>()
                .Select(r => r.Response)
                .Distinct()
                .ToDictionary(
                    r => r,
                    r => State($"Respond{string.Concat(r.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_'))}")
                );

            string State(string state) =>
                states.Add(state)
                ? state
                : State($"{state}_");
        }
    }

    internal void AppendMermaid(StringBuilder sb) => sb
        .AppendLine("stateDiagram-v2")
        .AppendLine("direction LR")
        .AppendDelegate(ResponseStates.Select(r => (Action<StringBuilder>)(sb => sb.AppendLine($"state \"{r.Key.Replace("\"", "#quot;")}\" as {r.Value}"))))
        .AppendLine($"[*] --> {Steps.First().Name}")
        .AppendDelegate(Steps.Select(s => (Action<StringBuilder>)(sb => s.AppendMermaid(sb, ResponseStates))))
        .AppendDelegate(ResponseStates.Values.Select(state => (Action<StringBuilder>)(sb => sb.AppendLine($"{state} --> [*]"))));

    string MermaidHtml =>
        new StringBuilder().AppendDelegate(AppendMermaid).ToString().HtmlEncode();

    string Anchor =>
        Name.Replace(" ", "-").HtmlEncode();

    internal string NavigationHtml =>
        $"<li><a href=\"#{Anchor}\">{Name.HtmlEncode()}</a></li>";

    internal void AppendHtml(StringBuilder sb, IEnumerable<Operation> operations) => sb
        .AppendLine($"<section id=\"{Anchor}\"><header><h2>{Name.HtmlEncode()}</h2></header>")
        .Append(Description.MarkdownToHtml())
        .AppendJoin("", SubOperationNotes(operations, o => $"<a href=\"#{o.Anchor}\">{o.Name.HtmlEncode()}</a>").Select(note => $"<p>{note}</p>"))
        .AppendLine("<div style=\"display: flex; justify-content: center; width: 100%;margin-bottom:32px;\" class=\"mermaid\">")
        .Append(MermaidHtml)
        .AppendLine("</div>")
        .AppendDelegate(ResultTable.AppendHtml)
        .Append("</section><main style=\"padding:0;\"><details><summary>Mermaid</summary><pre style=\"max-width:100%;\"><code style=\"max-width:100%;\">")
        .Append(MermaidHtml)
        .Append("</code></pre></details><details><summary>CSV</summary><pre style=\"max-width:100%;\"><code style=\"max-width:100%;\">")
        .Append(new StringBuilder().AppendDelegate(ResultTable.AppendCsv).ToString().HtmlEncode())
        .AppendLine("</code></pre></details></main><hr/>");

    internal void AppendMarkdown(StringBuilder sb, IEnumerable<Operation> operations) => sb
        .AppendLine($"# {Name}")
        .AppendLine()
        .Append(Description.Length > 0 ? $"{Description.ReplaceLineEndings()}{Environment.NewLine}{Environment.NewLine}" : "")
        .AppendJoin("", SubOperationNotes(operations, o => $"[{o.Name}](#{o.Name.MarkdownAnchor()})").Select(note => $"{note}{Environment.NewLine}{Environment.NewLine}"))
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
