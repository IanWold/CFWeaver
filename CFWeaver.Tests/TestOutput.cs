using System.Text.Json;

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

    [Fact]
    public async Task WritesToStandardOutput()
    {
        var result = await RunTestAsync("input.md -o - -f md",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = 200 | Failure = 400
                """
            )
        );

        Assert.StartsWith("# Operation 1", result.Output);
        Assert.Contains("|Step|Respond|Condition|", result.Output);
        Assert.Empty(result.Files);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task JsonListsEachScenario()
    {
        var result = await RunTestAsync("input.md -o - -f json",
            (
                "input.md",
                """
                # Get an Item

                * Authenticate: Success | Failure = 403 ? the caller is not authenticated
                * GetFromDatabase: Found = 200 ? the item exists & Cached=no | Not found = 404 ? the item doesn't exist
                """
            )
        );

        using var json = JsonDocument.Parse(result.Output);
        var operation = json.RootElement.GetProperty("operations").EnumerateArray().Single();

        string[] expected =
        [
            "Authenticate:Success > GetFromDatabase:Found = 200 | the item exists | Cached=no",
            "Authenticate:Success > GetFromDatabase:Not found = 404 | the item doesn't exist | ",
            "Authenticate:Failure = 403 | the caller is not authenticated | ",
        ];

        Assert.Equal("Get an Item", operation.GetProperty("name").GetString());
        Assert.Equal(expected, operation.GetProperty("scenarios").EnumerateArray().Select(s =>
            $"{string.Join(" > ", s.GetProperty("path").EnumerateArray().Select(c => $"{c.GetProperty("step").GetString()}:{c.GetProperty("result").GetString()}"))}"
            + $" = {s.GetProperty("response").GetInt32()}"
            + $" | {string.Join(", ", s.GetProperty("conditions").EnumerateArray().Select(c => c.GetString()))}"
            + $" | {string.Join(", ", s.GetProperty("variables").EnumerateObject().Select(v => $"{v.Name}={v.Value.GetString()}"))}"
        ).ToArray());
    }

    [Fact]
    public async Task WarnsAboutUnreachableSteps()
    {
        var result = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Done = 200 | Skip = Third
                * Second: Done = 201
                * Third: Done = 202
                """
            )
        );

        Assert.Contains("Warning (line 4 \"* Second: Done = 201\"): No path reaches this step.", result.Error);
        Assert.DoesNotContain("Third", result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("output.md", result.Files.Keys);
    }
}
