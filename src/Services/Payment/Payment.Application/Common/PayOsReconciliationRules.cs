using Payment.Domain.Enums;

namespace Payment.Application.Common;

public enum PayOsPaidReconciliationDecision
{
    Apply,
    AlreadyApplied,
    RejectAmount,
    RejectStatus
}

public static class PayOsReconciliationRules
{
    public static PayOsPaidReconciliationDecision EvaluatePaid(
        PaymentStatus currentStatus,
        decimal expectedAmount,
        long actualAmount)
    {
        var expectedVnd = (long)Math.Round(expectedAmount, MidpointRounding.AwayFromZero);
        if (actualAmount != expectedVnd)
            return PayOsPaidReconciliationDecision.RejectAmount;

        return currentStatus switch
        {
            PaymentStatus.Pending => PayOsPaidReconciliationDecision.Apply,
            PaymentStatus.Paid => PayOsPaidReconciliationDecision.AlreadyApplied,
            _ => PayOsPaidReconciliationDecision.RejectStatus
        };
    }
}
