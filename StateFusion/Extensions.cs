using System.Text;

namespace StateFusion;

file record ResultAccumulator<T>(IEnumerable<T> Successes, IEnumerable<Error> Errors);

public static class Extensions
{
    internal static ParseResult<U> Coalesce<T, U>(this IEnumerable<ParseResult<T>> results, Func<IEnumerable<T>, U> coalesce)
    {
        var aggregation = results.Aggregate<ParseResult<T>, ResultAccumulator<T>>(new([],[]), (accumulator, result) => result.Map(
            success: t => accumulator with { Successes = [..accumulator.Successes, t] },
            failure: errors => accumulator with { Errors = [..accumulator.Errors, ..errors] }
        ));

        return aggregation.Errors.Any()
            ? new FailureResult(aggregation.Errors)
            : coalesce(aggregation.Successes);
    }
    
    internal static StringBuilder AppendDelegate(this StringBuilder sb, params IEnumerable<Action<StringBuilder>> delegates)
    {
        foreach (var appender in delegates)
        {
            appender(sb);
        }

        return sb;
    }

    public static string PrettyPrint(this IEnumerable<Error> errors) =>
        string.Join("\n", errors);
}
