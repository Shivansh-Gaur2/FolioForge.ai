using FolioForge.Application.Commands.CreatePortfolio;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Messaging;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.Repositories;
using FolioForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FolioForge.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

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
            services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddHttpClient<IAiService, GroqAiService>(); 
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
