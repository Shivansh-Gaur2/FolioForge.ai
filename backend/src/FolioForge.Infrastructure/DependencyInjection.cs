using FolioForge.Application.Commands.CreatePortfolio;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Messaging;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.RateLimiting;
using FolioForge.Infrastructure.Repositories;
using FolioForge.Infrastructure.Resilience;
using FolioForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Reflection;

namespace FolioForge.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // ==============================================================
            // REDIS DISTRIBUTED CACHE
            // ==============================================================
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis configuration is required.");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "FolioForge:";
            });

            // Register IConnectionMultiplexer as singleton for advanced Redis ops (e.g., key scanning)
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddSingleton<ICacheService, RedisCacheService>();

            // Register tenant context as scoped (one per request)
            services.AddScoped<ITenantContext, TenantContext>();

            // Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register IApplicationDbContext as the same instance as ApplicationDbContext
            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<IAuthService, JwtAuthService>();
            services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddHttpClient("Razorpay");
            services.AddScoped<IPaymentService, RazorpayService>();
            services.AddHttpClient<IAiService, GroqAiService>();

            // Distributed rate limiting (Token Bucket via Redis)
            services.AddDistributedRateLimiting(configuration);

            // Resilience patterns (Bulkhead + Circuit Breaker)
            // Must be called AFTER service registrations above so decorators can wrap them
            services.AddResiliencePatterns(configuration);

            return services;
        }
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Scans this assembly and registers all Commands/Queries automatically
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePortfolioCommand).Assembly));

            return services;
        }
    }
}
