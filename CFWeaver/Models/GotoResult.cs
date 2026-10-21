namespace CFWeaver;

public record GotoResult(string Name, Dictionary<string, string> Variables, Line Line, string Goto) : Result(Name, Variables, Line);
