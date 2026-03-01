using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Messaging;
using FolioForge.Infrastructure.Resilience.Bulkhead;
using FolioForge.Infrastructure.Resilience.CircuitBreaker;
using FolioForge.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FolioForge.Infrastructure.Resilience;

/// <summary>
/// Extension methods to register all resilience infrastructure (Bulkhead + Circuit Breaker).
///
/// Registration order matters:
///   1. Configuration binding (options)
///   2. Core services (factory, partition manager)
///   3. Decorator services (wrap existing implementations)
///
/// Call this AFTER AddInfrastructure() so the inner services are already registered.
/// The decorators will replace the interface registrations while keeping the
/// concrete (inner) implementations available via their concrete types.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddResiliencePatterns(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ══════════════════════════════════════════════════════════
        // BULKHEAD CONFIGURATION
        // ══════════════════════════════════════════════════════════
        services.Configure<BulkheadOptions>(
            configuration.GetSection(BulkheadOptions.SectionName));

        services.PostConfigure<BulkheadOptions>(options =>
        {
            // Ensure sensible defaults for known partitions
            if (!options.Partitions.ContainsKey("Default"))
            {
                options.Partitions["Default"] = new PartitionOptions
                {
                    MaxConcurrency = 50,
                    MaxQueueSize = 100,
                    QueueTimeoutMs = 5000
                };
            }

            if (!options.Partitions.ContainsKey("Upload"))
            {
                options.Partitions["Upload"] = new PartitionOptions
                {
                    MaxConcurrency = 5,
                    MaxQueueSize = 10,
                    QueueTimeoutMs = 10000
                };
            }
        });

        services.AddSingleton<BulkheadPartitionManager>();

        // ══════════════════════════════════════════════════════════
        // CIRCUIT BREAKER CONFIGURATION
        // ══════════════════════════════════════════════════════════
        services.Configure<CircuitBreakerOptions>(
            configuration.GetSection(CircuitBreakerOptions.SectionName));

        services.PostConfigure<CircuitBreakerOptions>(options =>
        {
            if (!options.Breakers.ContainsKey("GroqAi"))
            {
                options.Breakers["GroqAi"] = new BreakerConfig
                {
                    FailureThreshold = 3,
                    OpenDurationSeconds = 30,
                    HalfOpenMaxAttempts = 2,
                    SuccessThresholdInHalfOpen = 2,
                    HandledExceptionTypes = ["HttpRequestException", "TaskCanceledException"]
                };
            }

            if (!options.Breakers.ContainsKey("RabbitMq"))
            {
                options.Breakers["RabbitMq"] = new BreakerConfig
                {
                    FailureThreshold = 5,
                    OpenDurationSeconds = 15,
                    HalfOpenMaxAttempts = 1,
                    SuccessThresholdInHalfOpen = 1
                };
            }
        });

        services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();

        // ══════════════════════════════════════════════════════════
        // SERVICE DECORATORS (Circuit Breaker wrapping)
        // ══════════════════════════════════════════════════════════
        // 
        // Strategy: We use the "Decorate" pattern manually.
        // 1. Remove the existing IAiService registration
        // 2. Register the concrete inner service directly
        // 3. Register the decorator as the IAiService implementation
        //
        // This keeps existing service code UNTOUCHED — Single Responsibility.

        DecorateAiService(services);
        DecorateEventPublisher(services);

        return services;
    }

    /// <summary>
    /// Replaces the IAiService registration with a circuit-breaker-protected decorator.
    /// The inner GroqAiService is resolved via HttpClientFactory (already registered).
    /// </summary>
    private static void DecorateAiService(IServiceCollection services)
    {
        // Find and remove the existing IAiService registration (if any)
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAiService));
        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        // Re-register GroqAiService as itself (typed HttpClient is already wired by AddHttpClient)
        // The HttpClientFactory still creates GroqAiService instances correctly.
        services.AddHttpClient<GroqAiService>();

        // Register the decorator as the IAiService implementation
        services.AddScoped<IAiService>(sp =>
        {
            var inner = sp.GetRequiredService<GroqAiService>();
            var factory = sp.GetRequiredService<ICircuitBreakerFactory>();
            var logger = sp.GetRequiredService<ILogger<ResilientAiServiceDecorator>>();
            return new ResilientAiServiceDecorator(inner, factory, logger);
        });
    }

    /// <summary>
    /// Replaces the IEventPublisher registration with a circuit-breaker-protected decorator.
    /// </summary>
    private static void DecorateEventPublisher(IServiceCollection services)
    {
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEventPublisher));
        if (existingDescriptor is not null)
        {
            services.Remove(existingDescriptor);
        }

        services.AddScoped<RabbitMqEventPublisher>();

        services.AddScoped<IEventPublisher>(sp =>
        {
            var inner = sp.GetRequiredService<RabbitMqEventPublisher>();
            var factory = sp.GetRequiredService<ICircuitBreakerFactory>();
            var logger = sp.GetRequiredService<ILogger<ResilientEventPublisherDecorator>>();
            return new ResilientEventPublisherDecorator(inner, factory, logger);
        });
    }
}
