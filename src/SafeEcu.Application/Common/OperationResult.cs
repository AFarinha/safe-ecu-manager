namespace SafeEcu.Application.Common;

public sealed class OperationResult
{
    private OperationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string? ErrorMessage { get; }

    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(string errorMessage) => new(false, errorMessage);
}
