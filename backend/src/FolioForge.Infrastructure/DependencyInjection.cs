using FolioForge.Application.Commands.CreatePortfolio;
using FolioForge.Domain.Interfaces;
using FolioForge.Infrastructure.Messaging;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.Repositories;
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
            // 1. Add this using
            // 2. Change the logic
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
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
