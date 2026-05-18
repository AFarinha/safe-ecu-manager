namespace SafeEcu.Application.Common;

public sealed class OperationResult<T>
{
    private OperationResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public string? ErrorMessage { get; }

    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static OperationResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
