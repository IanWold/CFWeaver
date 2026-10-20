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
}
