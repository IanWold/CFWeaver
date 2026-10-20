namespace CFWeaver.Tests;

using static Runner;

public class TestOutput
{
    [Fact]
    public async Task MarkdownTableHasHeaderAndSeparatorRows()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = 200 | Failure = 400
                """
            )
        );

        var lines = files["output.md"].Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var header = lines.IndexOf("|Step|Respond|Condition|");

        Assert.NotEqual(-1, header);
        Assert.Equal("|---|---|---|", lines[header + 1]);
    }

    [Fact]
    public async Task SuccessfulRunExitsWithZero()
    {
        var result = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = 200 | Failure = 400
                """
            )
        );

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Contains("output.md", result.Files.Keys);
    }

    [Fact]
    public async Task OperationNamesKeepHashCharacters()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                ## Handle C# requests

                * Step: Success = 200
                """
            )
        );

        Assert.StartsWith("# Handle C# requests", files["output.md"]);
    }
}
