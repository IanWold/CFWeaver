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

                * Step: Success => 200 | Failure => 400
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

                * Step: Success => 200 | Failure => 400 ? the request is invalid
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

                * Step: Success => 200
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

                * Step: Success => 200 | Failure => 400
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

                * Authenticate: Success | Failure => 403 ? the caller is not authenticated
                * GetFromDatabase: Found => 200 ? the item exists & Cached=no | Not found => 404 ? the item doesn't exist
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

                * First: Done => 200 | Skip = Third ? the item is cached
                * Second: Done => 201
                * Third: Done => 202
                """
            )
        );

        Assert.Contains("Warning 1001 (line 4 \"* Second: Done => 201\"): No path reaches this step.", result.Error);
        Assert.DoesNotContain("Third", result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("output.md", result.Files.Keys);
    }

    [Fact]
    public async Task WarnsAboutReturningAStepName()
    {
        var result = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * First: Done => 200 ? the item is cached | Skip => Third
                    - Next ? the item isn't cached
                * Second: Done => 201
                * Third: Done => 202
                """
            )
        );

        Assert.Equal(
            [
                "Warning 1002 (line 3 \"Skip => Third\"): Result returns the name of a step; use `=` to go to that step.",
                "Warning 1001 (line 6 \"* Third: Done => 202\"): No path reaches this step.",
            ],
            result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task WarnsWhenScenariosShareConditions()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # Get Fabric

                * AccessPolicy: Authorized | Unauthorized => 401 | Forbidden => 403
                * Search: Found => 200 ? fabrics match the filter | Failed => 500 ? the search service errors
                """
            )
        );

        Assert.Equal(
            ["Warning 1005 (line 3 \"* AccessPolicy: Authorized | Unauthorized => 401 | Forbidden => 403\"): Results `Unauthorized` and `Forbidden` lead to scenarios with the same conditions; give them different conditions."],
            result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task WarnsWhereScenariosWithSameConditionsSplit()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # Operation 1

                * Source: Cache ? the item is cached | Database ? the item is cached
                * Load: Loaded => 200 | Failed => 500 ? loading fails
                """
            )
        );

        Assert.Equal(
            ["Warning 1005 (line 3 \"* Source: Cache ? the item is cached | Database ? the item is cached\"): Results `Cache` and `Database` lead to scenarios with the same conditions; give them different conditions."],
            result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
    }

    [Fact]
    public async Task StepsCanNameTheOperationTheyCall()
    {
        var model =
            """
            # Bind Query

            * BindFilter [Parse Token]
                - Parsed
                - Invalid => 400 ? a filter token is malformed
            * BindExclude [Parse Token]: Parsed => 200 | Invalid => 400 ? an exclude token is malformed | Empty => 204 ? no exclude tokens are sent

            # Parse Token

            * Split: Parsed => Parsed | Malformed => Invalid ? the token has no colon
            """;

        var json = await RunTestAsync("input.md -o - -f json", ("input.md", model));
        var markdown = await RunTestAsync("input.md -o - -f md", ("input.md", model));

        using var document = JsonDocument.Parse(json.Output);
        var operations = document.RootElement.GetProperty("operations");

        Assert.Equal(
            ["BindFilter > Parse Token", "BindExclude > Parse Token"],
            operations[0].GetProperty("calls").EnumerateArray().Select(c => $"{c.GetProperty("step").GetString()} > {c.GetProperty("operation").GetString()}")
        );
        Assert.Equal(["Bind Query"], operations[1].GetProperty("calledBy").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(
            ["Warning 1004 (line 6 \"Empty => 204 ? no exclude tokens are sent\"): Step looks like it calls operation `Parse Token`, which never returns `Empty`."],
            json.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
        Assert.Contains("|BindFilter|BindExclude|Respond|Condition|", markdown.Output);
        Assert.Contains("Calls [Parse Token](#parse-token) as a sub-operation.", markdown.Output);
    }

    [Fact]
    public async Task WarnsWhenSubOperationReturnIsUnhandled()
    {
        var result = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # My Endpoint

                * Authenticate: Valid | Invalid => 403 ? the token is invalid
                * GetItem: Found => 200 | NotFound => 400 ? no item has this id | Error => 500 ? the database errors

                # Authenticate

                * CheckToken: Valid | Invalid => Invalid ? the token is malformed
                * LoadUser: Found => Valid | NotFound => Invalid ? the user was deleted | Error => Error ? the user store errors
                """
            )
        );

        Assert.Equal(
            ["Warning 1003 (line 3 \"* Authenticate: Valid | Invalid => 403 ? the token is invalid\"): Step looks like it calls operation `Authenticate`, but doesn't handle its return value `Error`."],
            result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("output.md", result.Files.Keys);
    }

    [Fact]
    public async Task WarnsWhenSubOperationNeverReturnsResult()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # My Endpoint

                * Authenticate
                    - Valid
                    - Invalid => 403 ? the token is invalid
                    - Expired => 401
                * GetItem: Found => 200 ? the item exists | NotFound => 404 ? no item has this id

                # Authenticate

                * Authenticate: Valid | Invalid => Invalid ? the token is malformed
                * LoadUser: Found => Valid | NotFound => Invalid ? the user was deleted
                """
            )
        );

        Assert.Equal(
            ["Warning 1004 (line 6 \"Expired => 401\"): Step looks like it calls operation `Authenticate`, which never returns `Expired`."],
            result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task SubOperationWithEveryReturnHandledHasNoWarnings()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # My Endpoint

                * Authenticate: Valid | Invalid => 403 ? the token is invalid
                * GetItem: Found => 200 | NotFound => 404 ? no item has this id

                # Authenticate

                * CheckToken: Valid | Invalid => Invalid ? the token is malformed
                * LoadUser: Found => Valid | NotFound => Invalid ? the user was deleted
                """
            )
        );

        Assert.Empty(result.Error);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task OutputsLinkSubOperationsToTheirCallers()
    {
        var model =
            """
            # Get Item

            * Authenticate: Valid | Invalid => 401
            * Lookup: Found => 200 | NotFound => 404

            # Authenticate

            * CheckToken: Valid | Missing => Invalid
            * LoadUser: Found => Valid | NotFound => Invalid
            """;

        var markdown = await RunTestAsync("input.md -o - -f md", ("input.md", model));
        var html = await RunTestAsync("input.md -o - -f html", ("input.md", model));
        var json = await RunTestAsync("input.md -o - -f json", ("input.md", model));
        var check = await RunTestAsync("input.md --check", ("input.md", model));

        using var document = JsonDocument.Parse(json.Output);
        var operations = document.RootElement.GetProperty("operations");
        var call = operations[0].GetProperty("calls").EnumerateArray().Single();

        Assert.Contains("Calls [Authenticate](#authenticate) as a sub-operation. This table shows only the value each call returns; the paths that lead to it are in that operation's own table.", markdown.Output);
        Assert.Contains("Called as a sub-operation by [Get Item](#get-item), whose table shows only the value it returns.", markdown.Output);
        Assert.Contains("<p>Calls <a href=\"#Authenticate\">Authenticate</a> as a sub-operation.", html.Output);
        Assert.Contains("<p>Called as a sub-operation by <a href=\"#Get-Item\">Get Item</a>,", html.Output);
        Assert.Equal("Authenticate", call.GetProperty("step").GetString());
        Assert.Equal("Authenticate", call.GetProperty("operation").GetString());
        Assert.Empty(operations[0].GetProperty("calledBy").EnumerateArray());
        Assert.Equal(["Get Item"], operations[1].GetProperty("calledBy").EnumerateArray().Select(c => c.GetString()));
        Assert.Contains("  Authenticate: 3 scenarios, called by Get Item", check.Output);
    }

    [Fact]
    public async Task HtmlInlinesStyleSheetAndLoadsDiagramsFromCdn()
    {
        var result = await RunTestAsync("input.md -o - -f html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success => 200 | Failure => 400
                """
            )
        );

        Assert.Contains("/* MVP.css v1.18", result.Output);
        Assert.Contains("Copyright (c) 2025 Andy Brewer", result.Output);
        Assert.Contains("<script src=\"https://cdn.jsdelivr.net/npm/mermaid@12.2.0/dist/mermaid.min.js\"></script>", result.Output);
        Assert.DoesNotContain("unpkg.com", result.Output);
    }

    [Fact]
    public async Task SelfContainedHtmlEmbedsDiagramLibrary()
    {
        var result = await RunTestAsync("input.md -o - --self-contained",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success => 200 | Failure => 400
                """
            )
        );

        Assert.DoesNotContain("https://cdn.jsdelivr.net", result.Output);
        Assert.Contains("Copyright (c) 2014 - 2022 Knut Sveidqvist", result.Output);
        Assert.True(result.Output.Length > 5_000_000);
        Assert.Equal(2, result.Output.Split("<script", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, result.Output.Split("</script", StringSplitOptions.None).Length - 1);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CheckSummarizesValidModelWithoutWritingOutput()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # Operation 1

                * First: Done => 200 | Skip = Third
                * Second: Done => 201
                * Third: Done => 202 | Failure => 500
                """
            )
        );

        Assert.Equal(
            """
            Model is well-formed: 1 operation, 3 scenarios.
              Operation 1: 3 scenarios
            """,
            result.Output.Trim(),
            ignoreLineEndingDifferences: true
        );
        Assert.Contains("Warning 1001 (line 4 \"* Second: Done => 201\"): No path reaches this step.", result.Error);
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task CheckReportsErrors()
    {
        var result = await RunTestAsync("input.md --check",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = Missing | Failure => 400
                """
            )
        );

        Assert.Contains("Error 304", result.Error);
        Assert.Empty(result.Output);
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task CheckWritesNoOutputEvenWithOutputPath()
    {
        var result = await RunTestAsync("input.md -o output.md --check",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success => 200 | Failure => 400
                """
            )
        );

        Assert.StartsWith("Model is well-formed: 1 operation, 2 scenarios.", result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task CheckReportsJsonDiagnostics()
    {
        var result = await RunTestAsync("input.md --check -f json",
            (
                "input.md",
                """
                # Operation 1

                * First: Done => 200 | Skip = Third ? the item is cached
                * Second: Done => 201
                * Third: Done => 202 | Failure => 500 ? the save fails
                """
            )
        );

        using var json = JsonDocument.Parse(result.Output);
        var operation = json.RootElement.GetProperty("operations").EnumerateArray().Single();
        var diagnostic = json.RootElement.GetProperty("diagnostics").EnumerateArray().Single();

        Assert.True(json.RootElement.GetProperty("wellFormed").GetBoolean());
        Assert.Equal("Operation 1", operation.GetProperty("name").GetString());
        Assert.Equal(3, operation.GetProperty("scenarios").GetInt32());
        Assert.Equal("warning", diagnostic.GetProperty("severity").GetString());
        Assert.Equal(1001, diagnostic.GetProperty("code").GetInt32());
        Assert.Equal(4, diagnostic.GetProperty("line").GetInt32());
        Assert.Equal("* Second: Done => 201", diagnostic.GetProperty("text").GetString());
        Assert.Empty(result.Error);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CheckReportsJsonErrors()
    {
        var result = await RunTestAsync("input.md --check -f json",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = Missing | Failure => 400
                """
            )
        );

        using var json = JsonDocument.Parse(result.Output);
        var diagnostic = json.RootElement.GetProperty("diagnostics").EnumerateArray().Single();

        Assert.False(json.RootElement.GetProperty("wellFormed").GetBoolean());
        Assert.Empty(json.RootElement.GetProperty("operations").EnumerateArray());
        Assert.Equal("error", diagnostic.GetProperty("severity").GetString());
        Assert.Equal(304, diagnostic.GetProperty("code").GetInt32());
        Assert.Equal(3, diagnostic.GetProperty("line").GetInt32());
        Assert.Empty(result.Error);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DiagramShowsReturnsAndLabelsEdges()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * Authenticate: Success | Failure => 403
                * Lookup: Found | Cached | NotFound => 404 | Gone => 404
                * Finish: OK => 200
                """
            )
        );

        var lines = files["output.md"].Split('\n').Select(l => l.TrimEnd('\r'));

        Assert.Equal(
            """
            stateDiagram-v2
            direction LR
            state "403" as Respond403
            state "404" as Respond404
            state "200" as Respond200
            [*] --> Authenticate
            Authenticate --> Lookup: Success
            Authenticate --> Respond403: Failure
            Lookup --> Finish: Found, Cached
            Lookup --> Respond404: NotFound, Gone
            Finish --> Respond200: OK
            Respond403 --> [*]
            Respond404 --> [*]
            Respond200 --> [*]
            """,
            string.Join("\n", lines.SkipWhile(l => l != "```mermaid").Skip(1).TakeWhile(l => l != "```")),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task TextReturnValuesAreStringsInJsonAndStatesInDiagrams()
    {
        var model =
            """
            # Operation 1

            * Validate: Valid | Invalid => throws "ArgumentException" | Negative => -1
            * Save: Saved => 201 | Saved2 => 201 | Conflict => 409 Conflict
            """;

        var json = await RunTestAsync("input.md -o - -f json", ("input.md", model));
        var markdown = await RunTestAsync("input.md -o - -f md", ("input.md", model));

        using var document = JsonDocument.Parse(json.Output);
        var responses = document.RootElement.GetProperty("operations")[0].GetProperty("scenarios").EnumerateArray()
            .Select(s => s.GetProperty("response"))
            .Select(r => r.ValueKind == JsonValueKind.Number ? $"number {r.GetInt32()}" : $"string {r.GetString()}");

        Assert.Equal(["number 201", "number 201", "string 409 Conflict", "string throws \"ArgumentException\"", "number -1"], responses);
        Assert.Contains("state \"throws #quot;ArgumentException#quot;\" as Respondthrows__ArgumentException_", markdown.Output);
        Assert.Contains("state \"-1\" as Respond_1", markdown.Output);
        Assert.Contains("Save --> Respond409_Conflict: Conflict", markdown.Output);
        Assert.Contains("Save --> Respond201: Saved, Saved2", markdown.Output);
    }

    [Fact]
    public async Task CsvQuotesFieldsAndClosesItsFence()
    {
        var (_, files) = await RunTestAsync("input.md -o output.md -f md",
            (
                "input.md",
                """
                # Operation 1

                * Step: Low => 400 ? limit is below 1, or "missing" | Ok => 200
                """
            )
        );

        var lines = files["output.md"].Split('\n').Select(l => l.TrimEnd('\r'));

        Assert.Equal(
            """"
            Step,Respond,Condition
            Low,400,"limit is below 1, or ""missing"""
            Ok,200,
            ```
            """",
            string.Join("\n", lines.SkipWhile(l => l != "```csv").Skip(1).Take(4)),
            ignoreLineEndingDifferences: true
        );
    }

    [Fact]
    public async Task HtmlEscapesModelText()
    {
        var (_, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Get an <Item>

                * Step: Low => 400 ? limit < 1 | Ok => 200
                """
            )
        );

        var html = files["output.html"];

        Assert.Contains("<h2>Get an &lt;Item&gt;</h2>", html);
        Assert.Contains("<td>limit &lt; 1</td>", html);
        Assert.DoesNotContain("<Item>", html);
        Assert.DoesNotContain("limit < 1", html);
    }

    [Fact]
    public async Task HtmlShowsLiteralTextAsCode()
    {
        var result = await RunTestAsync("input.md -o - -f html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Low => 400 ? `limit < 1 && offset > 0` | Ok => 200
                """
            )
        );

        Assert.Contains("<td><code>limit &lt; 1 &amp;&amp; offset &gt; 0</code></td>", result.Output);
    }

    [Fact]
    public async Task DescriptionsAreCopiedToOutput()
    {
        var model =
            """
            Models for the Items API.

            # Get Item

            - Source: `ItemsController.cs:10`

            Assumes the host started.

            * Step: Success => 200 | Failure => 500
            """;

        var json = await RunTestAsync("input.md -o - -f json", ("input.md", model));
        var markdown = await RunTestAsync("input.md -o - -f md", ("input.md", model));
        var html = await RunTestAsync("input.md -o - -f html", ("input.md", model));

        using var document = JsonDocument.Parse(json.Output);
        var operation = document.RootElement.GetProperty("operations").EnumerateArray().Single();

        Assert.StartsWith("CFWeaver ", document.RootElement.GetProperty("generator").GetString());
        Assert.Equal("Models for the Items API.", document.RootElement.GetProperty("description").GetString());
        Assert.Equal("- Source: `ItemsController.cs:10`\n\nAssumes the host started.", operation.GetProperty("description").GetString());
        Assert.Contains("- Source: `ItemsController.cs:10`", markdown.Output);
        Assert.StartsWith("Models for the Items API.", markdown.Output);
        Assert.Contains("<li>Source: <code>ItemsController.cs:10</code></li>", html.Output);
        Assert.Contains("<p>Assumes the host started.</p>", html.Output);
    }
}
