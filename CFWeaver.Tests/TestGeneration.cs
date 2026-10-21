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

    [Fact]
    public async Task IgnoresCommentListsOutsideSteps()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                - This file contains the control flow models for the Items operations.

                # Get Item

                This endpoint is located at `/items/{itemId}`.

                - `itemId`: int > 0

                * Authenticate: Success | Failure = 403
                * GetFromDatabase: Found = 200 | NotFound = 404
                """
            )
        );

        Assert.Equal(
            """
            |Authenticate|GetFromDatabase|Respond|Condition|
            |---|---|---|---|
            |Success|Found|200||
            |Success|NotFound|404||
            |Failure||403||
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task KeepsHyphensInListedResults()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First
                    - Valid ? the request is well-formed
                    - Invalid = 400 ? the request is ill-formed
                * Second: Done = 200
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Respond|Condition|
            |---|---|---|---|
            |Valid|Done|200|the request is well-formed|
            |Invalid||400|the request is ill-formed|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task TrimsVariableNamesAndValues()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Success ? DidError = no | Failure = 500 ? DidError=yes
                * Second: Done = 200
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Respond|Condition|DidError|
            |---|---|---|---|---|
            |Success|Done|200||no|
            |Failure||500||yes|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task KeepsDelimitersInConditionsAndValues()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Late = 409 ? the time is after 10:30 & Query = page=2 | OnTime ? is the store open? yes
                * Second: Done = 200
                """
            )
        );

        Assert.Equal(
            """
            |First|Second|Respond|Condition|Query|
            |---|---|---|---|---|
            |OnTime|Done|200|is the store open? yes||
            |Late||409|the time is after 10:30|page=2|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task ConditionsMayContainEquals()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * Sum: Over = 400 ? limit + offset = 10000 | Fine = 200 ? Condition = the sum is lower & DidError = no
                """
            )
        );

        Assert.Equal(
            """
            |Sum|Respond|Condition|DidError|
            |---|---|---|---|
            |Over|400|limit + offset = 10000||
            |Fine|200|the sum is lower|no|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task VariablesDoNotNeedConditions()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * End: OK = 200 ? Flag=yes
                """
            )
        );

        Assert.Equal(
            """
            |End|Respond|Condition|Flag|
            |---|---|---|---|
            |OK|200||yes|
            """,
            ExtractTable(files),
            ignoreLineEndingDifferences: true
        );
    }
}
