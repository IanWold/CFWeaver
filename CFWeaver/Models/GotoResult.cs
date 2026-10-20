namespace CFWeaver;

public record GotoResult(string Name, Dictionary<string, string> Variables, string Goto) : Result(Name, Variables);
