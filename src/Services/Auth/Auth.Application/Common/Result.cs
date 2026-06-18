namespace Auth.Application.Common;

/// <summary>
/// Result of a service operation. Carries either a value or an error code/message.
/// Controllers translate ErrorCode into HTTP status codes.
/// </summary>
public class Result<T>
{
    public bool Success { get; init; }

    public T? Value { get; init; }

    public string? Error { get; init; }

    public string? ErrorCode { get; init; }

    public static Result<T> Ok(T value) =>
        new() { Success = true, Value = value };

    public static Result<T> Fail(string error, string errorCode) =>
        new() { Success = false, Error = error, ErrorCode = errorCode };
}

public class Result
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? ErrorCode { get; init; }

    public static Result Ok() => new() { Success = true };

    public static Result Fail(string error, string errorCode) =>
        new() { Success = false, Error = error, ErrorCode = errorCode };
}

public static class AuthErrorCodes
{
    public const string DuplicateUsername = "DUPLICATE_USERNAME";
    public const string DuplicateEmail = "DUPLICATE_EMAIL";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string InvalidCurrentPassword = "INVALID_CURRENT_PASSWORD";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
}
