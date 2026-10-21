namespace CFWeaver;

file record OperationLines(Line Heading, List<string> Description, List<Line> Steps);

file record OperationAst(string Name, string Description, IEnumerable<StepAst> Steps);

file record StepAst(string Name, IEnumerable<ResultAst> Results, Line Line, string? SubOperation = null);

file record ResultAst(string Name, IEnumerable<VariableAst> Variables, Line Line, string? Goto = null, string? Response = null);

file record VariableAst(string Name, string Value);

public static class Parser
{
    public static ParseResult<Document> Parse(string source)
    {
        var lines = source.Split('\n');
        var preamble = new List<string>();
        var operations = new List<OperationLines>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            var trimmedLine = line.Trim();
            var operation = operations.LastOrDefault();

            if (trimmedLine.StartsWith('#'))
            {
                operations.Add(new(new(trimmedLine, i + 1), [], []));
            }
            else if (trimmedLine.StartsWith('*'))
            {
                if (operation is null)
                {
                    return Diagnostics.Errors.StepOutsideOperation.Result(new(trimmedLine, i + 1));
                }

                operation.Steps.Add(new(trimmedLine, i + 1));
            }
            else if (operation is null)
            {
                preamble.Add(line);
            }
            else if (operation.Steps.Count == 0)
            {
                operation.Description.Add(line);
            }
            else if (trimmedLine.StartsWith('-'))
            {
                operation.Steps.Add(new(trimmedLine, i + 1));
            }
        }

        if (operations.Count == 0)
        {
            return Diagnostics.Errors.InputNoOperations.Result();
        }

        return operations
            .Select(operation => ParseOperation(operation).Map(
                success: Resolve,
                failure: errors => new FailureResult(errors)
            ))
            .Coalesce(resolved => resolved.ToList())
            .Map<ParseResult<Document>>(
                success: resolved => CheckSubOperations(resolved) is { Count: > 0 } errors
                    ? new FailureResult(errors)
                    : new Document(resolved, Describe(preamble)),
                failure: errors => new FailureResult(errors)
            );

        static List<Error> CheckSubOperations(List<Operation> operations) =>
        [
            ..operations.SelectMany(operation => operation.Steps
                .Where(step => step.SubOperation is string name && (name == operation.Name || !operations.Any(o => o.Name == name)))
                .Select(step => step.SubOperation == operation.Name
                    ? Diagnostics.Errors.StepCallsOwnOperation.Error(step.Line)
                    : Diagnostics.Errors.StepCallsUnknownOperation.Error(step.Line)
                )
            )
        ];

        static string Describe(IEnumerable<string> lines)
        {
            var trimmed = (string[])[..lines.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse()];
            return string.Join("\n", trimmed.Where((line, i) => line.Length > 0 || trimmed[i - 1].Length > 0));
        }

        static ParseResult<Operation> Resolve(OperationAst ast)
        {
            var steps = ast.Steps.ToList();

            return steps
                .Select(ResolveStep)
                .Coalesce(resolved => new Operation(ast.Name, resolved, ast.Description));

            ParseResult<Step> ResolveStep(StepAst step, int index)
            {
                if (steps.FindIndex(s => s.Name == step.Name) != index)
                {
                    return Diagnostics.Errors.StepDuplicateName.Result(step.Line);
                }
                else if (!step.Results.Any())
                {
                    return Diagnostics.Errors.StepNoResults.Result(step.Line);
                }

                return step.Results
                    .Select(r => ResolveResult(r, index))
                    .Coalesce(results => new Step(step.Name, results, step.Line, step.SubOperation));
            }

            ParseResult<Result> ResolveResult(ResultAst result, int index)
            {
                var variables = result.Variables.ToDictionary(v => v.Name, v => v.Value);

                if (result.Response is string response)
                {
                    return new RespondResult(result.Name, variables, result.Line, response);
                }

                var target = result.Goto is string name
                    ? steps.FindIndex(s => s.Name == name)
                    : index + 1;

                if (target == -1)
                {
                    return Diagnostics.Errors.ResultGotoUnknownStep.Result(result.Line);
                }
                else if (target <= index)
                {
                    return Diagnostics.Errors.ResultGotoEarlierStep.Result(result.Line);
                }
                else if (target == steps.Count)
                {
                    return Diagnostics.Errors.ResultFinalStepNoReturn.Result(result.Line);
                }

                return new GotoResult(result.Name, variables, result.Line, steps[target].Name);
            }
        }

        static ParseResult<OperationAst> ParseOperation(OperationLines operation)
        {
            if (operation.Steps.Count == 0)
            {
                return Diagnostics.Errors.OperationNoSteps.Result(operation.Heading);
            }

            var name = operation.Heading.Text.TrimStart('#').Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Diagnostics.Errors.OperationNoName.Result(operation.Heading);
            }

            var linesByStep = new List<List<Line>>();

            foreach (var stepLine in operation.Steps)
            {
                if (stepLine.Text.StartsWith('*'))
                {
                    linesByStep.Add([stepLine]);
                }
                else
                {
                    linesByStep.Last().Add(stepLine);
                }
            }

            return linesByStep
                .Select(l => ParseStep(l.First(), l.Skip(1)))
                .Coalesce(steps => new OperationAst(name, Describe(operation.Description), steps));
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
            var split = line.Text.SplitOutsideLiterals(':', 2);

            var header = split.First().Replace("*", "").Trim();
            var (name, subOperation) = header.EndsWith(']') && header.IndexOf('[') is var open and > 0
                ? (header[..open].Trim(), header[(open + 1)..^1].Trim())
                : (header, null);
            var isOnlyName = split.Length == 1;

            if (isOnlyName && name.Contains(' '))
            {
                return Diagnostics.Errors.StepNoColon.Result(line);
            }
            else if (string.IsNullOrEmpty(name))
            {
                return Diagnostics.Errors.StepNoName.Result(line);
            }
            else if (name.Contains(' '))
            {
                return Diagnostics.Errors.StepNameSpaces.Result(line);
            }

            return isOnlyName
                ? new StepAst(name, [], line, subOperation)
                : split.Last().SplitOutsideLiterals('|')
                    .Select(l => ParseResult(new Line(l.Trim(), line.Number)))
                    .Coalesce(results => new StepAst(name, results, line, subOperation));
        }
        
        static ParseResult<ResultAst> ParseResult(Line line)
        {
            if (line.Text.HasUnclosedBacktick())
            {
                return Diagnostics.Errors.ResultUnclosedBacktick.Result(line);
            }

            var variablesSplit = line.Text.SplitOutsideLiterals('?', 2);

            var split = variablesSplit.First().SplitOutsideLiterals('=', 2);
            var variablesText = variablesSplit.Length > 1 ? variablesSplit.Last().SplitOutsideLiterals('&').Where(v => !string.IsNullOrWhiteSpace(v)) : [];
            var variables = variablesText.Select(v => {
                var assignmentSplit = v.SplitOutsideLiterals('=', 2).Select(s => s.Trim()).ToArray();
                return assignmentSplit.Length > 1 && IsVariableName(assignmentSplit.First())
                    ? new VariableAst(
                        Name: assignmentSplit.First(),
                        Value: assignmentSplit.Last()
                    )
                    : new VariableAst(
                        Name: "Condition",
                        Value: v.Trim()
                    );
            }).ToList();

            if (variables.Count(v => v.Name == "Condition") > 1)
            {
                return Diagnostics.Errors.ResultMultipleConditions.Result(line);
            }

            var duplicateVariableNames = variables.GroupBy(v => v.Name).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicateVariableNames.Any())
            {
                return Diagnostics.Errors.ResultDuplicateVariables.Result(line);
            }

            var name = split.First().Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Diagnostics.Errors.ResultNoName.Result(line);
            }

            if (split.Length == 1)
            {
                return new ResultAst(name, variables, line);
            }

            var isReturn = split.Last().StartsWith('>');
            var value = (isReturn ? split.Last()[1..] : split.Last()).Trim();

            if (string.IsNullOrEmpty(value))
            {
                return Diagnostics.Errors.ResultNoReturnOrGoto.Result(line);
            }
            else if (isReturn ? value.AnyCharacterOutsideLiterals(c => c is '&' or '=') : value.Any(c => char.IsWhiteSpace(c) || c is '&' or '='))
            {
                return Diagnostics.Errors.ResultValueNotSingleWord.Result(line);
            }

            return isReturn
                ? new ResultAst(name, variables, line, Response: value)
                : new ResultAst(name, variables, line, Goto: value);

            static bool IsVariableName(string name) =>
                name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.');
        }
    }

    static string[] SplitOutsideLiterals(this string text, char separator, int count = int.MaxValue)
    {
        var (isLiteral, _) = FindLiterals(text);
        var parts = new List<string>();
        var start = 0;

        for (var i = 0; i < text.Length && parts.Count < count - 1; i++)
        {
            if (text[i] == separator && !isLiteral[i])
            {
                parts.Add(text[start..i]);
                start = i + 1;
            }
        }

        return [..parts, text[start..]];
    }

    static bool AnyCharacterOutsideLiterals(this string text, Func<char, bool> predicate)
    {
        var (isLiteral, _) = FindLiterals(text);
        return text.Where((c, i) => !isLiteral[i] && predicate(c)).Any();
    }

    static bool HasUnclosedBacktick(this string text) =>
        FindLiterals(text).HasUnclosedBacktick;

    static (bool[] IsLiteral, bool HasUnclosedBacktick) FindLiterals(string text)
    {
        var isLiteral = new bool[text.Length];
        var hasUnclosedBacktick = false;

        for (var i = 0; i < text.Length;)
        {
            if (text[i] != '`')
            {
                i++;
                continue;
            }

            var openingLength = BacktickRunLength(i);
            var closing = i + openingLength;

            while (closing < text.Length && !(text[closing] == '`' && BacktickRunLength(closing) == openingLength))
            {
                closing += text[closing] == '`' ? BacktickRunLength(closing) : 1;
            }

            if (closing >= text.Length)
            {
                hasUnclosedBacktick = true;
                i += openingLength;
                continue;
            }

            Array.Fill(isLiteral, true, i, closing + openingLength - i);
            i = closing + openingLength;
        }

        return (isLiteral, hasUnclosedBacktick);

        int BacktickRunLength(int start)
        {
            var end = start;
            while (end < text.Length && text[end] == '`')
            {
                end++;
            }

            return end - start;
        }
    }
}
