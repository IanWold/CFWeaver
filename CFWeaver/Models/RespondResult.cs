namespace CFWeaver;

public record RespondResult(string Name, Dictionary<string, string> Variables, Line Line, string Response) : Result(Name, Variables, Line);
