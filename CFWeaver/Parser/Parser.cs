namespace CFWeaver;

file record OperationAst(string Name, IEnumerable<StepAst> Steps);

file record StepAst(string Name, IEnumerable<ResultAst> Results);

file record ResultAst(string Name, IEnumerable<VariableAst> Variables, string? Goto = null, int? Response = null);

file record VariableAst(string Name, string Value);

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
            else if (trimmedLine.First() == '*'|| trimmedLine.First() == '-')
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
                        ? (Result)new RespondResult(r.Name, r.Variables.ToDictionary(v => v.Name, v => v.Value), response)
                        : new GotoResult(r.Name, r.Variables.ToDictionary(v => v.Name, v => v.Value), steps.Single(s => s.Name == (r.Goto ?? steps.Last().Name)))
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
            
            var stepLines = lines.Skip(1);
            List<List<Line>> linesByOperation = [];

            foreach (var stepLine in stepLines)
            {
                if (stepLine.Text.First() == '*')
                {
                    linesByOperation.Add([stepLine]);
                }
                else if (stepLine.Text.First() == '-')
                {
                    linesByOperation.Last().Add(stepLine);
                }
            }

            return linesByOperation
                .Select(l => ParseStep(l.First(), l.Skip(1)))
                .Coalesce(steps => new OperationAst(name, steps));
        }
        
        static ParseResult<StepAst> ParseStep(Line headerLine, IEnumerable<Line> resultLines)
        {
            var result = ParseStepHeader(headerLine);
            return result.Map(
                success: step =>
                    !resultLines.Any()
                    ? step
                    : resultLines
                        .Select(l => ParseResult(l with { Text = l.Text.Replace("-", "").Trim() }))
                        .Coalesce(results => step with { Results = [..step.Results, ..results]}),
                failure: _ => result
            );
        }

        static ParseResult<StepAst> ParseStepHeader(Line line)
        {
            var split = line.Text.Split(':');

            var name = split.First().Replace("*", "").Trim();
            var isOnlyName = split.Length == 1;

            if (isOnlyName && name.Contains(' '))
            {
                return Errors.StepNoColon.Result(line);
            }
            else if (string.IsNullOrEmpty(name))
            {
                return Errors.StepNoName.Result(line);
            }
            else if (name.Contains(' '))
            {
                return Errors.StepNameSpaces.Result(line);
            }

            return isOnlyName
                ? new StepAst(name, [])
                : split.Last().Split('|')
                    .Select(l => ParseResult(new Line(l.Trim(), line.Number)))
                    .Coalesce(results => new StepAst(name, results));
        }
        
        static ParseResult<ResultAst> ParseResult(Line line)
        {
            var variablesSplit = line.Text.Split("?");

            var split = variablesSplit.First().Split('=');
            var variablesText = variablesSplit.Length > 1 ? variablesSplit.Last().Trim().Split('&') : [];
            var variables = variablesText.Select(v => {
                var assignmentSplit = v.Trim().Split("=");
                return assignmentSplit.Length > 1
                    ? new VariableAst(
                        Name: assignmentSplit.First(),
                        Value: assignmentSplit.Last()
                    )
                    : new VariableAst(
                        Name: "Condition",
                        Value: assignmentSplit.First()
                    );
            });

            var duplicateVariableNames = variables.GroupBy(v => v.Name).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicateVariableNames.Any())
            {
                return Errors.ResultDuplicateVariables.Result(line);
            }

            var name = split.First().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Errors.ResultNoName.Result(line);
            }

            if (split.Length == 1)
            {
                return new ResultAst(name, variables);
            }
            else if (split.Last().Trim() is not string last || string.IsNullOrEmpty(last))
            {
                return Errors.ResultNoReturnOrGoto.Result(line);
            }
            else if (int.TryParse(last, out var response))
            {
                return new ResultAst(name, variables, Response: response);
            }
            else
            {
                return new ResultAst(name, variables, Goto: last);
            }
        }
    }
}
