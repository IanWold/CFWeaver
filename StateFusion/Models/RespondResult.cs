namespace StateFusion;

public record RespondResult(string Name, string? Condition, int Response) : Result(Name, Condition)
{
    internal override IEnumerable<Result.Row> ResultsTable(string step) =>
        [new([new(step, Name), new("Respond", Response.ToString())], EnumeratedCondition)];
}
