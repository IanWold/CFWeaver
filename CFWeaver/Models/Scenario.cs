namespace CFWeaver;

public record Scenario(IReadOnlyList<Scenario.Choice> Path, int Response, Dictionary<string, string> Variables)
{
    public record Choice(string Step, string Result);
}
