namespace StateFusion;

public record SuccessResult<T>(T Result);

public record FailureResult(IEnumerable<Error> Errors);

public class ParseResult<T>
{
    readonly object _value;

    private ParseResult(object value) =>
        _value = value;

    public U Map<U>(
        Func<T, U> success,
        Func<IEnumerable<Error>, U> failure
    ) =>
        _value switch
        {
            SuccessResult<T> s => success(s.Result),
            FailureResult e => failure(e.Errors),
            _ => failure([])
        };

    public Task MapAsync(
        Func<T, Task> success,
        Func<IEnumerable<Error>, Task> failure
    ) =>
        _value switch
        {
            SuccessResult<T> s => success(s.Result),
            FailureResult f => failure(f.Errors),
            _ => failure([])
        };

    public static implicit operator ParseResult<T>(SuccessResult<T> success) => new(success);
    public static implicit operator ParseResult<T>(FailureResult failure) => new(failure);
    public static implicit operator ParseResult<T>(T value) => new(new SuccessResult<T>(value!));
}
