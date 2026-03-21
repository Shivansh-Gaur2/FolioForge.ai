using FolioForge.Infrastructure;
using FolioForge.Infrastructure.Telemetry;
using FolioForge.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // 1. REGISTER INFRASTRUCTURE (Database, Redis, etc.)
        services.AddInfrastructure(hostContext.Configuration);

        // 2. Observability: tracing & metrics
        services.AddFolioForgeOpenTelemetry(hostContext.Configuration);

        // 3. Register the Worker Service itself
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
