namespace CFWeaver.Tests;

using static Runner;

public class TestErrors
{
    [Fact]
    public async Task InputContainsNoOperationDefinitions()
    {
        (string console, Dictionary<string, string> files) = await RunTestAsync(
            ["input.md", "-o", "output.html"],
            new() {
                ["input.md"] = """

                    """
            }
        );

        Assert.Contains("Input contains no operation definitions", console);
        Assert.Empty(files);
    }
}
