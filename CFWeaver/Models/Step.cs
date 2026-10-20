using System.Text;

namespace CFWeaver;

public record Step(string Name, IEnumerable<Result> Results, Line Line)
{
    internal void AppendMermaid(StringBuilder sb)
    {
        if (Results.OfType<GotoResult>() is IEnumerable<GotoResult> gotoResults && gotoResults.Any())
        {
            sb.AppendJoin("\r\n", gotoResults.Select(g => $"{Name} --> {g.Goto}"));
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"{Name} --> [*]");
        }
    }
}
