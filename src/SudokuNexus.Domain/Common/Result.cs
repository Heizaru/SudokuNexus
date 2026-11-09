namespace SudokuNexus.Domain.Common;

/// <summary>
/// Represents the outcome of a domain or application operation without relying on exceptions
/// for expected validation or business rule failures.
/// </summary>
public readonly record struct Result<T>
{
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
        ErrorCode = null;
        ErrorMessage = null;
    }

    private Result(string errorCode, string errorMessage)
    {
        Value = default;
        IsSuccess = false;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(string errorCode, string errorMessage) => new(errorCode, errorMessage);
}
