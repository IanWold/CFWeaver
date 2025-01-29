namespace StateFusion;

file record EndpointAst(string Name, IEnumerable<StepAst> Steps);

file record StepAst(string Name, IEnumerable<ResultAst> Results);

file record ResultAst(string Name, string? Condition, string? Goto = null, int? Response = null);

file record GeneralError(int Code, string Message)
{
    public FailureResult Result() =>
        new([new(Code, Message)]);
}

file record LineError(int Code, string Message)
{
    public FailureResult Result(Line line) =>
        new([new(Code, Message, line)]);
}

file static class Errors
{
    public static readonly GeneralError InputNoEndpoints = new(1, "Input contains no endpoint definitions.");

    public static readonly LineError EndpointNoSteps = new(101, "Endpoint contains no step definitions");
    public static readonly LineError EndpointNoName = new(102, "Endpoint has no name");

    public static readonly LineError StepNoColon = new(201, "Step definitions must include a single `:` separating the step name from the step results");
    public static readonly LineError StepNoName = new(202, "Step has no name");
    public static readonly LineError StepNameSpaces = new(203, "Step names must not contain spaces");

    public static readonly LineError ResultNoName = new(301, "Result has no name");
    public static readonly LineError ResultNoReturnOrGoto = new(302, "No return or goto value given for result");
}

public static class Parser
{
    public static ParseResult<Document> Parse(string source)
    {
        var lines = source.Split('\n');
        List<List<Line>> linesByEndpoint = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }
            else if (trimmedLine.First() == '#')
            {
                linesByEndpoint.Add([new(trimmedLine, i)]);
            }
            else if (trimmedLine.First() == '*')
            {
                linesByEndpoint.Last().Add(new(trimmedLine, i));
            }
        }

        if (linesByEndpoint.Count == 0)
        {
            return Errors.InputNoEndpoints.Result();
        }

        return linesByEndpoint
            .Select(ParseEndpoint)
            .Coalesce(e => new Document(e.Select(Raise)));

        static Endpoint Raise(EndpointAst ast)
        {
            var reversedSteps = ast.Steps.Reverse();
            List<Step> steps = [];

            foreach (var astStep in reversedSteps)
            {
                steps.Add(new(
                    astStep.Name,
                    [..astStep.Results.Select(r =>
                        r.Response is int response
                        ? (Result)new RespondResult(r.Name, r.Condition, response)
                        : new GotoResult(r.Name, r.Condition, steps.Single(s => s.Name == (r.Goto ?? steps.Last().Name)))
                    )]
                ));
            }

            steps.Reverse();
            return new(ast.Name, steps);
        }

        static ParseResult<EndpointAst> ParseEndpoint(IEnumerable<Line> lines)
        {
            if (lines.Count() < 2)
            {
                return Errors.EndpointNoSteps.Result(lines.First());
            }

            var name = lines.First().Text.Split('#').Last().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Errors.EndpointNoName.Result(lines.First());
            }

            return lines.Skip(1)
                .Select(ParseStep)
                .Coalesce(steps => new EndpointAst(name, steps));
        }

        static ParseResult<StepAst> ParseStep(Line line)
        {
            var split = line.Text.Split(':');
            if (split.Length != 2)
            {
                return Errors.StepNoColon.Result(line);
            }

            var name = split.First().Replace("*", "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Errors.StepNoName.Result(line);
            }
            else if (name.Contains(' '))
            {
                return Errors.StepNameSpaces.Result(line);
            }

            return split.Last().Split('|')
                .Select(l => ParseResult(new Line(l.Trim(), line.Number)))
                .Coalesce(results => new StepAst(name, results));
        }

        static ParseResult<ResultAst> ParseResult(Line line)
        {
            var conditionSplit = line.Text.Split("?");

            var split = conditionSplit.First().Split('=');
            var condition = conditionSplit.Length > 1 ? conditionSplit.Last().Trim() : null;

            var name = split.First().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Errors.ResultNoName.Result(line);
            }

            if (split.Length == 1)
            {
                return new ResultAst(name, condition);
            }
            else if (split.Last().Trim() is not string last || string.IsNullOrEmpty(last))
            {
                return Errors.ResultNoReturnOrGoto.Result(line);
            }
            else if (int.TryParse(last, out var response))
            {
                return new ResultAst(name, condition, Response: response);
            }
            else
            {
                return new ResultAst(name, condition, Goto: last);
            }
        }
    }
}
