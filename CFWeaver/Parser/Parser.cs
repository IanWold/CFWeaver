namespace CFWeaver;

file record OperationAst(string Name, IEnumerable<StepAst> Steps);

file record StepAst(string Name, IEnumerable<ResultAst> Results);

file record ResultAst(string Name, string? Condition, string? Goto = null, int? Response = null);

public static class Parser
{
    public static ParseResult<Document> Parse(string source)
    {
        var lines = source.Split('\n');
        List<List<Line>> linesByOperation = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }
            else if (trimmedLine.First() == '#')
            {
                linesByOperation.Add([new(trimmedLine, i)]);
            }
            else if (trimmedLine.First() == '*')
            {
                linesByOperation.Last().Add(new(trimmedLine, i));
            }
        }

        if (linesByOperation.Count == 0)
        {
            return Errors.InputNoOperations.Result();
        }

        return linesByOperation
            .Select(ParseOperation)
            .Coalesce(e => new Document(e.Select(Raise)));

        static Operation Raise(OperationAst ast)
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

        static ParseResult<OperationAst> ParseOperation(IEnumerable<Line> lines)
        {
            if (lines.Count() < 2)
            {
                return Errors.OperationNoSteps.Result(lines.First());
            }

            var name = lines.First().Text.Split('#').Last().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Errors.OperationNoName.Result(lines.First());
            }

            return lines.Skip(1)
                .Select(ParseStep)
                .Coalesce(steps => new OperationAst(name, steps));
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
