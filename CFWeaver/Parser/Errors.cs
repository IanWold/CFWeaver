namespace CFWeaver;

static class Errors
{
    internal record GeneralError(int Code, string Message)
    {
        public FailureResult Result() =>
            new([new(Code, Message)]);
    }

    internal record LineError(int Code, string Message)
    {
        public FailureResult Result(Line line) =>
            new([new(Code, Message, line)]);
    }

    internal static readonly GeneralError InputNoOperations = new(1, "Input contains no operation definitions.");
    internal static readonly GeneralError FileDoesNotExist = new(2, "Specified input file does not exist.");

    internal static readonly LineError OperationNoSteps = new(101, "Operation contains no step definitions");
    internal static readonly LineError OperationNoName = new(102, "Operation has no name");

    internal static readonly LineError StepNoColon = new(201, "Step definitions must include a single `:` separating the step name from the step results");
    internal static readonly LineError StepNoName = new(202, "Step has no name");
    internal static readonly LineError StepNameSpaces = new(203, "Step names must not contain spaces");

    internal static readonly LineError ResultNoName = new(301, "Result has no name");
    internal static readonly LineError ResultNoReturnOrGoto = new(302, "No return or goto value given for result");
    internal static readonly LineError ResultDuplicateVariables = new(303, "Result must not contain variables with duplicate names.");
}
