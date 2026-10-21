namespace CFWeaver.Tests;

using static Runner;

public class TestHelp
{
    [Fact]
    public async Task HelpListsTopics()
    {
        var result = await RunTestAsync("help");

        Assert.Contains("cfweaver help recipes", result.Output);
        Assert.Contains("cfweaver help <code>", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HelpPrintsModelingRecipes()
    {
        var result = await RunTestAsync("help recipes");

        Assert.Contains("## Loops", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HelpPrintsEveryError()
    {
        var result = await RunTestAsync("help errors");

        Assert.Contains("## `309`", result.Output);
        Assert.Contains("## `1004`", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HelpPrintsOneErrorIncludingItsExamples()
    {
        var result = await RunTestAsync("help 1003");

        Assert.StartsWith("## `1003`", result.Output);
        Assert.Contains("# Authenticate", result.Output);
        Assert.Contains("Adding one, such as `Error => 500`, fixes it.", result.Output);
        Assert.DoesNotContain("## `1004`", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HelpRejectsUnknownTopic()
    {
        var result = await RunTestAsync("help 999");

        Assert.Contains("Error 5: No help topic with that name", result.Error);
        Assert.Empty(result.Output);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task GuideListsHelpTopics()
    {
        var result = await RunTestAsync("--help");

        Assert.Contains("cfweaver help errors", result.Output);
    }
}
