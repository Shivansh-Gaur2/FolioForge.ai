namespace FolioForge.Domain.Entities;

/// <summary>
/// Represents a subscription plan (Free, Pro, etc.).
/// Plans are seeded — not user-created.
/// </summary>
public class Plan : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;

    // Pricing (in cents to avoid floating-point issues)
    public int PriceMonthlyInCents { get; private set; }
    public int PriceYearlyInCents { get; private set; }

    // Limits
    public int MaxPortfolios { get; private set; }
    public int MaxAiParsesPerMonth { get; private set; }

    // Feature flags
    public bool CustomDomain { get; private set; }
    public bool RemoveWatermark { get; private set; }
    public bool PasswordProtection { get; private set; }
    public bool Analytics { get; private set; }

    // Stripe price IDs (configured per environment)
    public string? StripePriceMonthlyId { get; private set; }
    public string? StripePriceYearlyId { get; private set; }

    private Plan() { }

    public Plan(
        string name, string slug,
        int priceMonthlyInCents, int priceYearlyInCents,
        int maxPortfolios, int maxAiParsesPerMonth,
        bool customDomain, bool removeWatermark,
        bool passwordProtection, bool analytics)
    {
        Name = name;
        Slug = slug;
        PriceMonthlyInCents = priceMonthlyInCents;
        PriceYearlyInCents = priceYearlyInCents;
        MaxPortfolios = maxPortfolios;
        MaxAiParsesPerMonth = maxAiParsesPerMonth;
        CustomDomain = customDomain;
        RemoveWatermark = removeWatermark;
        PasswordProtection = passwordProtection;
        Analytics = analytics;
    }

    public void SetStripePriceIds(string? monthlyId, string? yearlyId)
    {
        StripePriceMonthlyId = monthlyId;
        StripePriceYearlyId = yearlyId;
        UpdatedAt = DateTime.UtcNow;
    }
}
