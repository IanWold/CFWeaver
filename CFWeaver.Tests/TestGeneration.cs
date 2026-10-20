namespace CFWeaver.Tests;

using static Runner;

public class TestGeneration
{
    static string ExtractTable(Dictionary<string, string> files) =>
        string.Join("\n", files["output.md"].Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.StartsWith('|')));

    [Fact]
    public async Task EnumeratesEveryPathThroughTheOperation()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Get an Item

                * Authenticate: Success | Failure = 403 ? I am not authenticated to access items
                * Validate: Success | Failure = 400 ? the request is invalid
                * GetFromDatabase: Found ? the item exists | Not found = 404 ? the item does not exist | Error = 500 ? The items table errors on select
                * Authorize: Success = 200 | Failure = 401 ? I am not authorized to access the specific item
                """
            )
        );

        Assert.Equal(
            """
            |Authenticate|Validate|GetFromDatabase|Authorize|Respond|Condition|
            |---|---|---|---|---|---|
            |Success|Success|Found|Success|200|the item exists|
            |Success|Success|Found|Failure|401|the item exists and I am not authorized to access the specific item|
            |Success|Success|Not found||404|the item does not exist|
            |Success|Success|Error||500|The items table errors on select|
            |Success|Failure|||400|the request is invalid|
            |Failure||||403|I am not authenticated to access items|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task ListsContinuingResultsBeforeReturningResults()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Done = 200 | Next
                * Second: Done = 201
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Respond|Condition|
            |---|---|---|---|
            |Next|Done|201||
            |Done||200||
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task FollowsExplicitGotos()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Skip = Third | Next
                * Second: Done = 200
                * Third: Done = 201
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Third|Respond|Condition|
            |---|---|---|---|---|
            |Skip||Done|201||
            |Next|Done||200||
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task JoinsConditionsAlongThePath()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Success ? first holds | Failure = 400 ? first fails
                * Second: Success ? second holds
                * Third: Done = 200 ? third holds
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Third|Respond|Condition|
            |---|---|---|---|---|
            |Success|Success|Done|200|first holds and second holds and third holds|
            |Failure|||400|first fails|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task LaterAssignmentsOverrideEarlierOnes()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Fast ? Mode=fast | Slow ? Mode=slow
                * Second: Reset = 200 ? Mode=none | Keep = 201
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Respond|Condition|Mode|
            |---|---|---|---|---|
            |Fast|Reset|200||none|
            |Fast|Keep|201||fast|
            |Slow|Reset|200||none|
            |Slow|Keep|201||slow|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }
}
