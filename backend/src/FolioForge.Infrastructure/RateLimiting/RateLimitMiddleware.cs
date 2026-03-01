using FolioForge.Application.Common.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// ASP.NET Core middleware that enforces distributed rate limiting on every request.
///
/// Design decisions:
/// ─────────────────
/// 1. Placed AFTER authentication (so we have the user identity) but BEFORE
///    controller execution (so we short-circuit before doing real work).
///
/// 2. Uses the [RateLimit] attribute to determine which policy applies.
///    No attribute → "Default" policy. [RateLimit(Disabled = true)] → skip.
///
/// 3. Emits standard rate-limit headers on EVERY response (configurable):
///    • RateLimit-Limit     — bucket capacity
///    • RateLimit-Remaining — tokens left
///    • Retry-After         — seconds until a token is available (on 429 only)
///
/// 4. Returns RFC 7231 compliant 429 Too Many Requests with a JSON body
///    that includes retry timing for programmatic clients.
///
/// 5. FAIL-OPEN: If the rate limiter throws (Redis down), the request is allowed.
///    Availability > rate limiting. Ops is alerted via Error-level logging.
/// </summary>
public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly IClientIdentityResolver _identityResolver;
    private readonly IOptionsMonitor<RateLimiterOptions> _optionsMonitor;
    private readonly ILogger<RateLimitMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Paths that are always exempt from rate limiting.
    /// Health checks and metrics endpoints must never be throttled.
    /// </summary>
    private static readonly string[] AlwaysExcludedPaths =
    [
        "/health",
        "/healthz",
        "/metrics",
        "/swagger"
    ];

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        IClientIdentityResolver identityResolver,
        IOptionsMonitor<RateLimiterOptions> optionsMonitor,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _identityResolver = identityResolver;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _optionsMonitor.CurrentValue;

        // Master switch
        if (!options.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip infrastructure endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Check whitelist
        var clientId = _identityResolver.Resolve(context);
        if (IsWhitelisted(clientId, options))
        {
            await _next(context);
            return;
        }

        // Determine which policy applies to this endpoint
        var policyName = ResolvePolicyName(context);
        if (policyName is null)
        {
            // [RateLimit(Disabled = true)] on this endpoint
            await _next(context);
            return;
        }

        // Execute the rate limit check
        var result = await _rateLimiter.TryAcquireAsync(policyName, clientId, context.RequestAborted);

        // Attach rate-limit headers (helps clients self-regulate)
        if (options.IncludeHeadersOnSuccess || !result.IsAllowed)
        {
            SetRateLimitHeaders(context.Response, result);
        }

        if (result.IsAllowed)
        {
            await _next(context);
            return;
        }

        // 429 Too Many Requests
        await WriteThrottledResponse(context, result, policyName, clientId);
    }

    /// <summary>
    /// Resolves the rate-limit policy name from endpoint metadata.
    /// Returns null if rate limiting is explicitly disabled for this endpoint.
    /// </summary>
    private static string? ResolvePolicyName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return "Default";
        }

        // Check for [RateLimit] attribute on the action method first, then on the controller
        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor is not null)
        {
            // Action-level attribute takes priority
            var actionAttr = actionDescriptor.MethodInfo.GetCustomAttribute<RateLimitAttribute>();
            if (actionAttr is not null)
            {
                return actionAttr.Disabled ? null : actionAttr.PolicyName;
            }

            // Controller-level attribute
            var controllerAttr = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<RateLimitAttribute>();
            if (controllerAttr is not null)
            {
                return controllerAttr.Disabled ? null : controllerAttr.PolicyName;
            }
        }

        // No attribute → use Default policy
        return "Default";
    }

    private static bool IsExcludedPath(string path)
    {
        foreach (var excluded in AlwaysExcludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsWhitelisted(string clientId, RateLimiterOptions options)
    {
        foreach (var entry in options.Whitelist)
        {
            if (clientId.Contains(entry, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static void SetRateLimitHeaders(HttpResponse response, RateLimitResult result)
    {
        response.Headers["RateLimit-Limit"] = result.Limit.ToString();
        response.Headers["RateLimit-Remaining"] = result.Remaining.ToString();

        if (!result.IsAllowed)
        {
            response.Headers["Retry-After"] = Math.Ceiling(result.RetryAfterSeconds).ToString();
        }
    }

    private async Task WriteThrottledResponse(
        HttpContext context,
        RateLimitResult result,
        string policyName,
        string clientId)
    {
        _logger.LogWarning(
            "Request throttled: {Method} {Path} | Client: {ClientId} | Policy: {PolicyName} | Retry-After: {RetryAfter:F1}s",
            context.Request.Method,
            context.Request.Path,
            clientId,
            policyName,
            result.RetryAfterSeconds);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var body = new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please retry after the duration specified in the Retry-After header.",
            retryAfterSeconds = Math.Ceiling(result.RetryAfterSeconds),
            limit = result.Limit,
            remaining = result.Remaining
        };

        await context.Response.WriteAsJsonAsync(body, JsonOptions, context.RequestAborted);
    }
}
