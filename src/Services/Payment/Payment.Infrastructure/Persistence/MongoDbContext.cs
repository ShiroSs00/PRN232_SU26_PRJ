using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Payment.Domain.Entities;
using Shared.Common.Entities;
using Shared.Common.Settings;

namespace Payment.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public string DatabaseName => _database.DatabaseNamespace.DatabaseName;

    public IMongoDatabase Database => _database;

    public IMongoCollection<FeePolicy> FeePolicies =>
        _database.GetCollection<FeePolicy>("fee_policies");

    public IMongoCollection<Payment.Domain.Entities.Payment> Payments =>
        _database.GetCollection<Payment.Domain.Entities.Payment>("payments");

    public IMongoCollection<PaymentTransaction> PaymentTransactions =>
        _database.GetCollection<PaymentTransaction>("payment_transactions");

    public IMongoCollection<Subscription> Subscriptions =>
        _database.GetCollection<Subscription>("subscriptions");

    public IMongoCollection<SubscriptionPayment> SubscriptionPayments =>
        _database.GetCollection<SubscriptionPayment>("subscription_payments");

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
