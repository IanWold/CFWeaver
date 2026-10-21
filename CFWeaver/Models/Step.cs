using System.Text;

namespace CFWeaver;

public record Step(string Name, IEnumerable<Result> Results, Line Line, string? SubOperation = null)
{
    internal void AppendMermaid(StringBuilder sb, IReadOnlyDictionary<string, string> responseStates) => sb.AppendDelegate(
        Results
        .GroupBy(r => r switch
        {
            GotoResult gotoResult => gotoResult.Goto,
            RespondResult respond => responseStates[respond.Response],
            _ => throw new InvalidOperationException($"Unknown result type {r.GetType().Name}")
        })
        .Select(target => (Action<StringBuilder>)(sb => sb.AppendLine($"{Name} --> {target.Key}: {string.Join(", ", target.Select(r => r.Name))}")))
    );
}
