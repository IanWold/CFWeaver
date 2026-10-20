namespace CFWeaver;

public record RespondResult(string Name, Dictionary<string, string> Variables, int Response) : Result(Name, Variables);
