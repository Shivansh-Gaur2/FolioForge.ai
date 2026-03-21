# FolioForge.Infrastructure 🔧

> **Infrastructure Layer - External Concerns Implementation**

This layer contains all implementations for external dependencies: databases, message queues, AI services, and file handling. It implements interfaces defined in Application and Domain layers.

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Database Access** | Entity Framework Core DbContext with tenant query filters |
| **Repository Implementations** | Portfolio, Tenant, and User repository implementations |
| **Multi-Tenancy** | Tenant middleware, scoped tenant context, automatic TenantId assignment |
| **Authentication** | JWT access token generation + refresh token management (HMAC-SHA256) |
| **External Services** | AI providers (Groq, OpenAI, Gemini), PDF parsing |
| **Distributed Cache** | Redis-backed `ICacheService` (cache-aside, prefix invalidation) |
| **Rate Limiting** | Redis Token Bucket algorithm — atomic Lua script, per-client, multi-instance safe |
| **Resilience** | Custom Circuit Breaker (AI service) + Bulkhead isolation (per-endpoint partitions) |
| **Message Queues** | RabbitMQ event publishing with OpenTelemetry context propagation |
| **Observability** | OpenTelemetry tracing (Jaeger) + metrics (Prometheus), smart head-based sampler |
| **Dependency Injection** | Service registration extensions |

---

## 📂 Project Structure

```
FolioForge.Infrastructure/
├── Middleware/
│   └── TenantMiddleware.cs          # Multi-tenant resolution (JWT → Header)
├── Persistence/
│   └── ApplicationDbContext.cs      # EF Core DbContext (5 DbSets, query filters)
├── Repositories/
│   ├── PortfolioRepository.cs       # IPortfolioRepository implementation
│   ├── TenantRepository.cs          # ITenantRepository (IgnoreQueryFilters)
│   └── UserRepository.cs            # IUserRepository (cross-tenant email check)
├── Services/
│   ├── GeminiAiService.cs           # Google Gemini 2.0 Flash implementation
│   ├── GroqAiService.cs             # Groq Llama 3.3-70B implementation
│   ├── JwtAuthService.cs            # JWT access token + refresh token helpers (IAuthService)
│   ├── OpenAiService.cs             # OpenAI GPT implementation
│   ├── PdfService.cs                # PDF text extraction (PdfPig)
│   ├── RedisCacheService.cs         # Redis-backed ICacheService
│   ├── ResilientAiServiceDecorator.cs # Decorator wrapping IAiService with Circuit Breaker
│   └── TenantContext.cs             # Scoped tenant context (ITenantContext)
├── Messaging/
│   └── RabbitMqEventPublisher.cs    # RabbitMQ publisher (durable, OTel context injection)
├── RateLimiting/
│   ├── RedisTokenBucketRateLimiter.cs # Atomic Lua Token Bucket
│   ├── RateLimitMiddleware.cs       # ASP.NET Core middleware integration
│   ├── RateLimitAttribute.cs        # [RateLimit("PolicyName")] attribute
│   ├── ClientIdentityResolver.cs   # Extracts client identity (user ID or IP)
│   ├── RateLimiterOptions.cs        # Per-policy configuration
│   └── RateLimitingServiceCollectionExtensions.cs
├── Resilience/
│   ├── CircuitBreaker/
│   │   ├── CircuitBreaker.cs            # State-machine circuit breaker (Closed/Open/HalfOpen)
│   │   ├── CircuitBreakerFactory.cs    # Named registry for circuit breakers
│   │   ├── CircuitBreakerOptions.cs    # Thresholds and retry window
│   │   ├── CircuitBreakerOpenException.cs
│   │   └── CircuitBreakerState.cs
│   ├── Bulkhead/
│   │   ├── BulkheadMiddleware.cs        # ASP.NET Core middleware integration
│   │   ├── BulkheadAttribute.cs        # [Bulkhead("PartitionName")] attribute
│   │   ├── BulkheadOptions.cs
│   │   └── BulkheadPartitionManager.cs  # Partition state + snapshot API
│   └── ResilienceServiceCollectionExtensions.cs
├── Telemetry/
│   ├── FolioForgeDiagnostics.cs     # Static ActivitySource + Meter + tag constants
│   ├── OpenTelemetryExtension.cs   # Tracing (Jaeger) + metrics (Prometheus) setup
│   ├── RabbitMqContextPropagator.cs # W3C TraceContext extract/inject for RabbitMQ headers
│   └── SmartSampler.cs             # Parent-based sampler that force-promotes error spans
├── Migrations/
│   └── *.cs                         # EF Core migrations
├── DependencyInjection.cs           # Service registration
└── FolioForge.Infrastructure.csproj
```

---

## 🗄️ Database Access

### ApplicationDbContext

The EF Core DbContext managing all database operations with **multi-tenant data isolation**:

```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<PortfolioSection> Sections { get; set; }
}
```

#### Tenant Query Filters

Global query filters automatically scope data to the current tenant:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Users are scoped to current tenant
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    });

    // Portfolios are scoped to current tenant
    modelBuilder.Entity<Portfolio>(entity =>
    {
        entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
        entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    });
}
```

#### Automatic TenantId Assignment

The `SaveChangesAsync` override auto-stamps `TenantId` on new entities that implement `ITenantEntity`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
    {
        if (entry.State == EntityState.Added && _tenantContext.IsResolved)
        {
            entry.Entity.TenantId = _tenantContext.TenantId;
        }
    }
    return await base.SaveChangesAsync(cancellationToken);
}
```

#### Entity Configuration Summary

| Entity | Table | Key Features |
|--------|-------|-------------|
| `Tenant` | `tenants` | Unique `Identifier` index |
| `User` | `users` | Unique `Email` index, tenant query filter |
| `Portfolio` | `portfolios` | Composite unique index `(TenantId, Slug)`, tenant query filter, Theme as JSON |
| `PortfolioSection` | `portfolio_sections` | Cascade delete from Portfolio |
| `RefreshToken` | `refresh_tokens` | Per-user opaque tokens; `IsActive` = not revoked and not expired |

**Key Configurations:**
- JSON serialization for Theme value object (stored as `nvarchar(max)`)
- Cascade delete for sections when portfolio is deleted
- Composite unique index `(TenantId, Slug)` — slug uniqueness is per-tenant
- Global query filters on `User` and `Portfolio` for tenant isolation

---

### Repositories

#### PortfolioRepository

Implementation of `IPortfolioRepository` — all queries are automatically tenant-scoped:

```csharp
public class PortfolioRepository : IPortfolioRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<Portfolio?> GetByIdAsync(Guid id)
        => await _context.Portfolios.Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Id == id);
    
    public async Task<Portfolio?> GetBySlugAsync(string slug)
        => await _context.Portfolios.Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    
    public async Task AddAsync(Portfolio portfolio)
        => await _context.Portfolios.AddAsync(portfolio);
}
```

#### TenantRepository

Uses `IgnoreQueryFilters()` since tenant lookups must work globally:

```csharp
public class TenantRepository : ITenantRepository
{
    public async Task<Tenant?> GetByIdentifierAsync(string identifier)
        => await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Identifier == identifier);

    public async Task<Tenant?> GetByIdAsync(Guid id)
        => await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);
}
```

#### UserRepository

Login and registration require cross-tenant email checks:

```csharp
public class UserRepository : IUserRepository
{
    // Cross-tenant lookup for login
    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    // Cross-tenant check for registration
    public async Task<bool> EmailExistsGloballyAsync(string email)
        => await _context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email.ToLowerInvariant());

    // Tenant-scoped lookup (uses query filter)
    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
}
```

---

## 🔐 Multi-Tenancy & Authentication

### TenantMiddleware

Resolves the current tenant for each HTTP request with a two-strategy fallback:

```
Request
  │
  ├── Path excluded? (/api/auth/*, /api/tenants/*, /swagger/*, /health)
  │     └── YES → Skip tenant resolution, pass through
  │
  ├── Strategy 1: JWT Bearer token
  │     └── Parse JWT → Extract "tenantId" claim → Lookup tenant → SetTenant()
  │
  ├── Strategy 2: X-Tenant-Id header (fallback)
  │     └── Read header → Lookup by identifier → SetTenant()
  │
  └── Neither found → Return 400 Bad Request
```

**Excluded Paths:** `/api/tenants`, `/api/auth`, `/swagger`, `/health`

### TenantContext

Scoped service (one instance per HTTP request) implementing `ITenantContext`:

```csharp
public class TenantContext : ITenantContext
{
    private Guid _tenantId;
    private string _identifier = string.Empty;

    public Guid TenantId => IsResolved ? _tenantId 
        : throw new InvalidOperationException("Tenant has not been resolved.");
    
    public string TenantIdentifier => _identifier;
    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId, string identifier)
    {
        _tenantId = tenantId;
        _identifier = identifier;
        IsResolved = true;
    }
}
```

### JwtAuthService

Generates **short-lived access tokens** (15 min) and provides helpers for refresh token management:

```csharp
public class JwtAuthService : IAuthService
{
    // Generate short-lived access token (JWT)
    public string GenerateAccessToken(Guid userId, Guid tenantId, string email, string fullName)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("fullName", fullName),
            new Claim("tenantId", tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes), // 15 min
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Generate a cryptographically random refresh token string
    public string GenerateRefreshTokenString()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    // Validate an expired access token (for the refresh flow)
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) { ... }
}
```

| JWT Claim | Value | Purpose |
|-----------|-------|---------|
| `sub` | User ID (GUID) | User identification |
| `email` | User email | Display & lookup |
| `fullName` | User name | Display |
| `tenantId` | Tenant ID (GUID) | Tenant resolution by middleware |
| `jti` | Random GUID | Token unique ID |

> Refresh tokens are stored in the `refresh_tokens` table (see `RefreshToken` entity in Domain), not inside the JWT.

## 📦 Distributed Cache (Redis)

### RedisCacheService

Implements `ICacheService` using `IDistributedCache` (standard ops) and `IConnectionMultiplexer` (advanced key-pattern scanning):

| Method | Description |
|--------|-------------|
| `GetAsync<T>` | Deserialize cached JSON; returns `default` on miss |
| `SetAsync<T>` | Serialize to JSON; default TTL 30 minutes |
| `RemoveAsync` | Delete a single key |
| `RemoveByPrefixAsync` | Scan + delete all keys matching a prefix (cache invalidation) |
| `GetOrSetAsync<T>` | Cache-aside pattern — fetch from cache or populate via factory |
| `ExistsAsync` | Check key presence |

---

## ⏱️ Rate Limiting

### RedisTokenBucketRateLimiter

All state is stored in Redis as a hash `(policyName:clientId)` and mutated by an atomic Lua script — race-condition-free across multiple API instances.

**Policies** (configured in `appsettings.json`):

| Policy | Burst | Rate | Used On |
|--------|-------|------|---------|
| `Auth` | 5 | 2/s | `AuthController` |
| `Upload` | 3 | 1/s | Upload endpoint |
| `Default` | 20 | 10/s | All other endpoints |

The `[RateLimit("PolicyName")]` attribute and `RateLimitMiddleware` wire the limiter into the ASP.NET Core pipeline. `ClientIdentityResolver` uses the authenticated `userId` as the bucket key for logged-in requests, or falls back to the client IP.

---

## 🛡️ Resilience

### Circuit Breaker

`CircuitBreaker` is a state machine with three states:

| State | Behavior |
|-------|----------|
| `Closed` | Normal operation; failures counted |
| `Open` | Fast-fail with `CircuitBreakerOpenException`; no calls to downstream |
| `HalfOpen` | Single probe request; success → Closed, failure → Open again |

`ResilientAiServiceDecorator` wraps the `IAiService` implementation using the `GroqAi` named circuit breaker. Callers that catch `CircuitBreakerOpenException` can queue for retry or return a graceful degradation response.

`ICircuitBreakerFactory` provides a registry of named breakers — inspected live via `GET /api/resilience`.

### Bulkhead

`BulkheadMiddleware` reads the `[Bulkhead("PartitionName")]` attribute and limits concurrency per partition with an optional queue. Requests exceeding the queue size receive `503 Service Unavailable`.

`BulkheadPartitionManager.GetSnapshot()` provides live utilization data (active count, queued count, utilization %) — also exposed at `GET /api/resilience`.

---

## 📊 Observability (OpenTelemetry)

### Tracing

| Source | What is traced |
|--------|----------------|
| `FolioForgeDiagnostics.ActivitySource` | Custom spans for resume processing, AI calls |
| ASP.NET Core instrumentation | All incoming HTTP requests (filtered: no `/swagger`, `/health`, `/metrics`) |
| HttpClient instrumentation | All outgoing HTTP calls (Groq, Gemini API) |
| SqlClient instrumentation | EF Core SQL queries with statement text |
| StackExchange.Redis instrumentation | Redis operations |
| RabbitMQ (manual) | Producer and consumer spans with W3C context propagation |

Spans are exported to Jaeger via OTLP gRPC (`http://localhost:4317` by default).

`SmartSampler` uses parent-based head sampling. Error spans above a configurable ratio are force-promoted to a separate OTLP exporter, ensuring errors are never dropped even at low sampling rates.

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `resume_processing_duration_ms` | Histogram | End-to-end resume processing time |
| `messages_processed_total` | Counter | RabbitMQ messages processed (tagged: `event_type`, `success`) |
| Built-in ASP.NET Core metrics | Various | Request duration, active requests |
| Built-in HttpClient metrics | Various | Outgoing request duration |

Prometheus scrapes `/metrics`. A sample `prometheus.yml` and `docker-compose.observability.yml` are provided at the repo root for local Jaeger + Prometheus + Grafana setup.

---

## 🤖 AI Services

### GroqAiService (Primary)

Uses Groq's Llama 3.3-70B for resume parsing:

```csharp
public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GroqAiService> _logger;
    
    public async Task<string> GeneratePortfolioDataAsync(string resumeText)
    {
        var url = "https://api.groq.com/openai/v1/chat/completions";
        
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = "You are a resume parser. Output ONLY valid JSON." },
                new { role = "user", content = BuildPrompt(resumeText) }
            },
            temperature = 0.1  // Low temperature for consistent JSON
        };
        
        // ... HTTP call and response parsing
        return CleanJson(textResult);
    }
    
    private string BuildPrompt(string resumeText)
    {
        return $@"
        You are a professional resume parser. Analyze the text below and extract structured data.
        
        CRITICAL RULES:
        1. For 'Experience' and 'Projects', do NOT write paragraphs.
        2. Extract distinct achievements/responsibilities as a LIST of strings called 'points'.
        3. If the resume has bullet points, preserve them.
        4. Keep descriptions professional, concise, and impact-oriented.

        REQUIRED JSON STRUCTURE:
        {{
          ""summary"": ""Professional summary string"",
          ""skills"": [""C#"", ""React"", ""Azure""],
          ""experience"": [ 
            {{ 
              ""company"": ""Company Name"", 
              ""role"": ""Job Title"", 
              ""points"": [""Achievement 1"", ""Achievement 2""] 
            }} 
          ],
          ""projects"": [ 
            {{ 
              ""name"": ""Project Name"", 
              ""techStack"": ""React, Node.js"", 
              ""points"": [""Feature 1"", ""Feature 2""] 
            }} 
          ]
        }}

        RESUME TEXT:
        {resumeText}
        ";
    }
}
```

**Why Groq?**
- Fastest inference (< 1 second for most requests)
- Free tier available
- Compatible with OpenAI API format
- Llama 3.3-70B is excellent for structured extraction

### OpenAiService & GeminiAiService

Alternative AI providers with same interface:

```csharp
// Easy to swap providers
services.AddHttpClient<IAiService, GroqAiService>();    // Current
// services.AddHttpClient<IAiService, OpenAiService>(); // Alternative
// services.AddHttpClient<IAiService, GeminiAiService>(); // Alternative
```

---

## 📄 PDF Service

Uses **PdfPig** for text extraction:

```csharp
public class PdfService : IPdfService
{
    public string ExtractText(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
        
        var sb = new StringBuilder();
        
        using (var pdf = PdfDocument.Open(filePath))
        {
            foreach (var page in pdf.GetPages())
            {
                sb.Append(page.Text);
                sb.Append(" ");
            }
        }
        
        return sb.ToString().Trim();
    }
}
```

**Why PdfPig?**
- Pure .NET, no native dependencies
- MIT licensed
- Good text extraction quality
- Handles most PDF formats

---

## 📨 Message Queue

### RabbitMqEventPublisher

Publishes events to RabbitMQ for async processing:

```csharp
public class RabbitMqEventPublisher : IEventPublisher
{
    public async Task PublishAsync<T>(T @event) where T : class
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        
        // Declare queue (idempotent)
        await channel.QueueDeclareAsync(
            queue: "resume_processing_queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        // Serialize and publish
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);
        
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "resume_processing_queue",
            body: body
        );
    }
}
```

**Message Flow:**

```
API Controller                 RabbitMQ Queue              Worker Service
     │                              │                            │
     │ PublishAsync(event) ──────► │                            │
     │                              │ ◄──── BasicConsume ───────│
     │                              │                            │
     │                              │ ─── Deliver Message ────► │
     │                              │                            │
     │ Return 202 Accepted         │                            │ Process PDF
     │                              │                            │ Call AI
     │                              │                            │ Update DB
```

---

## 🔌 Dependency Injection

### DependencyInjection.cs

Central registration of all infrastructure services:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // Multi-Tenancy
        services.AddScoped<ITenantContext, TenantContext>();
        
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
        
        // Repositories
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Authentication
        services.AddScoped<IAuthService, JwtAuthService>();
        
        // Event Publishing
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        
        // External Services
        services.AddScoped<IPdfService, PdfService>();
        services.AddHttpClient<IAiService, GroqAiService>();
        
        return services;
    }
}
```

| Registration | Interface | Implementation | Lifetime |
|---|---|---|---|
| Tenant Context | `ITenantContext` | `TenantContext` | Scoped |
| Database | `ApplicationDbContext` | DbContext | Scoped |
| DbContext Interface | `IApplicationDbContext` | Same instance | Scoped |
| Portfolio Repo | `IPortfolioRepository` | `PortfolioRepository` | Scoped |
| Tenant Repo | `ITenantRepository` | `TenantRepository` | Scoped |
| User Repo | `IUserRepository` | `UserRepository` | Scoped |
| Auth Service | `IAuthService` | `JwtAuthService` | Scoped |
| Event Publisher | `IEventPublisher` | `RabbitMqEventPublisher` | Scoped |
| PDF Service | `IPdfService` | `PdfService` | Scoped |
| AI Service | `IAiService` | `GroqAiService` | HttpClient |

---

## 🗃️ Database Migrations

```bash
# Create a new migration
cd backend/src/FolioForge.Api
dotnet ef migrations add MigrationName --project ../FolioForge.Infrastructure

# Apply migrations
dotnet ef database update
```

---

## 🔗 Dependencies

```xml
<ItemGroup>
    <!-- Project References -->
    <ProjectReference Include="..\FolioForge.Application\FolioForge.Application.csproj" />
    <ProjectReference Include="..\FolioForge.Domain\FolioForge.Domain.csproj" />
    
    <!-- Database -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.x" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.x" />
    
    <!-- Authentication -->
    <PackageReference Include="BCrypt.Net-Next" Version="4.x" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.x" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.x" />
    
    <!-- Message Queue -->
    <PackageReference Include="RabbitMQ.Client" Version="7.x" />
    
    <!-- PDF Processing -->
    <PackageReference Include="PdfPig" Version="0.x" />
    
    <!-- AI / HTTP Client -->
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.x" />
    <PackageReference Include="OpenAI" Version="2.x" />
</ItemGroup>
```

---

## 📚 Related Documentation

- [FolioForge.Api README](../FolioForge.Api/README.md) - HTTP entry point
- [FolioForge.Application README](../FolioForge.Application/README.md) - Interface definitions
- [FolioForge.Domain README](../FolioForge.Domain/README.md) - Entity definitions
- [FolioForge.Worker README](../FolioForge.Worker/README.md) - Message consumer
