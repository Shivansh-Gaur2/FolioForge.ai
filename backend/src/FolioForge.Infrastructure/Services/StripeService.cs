using FolioForge.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FolioForge.Infrastructure.Services;

/// <summary>
/// Razorpay integration via direct REST API calls.
/// No third-party SDK needed — Razorpay's API uses Basic Auth + JSON.
/// </summary>
public class RazorpayService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;
    private readonly string _webhookSecret;
    private readonly ILogger<RazorpayService> _logger;

    private const string RazorpayBaseUrl = "https://api.razorpay.com/v1/";

    public RazorpayService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<RazorpayService> logger)
    {
        _keyId = configuration["Razorpay:KeyId"]
            ?? throw new InvalidOperationException("Razorpay:KeyId configuration is required.");
        _keySecret = configuration["Razorpay:KeySecret"]
            ?? throw new InvalidOperationException("Razorpay:KeySecret configuration is required.");
        _webhookSecret = configuration["Razorpay:WebhookSecret"]
            ?? throw new InvalidOperationException("Razorpay:WebhookSecret configuration is required.");
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient("Razorpay");
        _httpClient.BaseAddress = new Uri(RazorpayBaseUrl);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<PaymentSubscriptionResult> CreateSubscriptionAsync(
        Guid userId, string userEmail, string razorpayPlanId)
    {
        // 1. Create or find customer
        var customerId = await CreateCustomerAsync(userEmail, userId.ToString());

        // 2. Create subscription
        var payload = new
        {
            plan_id = razorpayPlanId,
            customer_id = customerId,
            total_count = 120, // Max billing cycles (10 years monthly)
            quantity = 1,
            notes = new { userId = userId.ToString() }
        };

        var response = await PostAsync("subscriptions", payload);
        var subscriptionId = response.GetProperty("id").GetString()!;

        _logger.LogInformation(
            "Created Razorpay subscription {SubscriptionId} for user {UserId}",
            subscriptionId, userId);

        return new PaymentSubscriptionResult(subscriptionId, customerId, _keyId);
    }

    public bool VerifyPaymentSignature(
        string razorpayPaymentId, string razorpaySubscriptionId, string razorpaySignature)
    {
        // Razorpay signature = HMAC-SHA256(razorpay_payment_id + "|" + razorpay_subscription_id, key_secret)
        var payload = $"{razorpayPaymentId}|{razorpaySubscriptionId}";
        var expectedSignature = ComputeHmacSha256(payload, _keySecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(razorpaySignature));
    }

    public RazorpayWebhookEvent? VerifyAndParseWebhook(string payload, string signatureHeader)
    {
        var expectedSignature = ComputeHmacSha256(payload, _webhookSecret);

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signatureHeader)))
        {
            _logger.LogWarning("Razorpay webhook signature verification failed");
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var eventType = root.GetProperty("event").GetString() ?? "";
            var payloadData = root.GetProperty("payload");

            string? subscriptionId = null;
            string? paymentId = null;
            string? customerId = null;
            string? status = null;
            string? planId = null;

            if (payloadData.TryGetProperty("subscription", out var subProp) &&
                subProp.TryGetProperty("entity", out var subEntity))
            {
                subscriptionId = subEntity.TryGetProperty("id", out var id) ? id.GetString() : null;
                customerId = subEntity.TryGetProperty("customer_id", out var cid) ? cid.GetString() : null;
                status = subEntity.TryGetProperty("status", out var s) ? s.GetString() : null;
                planId = subEntity.TryGetProperty("plan_id", out var pid) ? pid.GetString() : null;
            }

            if (payloadData.TryGetProperty("payment", out var payProp) &&
                payProp.TryGetProperty("entity", out var payEntity))
            {
                paymentId = payEntity.TryGetProperty("id", out var pid) ? pid.GetString() : null;
            }

            return new RazorpayWebhookEvent(eventType, subscriptionId, paymentId, customerId, status, planId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Razorpay webhook payload");
            return null;
        }
    }

    // ─────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────

    private async Task<string> CreateCustomerAsync(string email, string userId)
    {
        var payload = new
        {
            email,
            notes = new { userId }
        };

        var response = await PostAsync("customers", payload);
        return response.GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> PostAsync(string path, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(path, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay API error: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Razorpay API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.Clone();
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }
}
