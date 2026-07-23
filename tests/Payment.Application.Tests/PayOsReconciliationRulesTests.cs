using Payment.Application.Common;
using Payment.Domain.Enums;
using Xunit;

namespace Payment.Application.Tests;

public class PayOsReconciliationRulesTests
{
    [Fact]
    public void Pending_with_matching_amount_can_be_applied()
    {
        var decision = PayOsReconciliationRules.EvaluatePaid(
            PaymentStatus.Pending,
            15_000m,
            15_000);

        Assert.Equal(PayOsPaidReconciliationDecision.Apply, decision);
    }

    [Fact]
    public void Paid_with_matching_amount_is_idempotent()
    {
        var decision = PayOsReconciliationRules.EvaluatePaid(
            PaymentStatus.Paid,
            15_000m,
            15_000);

        Assert.Equal(PayOsPaidReconciliationDecision.AlreadyApplied, decision);
    }

    [Fact]
    public void Mismatched_amount_is_rejected()
    {
        var decision = PayOsReconciliationRules.EvaluatePaid(
            PaymentStatus.Pending,
            15_000m,
            14_999);

        Assert.Equal(PayOsPaidReconciliationDecision.RejectAmount, decision);
    }

    [Theory]
    [InlineData(PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public void Terminal_status_is_never_overwritten(PaymentStatus status)
    {
        var decision = PayOsReconciliationRules.EvaluatePaid(status, 15_000m, 15_000);

        Assert.Equal(PayOsPaidReconciliationDecision.RejectStatus, decision);
    }
}
