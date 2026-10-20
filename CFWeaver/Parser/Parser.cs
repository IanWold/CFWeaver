namespace CFWeaver;

file record OperationAst(string Name, IEnumerable<StepAst> Steps);

file record StepAst(string Name, IEnumerable<ResultAst> Results, Line Line);

file record ResultAst(string Name, IEnumerable<VariableAst> Variables, Line Line, string? Goto = null, int? Response = null);

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
            else if (trimmedLine.First() == '*' && linesByOperation.Count == 0)
            {
                return Errors.StepOutsideOperation.Result(new(trimmedLine, i + 1));
            }
            else if (trimmedLine.First() == '#')
            {
                linesByOperation.Add([new(trimmedLine, i + 1)]);
            }
            else if (trimmedLine.First() == '*' || trimmedLine.First() == '-' && linesByOperation.Count > 0)
            {
                linesByOperation.Last().Add(new(trimmedLine, i + 1));
            }
        }

        if (linesByOperation.Count == 0)
        {
            return Errors.InputNoOperations.Result();
        }

        return linesByOperation
            .Select(lines => ParseOperation(lines).Map(
                success: Resolve,
                failure: errors => new FailureResult(errors)
            ))
            .Coalesce(operations => new Document(operations));

        static ParseResult<Operation> Resolve(OperationAst ast)
        {
            var steps = ast.Steps.ToList();

            return steps
                .Select(ResolveStep)
                .Coalesce(resolved => new Operation(ast.Name, resolved));

            ParseResult<Step> ResolveStep(StepAst step, int index)
            {
                if (steps.FindIndex(s => s.Name == step.Name) != index)
                {
                    return Errors.StepDuplicateName.Result(step.Line);
                }
                else if (!step.Results.Any())
                {
                    return Errors.StepNoResults.Result(step.Line);
                }

                return step.Results
                    .Select(r => ResolveResult(r, index))
                    .Coalesce(results => new Step(step.Name, results, step.Line));
            }

            ParseResult<Result> ResolveResult(ResultAst result, int index)
            {
                var variables = result.Variables.ToDictionary(v => v.Name, v => v.Value);

                if (result.Response is int response)
                {
                    return new RespondResult(result.Name, variables, response);
                }

                var target = result.Goto is string name
                    ? steps.FindIndex(s => s.Name == name)
                    : index + 1;

                if (target == -1)
                {
                    return Errors.ResultGotoUnknownStep.Result(result.Line);
                }
                else if (target <= index)
                {
                    return Errors.ResultGotoEarlierStep.Result(result.Line);
                }
                else if (target == steps.Count)
                {
                    return Errors.ResultFinalStepNoReturn.Result(result.Line);
                }

                return new GotoResult(result.Name, variables, steps[target].Name);
            }
        }

        static ParseResult<OperationAst> ParseOperation(IEnumerable<Line> lines)
        {
            if (lines.Count() < 2)
            {
                return Errors.OperationNoSteps.Result(lines.First());
            }

            var name = lines.First().Text.TrimStart('#').Trim();
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
                else if (stepLine.Text.First() == '-' && linesByOperation.Count > 0)
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
                        .Select(l => ParseResult(l with { Text = l.Text[1..].Trim() }))
                        .Coalesce(results => step with { Results = [..step.Results, ..results]}),
                failure: _ => result
            );
        }

        static ParseResult<StepAst> ParseStepHeader(Line line)
        {
            var split = line.Text.Split(':', 2);

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
                ? new StepAst(name, [], line)
                : split.Last().Split('|')
                    .Select(l => ParseResult(new Line(l.Trim(), line.Number)))
                    .Coalesce(results => new StepAst(name, results, line));
        }
        
        static ParseResult<ResultAst> ParseResult(Line line)
        {
            var variablesSplit = line.Text.Split('?', 2);

            var split = variablesSplit.First().Split('=');
            var variablesText = variablesSplit.Length > 1 ? variablesSplit.Last().Trim().Split('&') : [];
            var variables = variablesText.Select(v => {
                var assignmentSplit = v.Split('=', 2, StringSplitOptions.TrimEntries);
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
                return new ResultAst(name, variables, line);
            }
            else if (split.Last().Trim() is not string last || string.IsNullOrEmpty(last))
            {
                return Errors.ResultNoReturnOrGoto.Result(line);
            }
            else if (int.TryParse(last, out var response))
            {
                return new ResultAst(name, variables, line, Response: response);
            }
            else
            {
                return new ResultAst(name, variables, line, Goto: last);
            }
        }
    }
}
