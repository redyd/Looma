namespace Looma.Domain.Core;

public class ResultT<T> : ResultBase
{
    public T? Value { get; }

    private ResultT(T? value, ResultStatus status = ResultStatus.Success, string? error = null)
        : base(status, error)
    {
        Value = value;
    }

    public static ResultT<T> Ok(T value) => new(value);

    public static ResultT<T> Ok() => new(default);

    public static ResultT<T> NotFound(string error) => new(default, ResultStatus.NotFound, error);
    public static ResultT<T> Forbidden(string error) => new(default, ResultStatus.Forbidden, error);
    public static ResultT<T> Conflict(string error) => new(default, ResultStatus.Conflict, error);
    public static ResultT<T> Failure(string error) => new(default, ResultStatus.Failure, error);
}