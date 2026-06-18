namespace Report.Application.Common;

public class Result<T>
{
    public bool Success { get; init; }

    public T? Value { get; init; }

    public string? Error { get; init; }

    public string? ErrorCode { get; init; }

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };

    public static Result<T> Fail(string error, string errorCode) =>
        new() { Success = false, Error = error, ErrorCode = errorCode };
}

public static class ReportErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
}
