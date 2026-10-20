
namespace CFWeaver.Tests;

public record RunResult(string Output, string Error, Dictionary<string, string> Files, int ExitCode)
{
    public void Deconstruct(out string console, out Dictionary<string, string> files)
    {
        console = Output + Error;
        files = Files;
    }
}
