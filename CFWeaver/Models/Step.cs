using System.Text;

namespace CFWeaver;

public record Step(string Name, IEnumerable<Result> Results)
{
    internal readonly IEnumerable<Result.Row> ResultTable =
    [
        ..Results.OfType<GotoResult>().SelectMany(r => r.ResultsTable(Name)),
        ..Results.OfType<RespondResult>().SelectMany(r => r.ResultsTable(Name)),
    ];

    internal readonly IEnumerable<string> VariableNames =
        ["Condition", ..Results.SelectMany(r => r.Variables.Keys).Distinct().Except(["Condition"])];

    internal void AppendMermaid(StringBuilder sb)
    {
        if (Results.OfType<GotoResult>() is IEnumerable<GotoResult> gotoResults && gotoResults.Any())
        {
            sb.AppendJoin("\r\n", gotoResults.Select(g => $"{Name} --> {g.Goto.Name}"));
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"{Name} --> [*]");
        }
    }
}
