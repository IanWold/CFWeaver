namespace CFWeaver;

public record RespondResult(string Name, Dictionary<string, string> Variables, int Response) : Result(Name, Variables)
{
    internal override IEnumerable<Row> ResultsTable(string step) =>
        [new([new(step, Name), new("Respond", Response.ToString())], Variables)];
}
