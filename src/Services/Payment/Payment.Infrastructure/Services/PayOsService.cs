using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs.Payments;
using Payment.Application.Settings;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

/// <summary>
/// PayOS integration: creates hosted payment links and processes inbound webhooks.
/// Uses the official payOS .NET SDK (PayOSClient) for both signature creation
/// (CreateAsync) and webhook signature verification (Webhooks.VerifyAsync).
/// </summary>
public class PayOsService : IPayOsService
{
    private static readonly JsonSerializerOptions WebhookJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly MongoDbContext _db;
    private readonly PayOsSettings _settings;
    private readonly ILogger<PayOsService> _logger;
    private readonly PayOSClient _client;

    public PayOsService(
        MongoDbContext db,
        IOptions<PayOsSettings> settings,
        ILogger<PayOsService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _logger = logger;
        _client = new PayOSClient(new PayOSOptions
        {
            ClientId = _settings.ClientId,
            ApiKey = _settings.ApiKey,
            ChecksumKey = _settings.ChecksumKey,
            BaseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
                ? "https://api-merchant.payos.vn"
                : _settings.BaseUrl
        });
    }

    public async Task<Result<PayOsLinkResponse>> CreatePaymentLinkAsync(string paymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ChecksumKey))
        {
            return Result<PayOsLinkResponse>.Fail(
                "PayOS credentials are not configured.",
                PaymentErrorCodes.PayOsSettingsMissing);
        }

        var payment = await _db.Payments.Find(x => x.Id == paymentId).FirstOrDefaultAsync(ct);
        if (payment is null)
            return Result<PayOsLinkResponse>.Fail("Payment not found.", PaymentErrorCodes.PaymentNotFound);
        if (payment.Status != PaymentStatus.Pending)
            return Result<PayOsLinkResponse>.Fail(
                $"Only Pending payments can be sent to PayOS (current: {payment.Status}).",
                PaymentErrorCodes.InvalidStatusTransition);

        // If a PayOS link was already created AND qrCode is stored, return it.
        if (!string.IsNullOrWhiteSpace(payment.PaymentLinkId) &&
            !string.IsNullOrWhiteSpace(payment.QrCode) &&
            payment.OrderCode.HasValue)
        {
            return Result<PayOsLinkResponse>.Ok(new PayOsLinkResponse
            {
                PaymentId = paymentId,
                OrderCode = payment.OrderCode.Value,
                CheckoutUrl = payment.CheckoutUrl,
                QrCode = payment.QrCode,
                PaymentLinkId = payment.PaymentLinkId,
                Amount = payment.Amount,
                Description = BuildDescription(payment)
            });
        }

        // If a link exists but no QrCode (old record), cancel it so we can create a fresh one.
        if (!string.IsNullOrWhiteSpace(payment.PaymentLinkId) && payment.OrderCode.HasValue)
        {
            try
            {
                await _client.PaymentRequests.CancelAsync(payment.PaymentLinkId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PayOS CancelAsync failed for payment {PaymentId}", paymentId);
            }
        }

        // Generate a new orderCode (old one was used by the canceled link).
        var orderCode = GenerateOrderCode();
        // PayOS requires a description ≤ 25 chars; we use a deterministic short tag.
        var description = BuildDescription(payment);
        var amountInt = (long)Math.Round(payment.Amount, MidpointRounding.AwayFromZero);
        if (amountInt <= 0)
        {
            return Result<PayOsLinkResponse>.Fail("Payment amount must be a positive integer (VND).",
                PaymentErrorCodes.ValidationFailed);
        }

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amountInt,
            Description = description,
            CancelUrl = _settings.CancelUrl,
            ReturnUrl = _settings.ReturnUrl
        };

        try
        {
            var response = await _client.PaymentRequests.CreateAsync(request);

            // Persist orderCode + checkout info so the webhook can reconcile back.
            var update = Builders<Domain.Entities.Payment>.Update
                .Set(x => x.OrderCode, orderCode)
                .Set(x => x.PaymentLinkId, response.PaymentLinkId)
                .Set(x => x.CheckoutUrl, response.CheckoutUrl)
                .Set(x => x.QrCode, response.QrCode)
                .Set(x => x.Method, PaymentMethod.EWallet);
            await _db.Payments.UpdateOneAsync(x => x.Id == paymentId, update, cancellationToken: ct);

            return Result<PayOsLinkResponse>.Ok(new PayOsLinkResponse
            {
                PaymentId = paymentId,
                OrderCode = orderCode,
                CheckoutUrl = response.CheckoutUrl,
                QrCode = response.QrCode,
                PaymentLinkId = response.PaymentLinkId,
                Amount = payment.Amount,
                Description = description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS CreatePaymentLink failed for payment {PaymentId}", paymentId);
            return Result<PayOsLinkResponse>.Fail(
                $"PayOS request failed: {ex.Message}",
                PaymentErrorCodes.PayOsRequestFailed);
        }
    }

    public async Task<Result> HandleWebhookAsync(string rawPayload, CancellationToken ct = default)
    {
        Webhook? webhook;
        try
        {
            webhook = JsonSerializer.Deserialize<Webhook>(rawPayload, WebhookJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "PayOS webhook: malformed JSON payload");
            return Result.Fail("Malformed webhook payload.", PaymentErrorCodes.ValidationFailed);
        }

        if (webhook is null || webhook.Data is null || string.IsNullOrEmpty(webhook.Signature))
        {
            _logger.LogWarning("PayOS webhook: missing data or signature");
            return Result.Fail("Webhook payload is missing data or signature.", PaymentErrorCodes.ValidationFailed);
        }

        // SDK throws InvalidSignatureException for bad signatures.
        WebhookData verified;
        try
        {
            verified = await _client.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS webhook: signature verification failed");
            return Result.Fail("Invalid webhook signature.", PaymentErrorCodes.ValidationFailed);
        }

        // Find payment by orderCode (Int64 unique sparse index).
        var payment = await _db.Payments
            .Find(x => x.OrderCode == verified.OrderCode)
            .FirstOrDefaultAsync(ct);

        if (payment is null)
        {
            _logger.LogWarning("PayOS webhook: no payment matches orderCode {OrderCode}", verified.OrderCode);
            // Idempotent: still treated as handled — caller will translate to 200.
            return Result.Ok();
        }

        var success = string.Equals(webhook.Code, "00", StringComparison.Ordinal) ||
                      webhook.Success ||
                      string.Equals(verified.Code, "00", StringComparison.Ordinal);
        if (!success)
        {
            _logger.LogWarning(
                "PayOS webhook: non-success code {Code} for orderCode {OrderCode}",
                webhook.Code, verified.OrderCode);
            return Result.Ok();
        }

        await ReconcilePaidAsync(
            payment,
            verified.Amount,
            verified.Reference,
            rawPayload,
            JsonSerializer.Serialize(verified, WebhookJsonOptions),
            ct);
        return Result.Ok();
    }
    public async Task<Result<PaymentDto>> CheckPaymentStatusAsync(string paymentId, CancellationToken ct = default)
    {

        var payment = await _db.Payments.Find(x => x.Id == paymentId).FirstOrDefaultAsync(ct);
        if (payment is null)
            return Result<PaymentDto>.Fail("Payment not found.", PaymentErrorCodes.PaymentNotFound);
        if (payment.Status != PaymentStatus.Pending)
            return Result<PaymentDto>.Ok(Map(payment));
        if (string.IsNullOrWhiteSpace(payment.PaymentLinkId) || !payment.OrderCode.HasValue)
            return Result<PaymentDto>.Fail("No PayOS payment link associated.", PaymentErrorCodes.ValidationFailed);
        if (!HasCredentials())
            return Result<PaymentDto>.Fail("PayOS credentials are not configured.", PaymentErrorCodes.PayOsSettingsMissing);

        try
        {
            var remote = await _client.PaymentRequests.GetAsync(payment.PaymentLinkId);
            if (remote.Id != payment.PaymentLinkId || remote.OrderCode != payment.OrderCode.Value)
            {
                _logger.LogError("PayOS identity mismatch for payment {PaymentId}", payment.Id);
                return Result<PaymentDto>.Fail(
                    "PayOS returned mismatched payment identity.",
                    PaymentErrorCodes.PayOsRequestFailed);
            }

            if (remote.Status != PaymentLinkStatus.Paid)
                return Result<PaymentDto>.Ok(Map(payment));

            var expectedAmount = (long)Math.Round(payment.Amount, MidpointRounding.AwayFromZero);
            if (remote.Amount != expectedAmount)
            {
                _logger.LogWarning(
                    "PayOS order amount mismatch for payment {PaymentId}: expected {Expected}, remote {Remote}",
                    payment.Id, expectedAmount, remote.Amount);
                return Result<PaymentDto>.Ok(Map(payment));
            }

            var remoteTransaction = remote.Transactions?
                .LastOrDefault(x => x.Amount == remote.AmountPaid) ??
                remote.Transactions?.LastOrDefault();
            await ReconcilePaidAsync(
                payment,
                remote.AmountPaid,
                remoteTransaction?.Reference,
                JsonSerializer.Serialize(new { payment.PaymentLinkId, payment.OrderCode }),
                JsonSerializer.Serialize(remote, WebhookJsonOptions),
                ct);

            var current = await _db.Payments.Find(x => x.Id == paymentId).FirstOrDefaultAsync(ct);
            return Result<PaymentDto>.Ok(Map(current ?? payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS status check error for payment {PaymentId}", paymentId);
            return Result<PaymentDto>.Fail(
                $"PayOS request failed: {ex.Message}",
                PaymentErrorCodes.PayOsRequestFailed);
        }
    }

    private async Task<Domain.Entities.Payment> ReconcilePaidAsync(
        Domain.Entities.Payment payment,
        long actualAmount,
        string? providerReference,
        string requestPayload,
        string responsePayload,
        CancellationToken ct)
    {
        var decision = PayOsReconciliationRules.EvaluatePaid(payment.Status, payment.Amount, actualAmount);
        if (decision == PayOsPaidReconciliationDecision.RejectAmount)
        {
            _logger.LogWarning(
                "PayOS amount mismatch for payment {PaymentId}: expected {Expected}, actual {Actual}",
                payment.Id, payment.Amount, actualAmount);
            return payment;
        }
        if (decision == PayOsPaidReconciliationDecision.RejectStatus)
        {
            _logger.LogWarning(
                "PayOS paid event ignored for payment {PaymentId} in terminal status {Status}",
                payment.Id, payment.Status);
            return payment;
        }

        var transactionCode = !string.IsNullOrWhiteSpace(providerReference)
            ? providerReference.Trim()
            : $"{payment.PaymentLinkId ?? "payment"}-{payment.OrderCode?.ToString() ?? payment.Id}";
        var now = DateTime.UtcNow;
        if (decision == PayOsPaidReconciliationDecision.Apply)
        {
            var update = Builders<Domain.Entities.Payment>.Update
                .Set(x => x.Status, PaymentStatus.Paid)
                .Set(x => x.PaidAt, now)
                .Set(x => x.Method, PaymentMethod.EWallet)
                .Set(x => x.TransactionCode, transactionCode);
            await _db.Payments.UpdateOneAsync(
                x => x.Id == payment.Id && x.Status == PaymentStatus.Pending,
                update,
                cancellationToken: ct);
        }

        var current = await _db.Payments.Find(x => x.Id == payment.Id).FirstOrDefaultAsync(ct) ?? payment;
        if (current.Status != PaymentStatus.Paid)
            return current;

        var transaction = new Domain.Entities.PaymentTransaction
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PaymentId = current.Id,
            Provider = "PayOS",
            TransactionCode = transactionCode,
            Amount = actualAmount,
            Method = PaymentMethod.EWallet,
            Status = PaymentStatus.Paid,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            CreatedAt = now,
            CompletedAt = now
        };
        try
        {
            await _db.PaymentTransactions.InsertOneAsync(transaction, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogInformation(
                "PayOS transaction {Code} for payment {PaymentId} was already recorded",
                transactionCode, current.Id);
        }

        return current;
    }

    private bool HasCredentials() =>
        !string.IsNullOrWhiteSpace(_settings.ClientId) &&
        !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(_settings.ChecksumKey);
    private static PaymentDto Map(Domain.Entities.Payment x) => new()
    {
        Id = x.Id,
        ParkingSessionId = x.ParkingSessionId,
        SubscriptionId = x.SubscriptionId,

        PlateNumber = x.PlateNumber,
        VehicleId = x.VehicleId,
        ShiftId = x.ShiftId,
        CreatedByUserId = x.CreatedByUserId,
        OwnerUserId = x.OwnerUserId,
        ConfirmedByUserId = x.ConfirmedByUserId,
        TransactionCode = x.TransactionCode,
        Amount = x.Amount,
        Method = x.Method,
        Status = x.Status,
        OrderCode = x.OrderCode,
        PaymentLinkId = x.PaymentLinkId,
        CheckoutUrl = x.CheckoutUrl,
        CreatedAt = x.CreatedAt,
        PaidAt = x.PaidAt,
        RefundedAt = x.RefundedAt,
        Note = x.Note
    };

    private static long GenerateOrderCode()
    {
        // PayOS orderCode must fit Int64 and be unique per merchant.
        // Unix milliseconds is monotonic enough for this workload and avoids the
        // same-second collision risk of a random suffix; a small random tail adds
        // safety if two requests land on the same millisecond.
        var millis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var suffix = Random.Shared.Next(0, 1000);
        return millis * 1000 + suffix;
    }

    private static string BuildDescription(Domain.Entities.Payment payment)
    {
        // PayOS requires the description ≤ 25 chars to fit on the bank transfer.
        // Use a stable prefix + last 8 of plate, sanitised.
        var plate = (payment.PlateNumber ?? string.Empty).Replace(" ", string.Empty);
        if (plate.Length > 12) plate = plate[^12..];
        var raw = $"PARK {plate}";
        return raw.Length > 25 ? raw[..25] : raw;
    }
}
