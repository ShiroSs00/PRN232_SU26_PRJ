namespace Payment.Application.Common;

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

public static class PaymentErrorCodes
{
    public const string FeePolicyNotFound = "FEE_POLICY_NOT_FOUND";
    public const string ActivePolicyNotFound = "ACTIVE_FEE_POLICY_NOT_FOUND";
    public const string PaymentNotFound = "PAYMENT_NOT_FOUND";
    public const string SubscriptionNotFound = "SUBSCRIPTION_NOT_FOUND";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string DuplicatePaymentForSession = "DUPLICATE_PAYMENT_FOR_SESSION";
    public const string PayOsRequestFailed = "PAYOS_REQUEST_FAILED";
    public const string PayOsSettingsMissing = "PAYOS_SETTINGS_MISSING";
    public const string InvalidPaymentMethod = "INVALID_PAYMENT_METHOD";
    public const string InvalidShift = "INVALID_SHIFT";
}
