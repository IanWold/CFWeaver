namespace CFWeaver;

public record GotoResult(string Name, string? Condition, Step Goto) : Result(Name, Condition)
{
    internal override IEnumerable<Result.Row> ResultsTable(string step) =>
        Goto.ResultTable.Select(t => new Result.Row([new(step, Name), ..t.Cells], [..EnumeratedCondition, ..t.Conditions]));
}
