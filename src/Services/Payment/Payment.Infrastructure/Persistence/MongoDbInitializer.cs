using MongoDB.Driver;
using Payment.Domain.Entities;
using Shared.Common.Entities;

namespace Payment.Infrastructure.Persistence;

public class MongoDbInitializer
{
    private readonly MongoDbContext _context;

    public MongoDbInitializer(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        await CreateFeePolicyIndexesAsync();
        await CreatePaymentIndexesAsync();
        await CreateSubscriptionIndexesAsync();
        await CreateSharedIndexesAsync();
    }

    private async Task CreateFeePolicyIndexesAsync()
    {
        await _context.FeePolicies.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<FeePolicy>(
                Builders<FeePolicy>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.VehicleTypeId).Ascending(x => x.IsActive),
                new CreateIndexOptions { Name = "ix_fee_policies_active_lookup" }),
            new CreateIndexModel<FeePolicy>(
                Builders<FeePolicy>.IndexKeys.Ascending(x => x.EffectiveFrom).Ascending(x => x.EffectiveTo),
                new CreateIndexOptions { Name = "ix_fee_policies_effective_range" })
        });
    }

    private async Task CreatePaymentIndexesAsync()
    {
        await _context.Payments.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Payment.Domain.Entities.Payment>(
                Builders<Payment.Domain.Entities.Payment>.IndexKeys.Ascending(x => x.ParkingSessionId),
                new CreateIndexOptions { Name = "ix_payments_parking_session_id" }),
            new CreateIndexModel<Payment.Domain.Entities.Payment>(
                Builders<Payment.Domain.Entities.Payment>.IndexKeys.Ascending(x => x.ShiftId),
                new CreateIndexOptions { Name = "ix_payments_shift_id" }),
            new CreateIndexModel<Payment.Domain.Entities.Payment>(
                Builders<Payment.Domain.Entities.Payment>.IndexKeys.Ascending(x => x.VehicleId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_payments_vehicle_created_at" }),
            new CreateIndexModel<Payment.Domain.Entities.Payment>(
                Builders<Payment.Domain.Entities.Payment>.IndexKeys.Ascending(x => x.Status).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_payments_status_created_at" }),
            new CreateIndexModel<Payment.Domain.Entities.Payment>(
                Builders<Payment.Domain.Entities.Payment>.IndexKeys.Ascending(x => x.OrderCode),
                new CreateIndexOptions
                {
                    Name = "ux_payments_order_code",
                    Unique = true,
                    Sparse = true
                })
        });

        await _context.PaymentTransactions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<PaymentTransaction>(
                Builders<PaymentTransaction>.IndexKeys.Ascending(x => x.PaymentId),
                new CreateIndexOptions { Name = "ix_payment_transactions_payment_id" }),
            new CreateIndexModel<PaymentTransaction>(
                Builders<PaymentTransaction>.IndexKeys.Ascending(x => x.Provider).Ascending(x => x.TransactionCode),
                new CreateIndexOptions { Unique = true, Name = "ux_payment_transactions_provider_code" })
        });
    }

    private async Task CreateSubscriptionIndexesAsync()
    {
        await _context.Subscriptions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(x => x.PlateNumber).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_subscriptions_plate_status" }),
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(x => x.VehicleId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_subscriptions_vehicle_status" }),
            new CreateIndexModel<Subscription>(
                Builders<Subscription>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.EndDate),
                new CreateIndexOptions { Name = "ix_subscriptions_building_end_date" })
        });

        await _context.SubscriptionPayments.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<SubscriptionPayment>(
                Builders<SubscriptionPayment>.IndexKeys.Ascending(x => x.SubscriptionId).Descending(x => x.PeriodStart),
                new CreateIndexOptions { Name = "ix_subscription_payments_subscription_period" }),
            new CreateIndexModel<SubscriptionPayment>(
                Builders<SubscriptionPayment>.IndexKeys.Ascending(x => x.VehicleId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_subscription_payments_vehicle_created_at" })
        });
    }

    private async Task CreateSharedIndexesAsync()
    {
        await _context.AuditLogs.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys.Ascending(x => x.EntityName).Ascending(x => x.EntityId),
                new CreateIndexOptions { Name = "ix_audit_logs_entity" }),
            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys.Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_audit_logs_created_at" })
        });

        await _context.Notifications.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.IsRead),
            new CreateIndexOptions { Name = "ix_notifications_user_unread" }));
    }
}
