using FolioForge.Application.Common.Interfaces;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FolioForge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IPlanRepository _planRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentService _paymentService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public BillingController(
        IPlanRepository planRepository,
        IUserRepository userRepository,
        IPaymentService paymentService,
        ApplicationDbContext dbContext,
        IConfiguration configuration)
    {
        _planRepository = planRepository;
        _userRepository = userRepository;
        _paymentService = paymentService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(sub!);
    }

    /// <summary>
    /// List all available plans with features & pricing.
    /// GET /api/billing/plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _planRepository.GetAllAsync();
        var razorpayKeyId = _configuration["Razorpay:KeyId"] ?? "";

        return Ok(new
        {
            plans = plans.Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.PriceMonthlyInCents,
                p.PriceYearlyInCents,
                p.MaxPortfolios,
                p.MaxAiParsesPerMonth,
                p.CustomDomain,
                p.RemoveWatermark,
                p.PasswordProtection,
                p.Analytics,
            }),
            razorpayKeyId,
        });
    }

    /// <summary>
    /// Get the current user's subscription status.
    /// GET /api/billing/status
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var user = await _userRepository.GetByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var plan = await _planRepository.GetByIdAsync(user.PlanId);

        return Ok(new BillingStatusResponse
        {
            PlanId = user.PlanId,
            PlanName = plan?.Name ?? "Free",
            PlanSlug = plan?.Slug ?? "free",
            SubscriptionStatus = user.SubscriptionStatus,
            AiParsesUsedThisMonth = user.AiParsesUsedThisMonth,
            MaxAiParsesPerMonth = plan?.MaxAiParsesPerMonth ?? 1,
            MaxPortfolios = plan?.MaxPortfolios ?? 1,
            HasSubscription = !string.IsNullOrEmpty(user.StripeSubscriptionId),
            Features = new PlanFeaturesResponse
            {
                CustomDomain = plan?.CustomDomain ?? false,
                RemoveWatermark = plan?.RemoveWatermark ?? false,
                PasswordProtection = plan?.PasswordProtection ?? false,
                Analytics = plan?.Analytics ?? false,
            }
        });
    }

    /// <summary>
    /// Create a Razorpay Subscription for the frontend checkout modal.
    /// POST /api/billing/create-subscription
    /// Returns { subscriptionId, customerId, razorpayKeyId } for the Razorpay checkout.js modal.
    /// </summary>
    [HttpPost("create-subscription")]
    [Authorize]
    [RateLimit("Auth")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var user = await _userRepository.GetByIdAsync(GetUserId());
        if (user == null) return NotFound();

        var plan = await _planRepository.GetByIdAsync(request.PlanId);
        if (plan == null) return BadRequest(new { error = "Invalid plan." });

        if (plan.PriceMonthlyInCents == 0)
            return BadRequest(new { error = "Cannot subscribe to a free plan." });

        // Pick the Razorpay plan ID based on billing interval
        var razorpayPlanId = request.BillingInterval == "yearly"
            ? plan.StripePriceYearlyId   // Reusing column — stores Razorpay plan_id
            : plan.StripePriceMonthlyId;

        if (string.IsNullOrEmpty(razorpayPlanId))
            return BadRequest(new { error = "Razorpay plan is not configured for this tier." });

        var result = await _paymentService.CreateSubscriptionAsync(
            user.Id, user.Email, razorpayPlanId);

        // Store the Razorpay customer ID on the user
        if (string.IsNullOrEmpty(user.StripeCustomerId))
        {
            user.SetStripeCustomer(result.CustomerId);
            await _userRepository.SaveChangesAsync();
        }

        return Ok(new
        {
            subscriptionId = result.SubscriptionId,
            razorpayKeyId = result.KeyId,
            userEmail = user.Email,
            userName = user.FullName,
        });
    }

    /// <summary>
    /// Verify a Razorpay payment after the checkout modal completes.
    /// POST /api/billing/verify-payment
    /// The frontend sends the Razorpay callback values for server-side verification.
    /// </summary>
    [HttpPost("verify-payment")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        var isValid = _paymentService.VerifyPaymentSignature(
            request.RazorpayPaymentId,
            request.RazorpaySubscriptionId,
            request.RazorpaySignature);

        if (!isValid)
            return BadRequest(new { error = "Payment verification failed. Invalid signature." });

        var user = await _userRepository.GetByIdAsync(GetUserId());
        if (user == null) return NotFound();

        // Determine plan from the request
        var plan = await _planRepository.GetByIdAsync(request.PlanId);
        var planId = plan?.Id ?? ApplicationDbContext.ProPlanId;

        user.UpdateSubscription(request.RazorpaySubscriptionId, "active", planId);
        await _userRepository.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment verified. Plan upgraded!",
            planSlug = plan?.Slug ?? "pro",
            planName = plan?.Name ?? "Pro",
        });
    }

    /// <summary>
    /// Razorpay webhook handler. Processes subscription lifecycle events.
    /// POST /api/billing/webhook
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
            return BadRequest();

        var webhookEvent = _paymentService.VerifyAndParseWebhook(payload, signature);
        if (webhookEvent == null)
            return BadRequest();

        switch (webhookEvent.EventType)
        {
            case "subscription.activated":
                await HandleSubscriptionActivated(webhookEvent);
                break;

            case "subscription.charged":
                // Recurring payment succeeded — keep plan active
                await HandleSubscriptionCharged(webhookEvent);
                break;

            case "subscription.cancelled":
            case "subscription.completed":
                await HandleSubscriptionCancelled(webhookEvent);
                break;

            case "subscription.halted":
                // Payment failed multiple times — downgrade
                await HandleSubscriptionCancelled(webhookEvent);
                break;
        }

        return Ok();
    }

    // ─────────────────────────────────────────────────────
    // Webhook Handlers
    // ─────────────────────────────────────────────────────

    private async Task HandleSubscriptionActivated(RazorpayWebhookEvent evt)
    {
        if (string.IsNullOrEmpty(evt.CustomerId)) return;

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StripeCustomerId == evt.CustomerId);

        if (user == null) return;

        // Determine plan from Razorpay plan ID
        var planId = ApplicationDbContext.ProPlanId;
        if (!string.IsNullOrEmpty(evt.PlanId))
        {
            var matchingPlan = await _dbContext.Plans
                .FirstOrDefaultAsync(p =>
                    p.StripePriceMonthlyId == evt.PlanId ||
                    p.StripePriceYearlyId == evt.PlanId);
            if (matchingPlan != null) planId = matchingPlan.Id;
        }

        user.UpdateSubscription(
            evt.SubscriptionId ?? "",
            "active",
            planId);

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleSubscriptionCharged(RazorpayWebhookEvent evt)
    {
        if (string.IsNullOrEmpty(evt.CustomerId)) return;

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StripeCustomerId == evt.CustomerId);

        if (user == null) return;

        // Ensure status stays active on successful charge
        if (user.SubscriptionStatus != "active")
        {
            user.UpdateSubscription(
                evt.SubscriptionId ?? user.StripeSubscriptionId ?? "",
                "active",
                user.PlanId);
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionCancelled(RazorpayWebhookEvent evt)
    {
        if (string.IsNullOrEmpty(evt.CustomerId)) return;

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.StripeCustomerId == evt.CustomerId);

        if (user == null) return;

        user.CancelSubscription();
        user.SetPlan(ApplicationDbContext.FreePlanId);
        await _dbContext.SaveChangesAsync();
    }
}

// ── Request / Response DTOs ──

public record CreateSubscriptionRequest(
    Guid PlanId,
    string BillingInterval // "monthly" or "yearly"
);

public record VerifyPaymentRequest(
    string RazorpayPaymentId,
    string RazorpaySubscriptionId,
    string RazorpaySignature,
    Guid PlanId
);

public class BillingStatusResponse
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = default!;
    public string PlanSlug { get; set; } = default!;
    public string SubscriptionStatus { get; set; } = default!;
    public int AiParsesUsedThisMonth { get; set; }
    public int MaxAiParsesPerMonth { get; set; }
    public int MaxPortfolios { get; set; }
    public bool HasSubscription { get; set; }
    public PlanFeaturesResponse Features { get; set; } = default!;
}

public class PlanFeaturesResponse
{
    public bool CustomDomain { get; set; }
    public bool RemoveWatermark { get; set; }
    public bool PasswordProtection { get; set; }
    public bool Analytics { get; set; }
}
