namespace Looma.Domain.Core;

public abstract class ResultBase(ResultStatus status, string? error = null)
{
    public string? Error { get; } = error;
    public ResultStatus Status { get; } = status;
    public bool Failed => Status != ResultStatus.Success;
    public bool Succeeded => Status == ResultStatus.Success;
}
