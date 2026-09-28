namespace CarminePeople.Application.Common.Models;

public class Result<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool success, T? data, string? errorCode, string? errorMessage)
    {
        Success = success;
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T data) => new(true, data, null, null);
    public static Result<T> Fail(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}
