namespace FolioForge.Application.Common.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Create a Razorpay Subscription for a user to subscribe to a plan.
    /// Returns subscription ID and other details needed by the frontend checkout modal.
    /// </summary>
    Task<PaymentSubscriptionResult> CreateSubscriptionAsync(Guid userId, string userEmail, string razorpayPlanId);

    /// <summary>
    /// Verify a Razorpay payment signature after the frontend checkout completes.
    /// Returns true if the signature is valid.
    /// </summary>
    bool VerifyPaymentSignature(string razorpayPaymentId, string razorpaySubscriptionId, string razorpaySignature);

    /// <summary>
    /// Verify a Razorpay webhook signature.
    /// Returns the parsed event, or null if verification fails.
    /// </summary>
    RazorpayWebhookEvent? VerifyAndParseWebhook(string payload, string signatureHeader);
}

public record PaymentSubscriptionResult(
    string SubscriptionId,
    string CustomerId,
    string KeyId   // Razorpay key_id for the frontend
);

/// <summary>
/// Simplified webhook event from Razorpay.
/// </summary>
public record RazorpayWebhookEvent(
    string EventType,
    string? SubscriptionId,
    string? PaymentId,
    string? CustomerId,
    string? Status,
    string? PlanId
);
