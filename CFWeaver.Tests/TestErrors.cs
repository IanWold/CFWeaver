namespace CFWeaver.Tests;

using static Runner;

public class TestErrors
{
    [Fact]
    public async Task InputContainsNoOperationDefinitions()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """

                """
            )
        );

        Assert.Contains("Input contains no operation definitions", console);
        Assert.Empty(files);
    }
}
