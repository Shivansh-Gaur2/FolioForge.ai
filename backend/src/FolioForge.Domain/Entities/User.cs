using FolioForge.Domain.Interfaces;

namespace FolioForge.Domain.Entities
{
    /// <summary>
    /// A user that belongs to a specific tenant.
    /// Passwords are stored as BCrypt hashes — never plain text.
    /// </summary>
    public class User : BaseEntity, ITenantEntity
    {
        public string Email { get; private set; } = default!;
        public string FullName { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public Guid TenantId { get; set; }

        // ── Billing ──────────────────────────────────────
        public Guid PlanId { get; private set; }
        public string? StripeCustomerId { get; private set; }
        public string? StripeSubscriptionId { get; private set; }
        public string SubscriptionStatus { get; private set; } = "active"; // active, past_due, canceled, trialing
        public int AiParsesUsedThisMonth { get; private set; }
        public DateTime AiParsesResetAt { get; private set; } = DateTime.UtcNow;

        private User() { }

        public User(string email, string fullName, string passwordHash, Guid tenantId)
        {
            Email = email.ToLowerInvariant();
            FullName = fullName;
            PasswordHash = passwordHash;
            TenantId = tenantId;
        }

        public void UpdateProfile(string fullName)
        {
            FullName = fullName;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPlan(Guid planId)
        {
            PlanId = planId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetStripeCustomer(string stripeCustomerId)
        {
            StripeCustomerId = stripeCustomerId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateSubscription(string subscriptionId, string status, Guid planId)
        {
            StripeSubscriptionId = subscriptionId;
            SubscriptionStatus = status;
            PlanId = planId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void CancelSubscription()
        {
            StripeSubscriptionId = null;
            SubscriptionStatus = "canceled";
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Increment AI parse counter. Resets monthly.
        /// </summary>
        public int IncrementAiParses()
        {
            // Reset counter if we've crossed into a new month
            if (DateTime.UtcNow >= AiParsesResetAt)
            {
                AiParsesUsedThisMonth = 0;
                AiParsesResetAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(1);
            }

            AiParsesUsedThisMonth++;
            UpdatedAt = DateTime.UtcNow;
            return AiParsesUsedThisMonth;
        }
    }
}
