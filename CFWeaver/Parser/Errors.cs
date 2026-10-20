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

    internal static readonly GeneralError InputNoOperations = new(1, "Input contains no operation definitions");
    internal static readonly GeneralError FileDoesNotExist = new(2, "Specified input file does not exist");
    internal static readonly GeneralError OutputNotSpecified = new(3, "No output path given; use -o <path>, -o - for stdout, or --check to only validate the model");

    internal static readonly LineError OperationNoSteps = new(101, "Operation contains no step definitions");
    internal static readonly LineError OperationNoName = new(102, "Operation has no name");

    internal static readonly LineError StepNoColon = new(201, "Step definitions must include a single `:` separating the step name from the step results");
    internal static readonly LineError StepNoName = new(202, "Step has no name");
    internal static readonly LineError StepNameSpaces = new(203, "Step names must not contain spaces");
    internal static readonly LineError StepDuplicateName = new(204, "Step names must be unique within an operation");
    internal static readonly LineError StepNoResults = new(205, "Step has no results");
    internal static readonly LineError StepOutsideOperation = new(206, "Step must come after an operation heading");

    internal static readonly LineError ResultNoName = new(301, "Result has no name");
    internal static readonly LineError ResultNoReturnOrGoto = new(302, "No return or goto value given for result");
    internal static readonly LineError ResultDuplicateVariables = new(303, "Result must not contain variables with duplicate names");
    internal static readonly LineError ResultGotoUnknownStep = new(304, "Result goes to a step that does not exist");
    internal static readonly LineError ResultGotoEarlierStep = new(305, "Result must go to a later step");
    internal static readonly LineError ResultFinalStepNoReturn = new(306, "Result on the final step must have a return value");
}
