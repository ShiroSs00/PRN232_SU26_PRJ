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

        var orderCode = payment.OrderCode ?? GenerateOrderCode();
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

        // Idempotent: already paid — log and ack.
        if (payment.Status == PaymentStatus.Paid)
        {
            _logger.LogInformation(
                "PayOS webhook: payment {PaymentId} already paid (orderCode {OrderCode}); skipping",
                payment.Id, verified.OrderCode);
            return Result.Ok();
        }

        // Only flip to Paid when PayOS reports success (data.code == "00") and it was Pending.
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

        if (payment.Status != PaymentStatus.Pending)
        {
            // Refunded / Cancelled / Failed — refuse to silently overwrite.
            _logger.LogWarning(
                "PayOS webhook: payment {PaymentId} is {Status}, not Pending; ignoring success",
                payment.Id, payment.Status);
            return Result.Ok();
        }

        var now = DateTime.UtcNow;
        var update = Builders<Domain.Entities.Payment>.Update
            .Set(x => x.Status, PaymentStatus.Paid)
            .Set(x => x.PaidAt, now)
            .Set(x => x.Method, PaymentMethod.EWallet)
            .Set(x => x.TransactionCode, verified.Reference);
        await _db.Payments.UpdateOneAsync(x => x.Id == payment.Id, update, cancellationToken: ct);

        var transaction = new Domain.Entities.PaymentTransaction
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PaymentId = payment.Id,
            Provider = "PayOS",
            // Reference may be empty for some webhook events — fall back to paymentLinkId+orderCode
            // to keep the unique (provider, transactionCode) index satisfied.
            TransactionCode = !string.IsNullOrWhiteSpace(verified.Reference)
                ? verified.Reference!
                : $"{verified.PaymentLinkId}-{verified.OrderCode}",
            Amount = verified.Amount,
            Method = PaymentMethod.EWallet,
            Status = PaymentStatus.Paid,
            RequestPayload = rawPayload,
            ResponsePayload = JsonSerializer.Serialize(verified, WebhookJsonOptions),
            CreatedAt = now,
            CompletedAt = now
        };

        try
        {
            await _db.PaymentTransactions.InsertOneAsync(transaction, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Same PayOS reference replayed — keep idempotent.
            _logger.LogInformation(
                "PayOS webhook: duplicate transaction {Code} for payment {PaymentId}; ignoring",
                transaction.TransactionCode, payment.Id);
        }

        return Result.Ok();
    }

    private static long GenerateOrderCode()
    {
        // PayOS orderCode must fit Int64 and be unique per merchant; using unix-seconds-based
        // value with a small random suffix is collision-safe for this workload.
        var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var suffix = Random.Shared.Next(100, 1000);
        return seconds * 1000 + suffix;
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
