using FolioForge.Application.Common.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FolioForge.Infrastructure.RateLimiting;

/// <summary>
/// Extension methods to register the distributed rate limiter into the DI container.
/// Keeps Program.cs clean and follows the same pattern as AddInfrastructure().
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the distributed Token Bucket rate limiter with Redis backing store.
    /// 
    /// Call this AFTER AddInfrastructure() (which registers IConnectionMultiplexer).
    /// </summary>
    public static IServiceCollection AddDistributedRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration from appsettings "RateLimiting" section
        services.Configure<RateLimiterOptions>(
            configuration.GetSection(RateLimiterOptions.SectionName));

        // Ensure default policies exist even if config section is missing
        services.PostConfigure<RateLimiterOptions>(options =>
        {
            if (!options.Policies.ContainsKey("Default"))
            {
                options.Policies["Default"] = new PolicyOptions
                {
                    BucketCapacity = 20,
                    RefillRate = 10,
                    RefillIntervalSeconds = 1.0
                };
            }

            if (!options.Policies.ContainsKey("Auth"))
            {
                options.Policies["Auth"] = new PolicyOptions
                {
                    BucketCapacity = 5,
                    RefillRate = 2,
                    RefillIntervalSeconds = 1.0
                };
            }
        });

        // Register rate limiter as singleton (stateless — all state lives in Redis)
        services.AddSingleton<IRateLimiter, RedisTokenBucketRateLimiter>();

        // Register identity resolver as singleton (stateless)
        services.AddSingleton<IClientIdentityResolver, ClientIdentityResolver>();

        return services;
    }
}
