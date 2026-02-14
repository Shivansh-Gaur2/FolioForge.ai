using FolioForge.Infrastructure;
using FolioForge.Worker;

var builder = Host.CreateApplicationBuilder(args);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // 1. REGISTER INFRASTRUCTURE (Database, Redis, etc.)
        // This line was missing! It loads DbContext, RabbitMQ, etc.
        services.AddInfrastructure(hostContext.Configuration);

        // 2. Register the Worker Service itself
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
builder.Services.AddHostedService<Worker>();
