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

        Assert.Contains("1", console);
        Assert.Contains("Input contains no operation definitions", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task OperationContainsNoStepDefinitions()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                Hello, World!
                """
            )
        );

        Assert.Contains("101", console);
        Assert.Contains("Operation contains no step definitions", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task OperationHasNoName()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                #   

                * Step : Result
                """
            )
        );

        Assert.Contains("102", console);
        Assert.Contains("Operation has no name", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task StepDefinitionMustIncludeColon()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step Result
                """
            )
        );

        Assert.Contains("201", console);
        Assert.Contains("Step definitions must include a single `:` separating the step name from the step results", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task StepHasNoName()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * : Result
                """
            )
        );

        Assert.Contains("202", console);
        Assert.Contains("Step has no name", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task StepNamesMustNotContainSpaces()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * First Step: Result
                """
            )
        );

        Assert.Contains("203", console);
        Assert.Contains("Step names must not contain spaces", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task ResultHasNoName()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success | | Failure
                """
            )
        );

        Assert.Contains("301", console);
        Assert.Contains("Result has no name", console);
        Assert.Empty(files);
    }
    
    [Fact]
    public async Task ResultHasNoValue()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Result =
                """
            )
        );

        Assert.Contains("302", console);
        Assert.Contains("No return or goto value given for result", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task StepNamesMustBeUnique()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success | Failure = 400
                * Step: Success = 200
                """
            )
        );

        Assert.Contains("204", console);
        Assert.Contains("Step names must be unique within an operation", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task StepHasNoResults()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * First: Success
                * Second
                """
            )
        );

        Assert.Contains("205", console);
        Assert.Contains("Step has no results", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task ResultGoesToUnknownStep()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = Missing | Failure = 400
                """
            )
        );

        Assert.Contains("304", console);
        Assert.Contains("Result goes to a step that does not exist", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task ResultGoesToEarlierStep()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * First: Success | Failure = 400
                * Second: Retry = First | Success = 200
                """
            )
        );

        Assert.Contains("305", console);
        Assert.Contains("Result must go to a later step", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task ResultOnFinalStepHasNoReturn()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success | Failure = 400
                """
            )
        );

        Assert.Contains("306", console);
        Assert.Contains("Result on the final step must have a return value", console);
        Assert.Empty(files);
    }

    [Fact]
    public async Task ErrorsReportOneBasedLineNumbers()
    {
        var (console, files) = await RunTestAsync("input.md -o output.html",
            (
                "input.md",
                """
                # Operation 1

                * Step: Success = Missing | Failure = 400
                """
            )
        );

        Assert.Contains("(line 3 ", console);
        Assert.Empty(files);
    }
}
