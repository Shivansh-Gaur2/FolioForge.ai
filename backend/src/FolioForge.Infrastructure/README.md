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
| **Authentication** | JWT token generation (HMAC-SHA256) |
| **External Services** | AI providers (Groq, OpenAI, Gemini), PDF parsing |
| **Message Queues** | RabbitMQ event publishing |
| **Dependency Injection** | Service registration extensions |

---

## 📂 Project Structure

```
FolioForge.Infrastructure/
├── Middleware/
│   └── TenantMiddleware.cs          # Multi-tenant resolution (JWT → Header)
├── Persistence/
│   └── ApplicationDbContext.cs      # EF Core DbContext (4 DbSets, query filters)
├── Repositories/
│   ├── PortfolioRepository.cs       # IPortfolioRepository implementation
│   ├── TenantRepository.cs          # ITenantRepository (IgnoreQueryFilters)
│   └── UserRepository.cs            # IUserRepository (cross-tenant email check)
├── Services/
│   ├── GeminiAiService.cs           # Google Gemini 2.0 Flash implementation
│   ├── GroqAiService.cs             # Groq Llama 3.3-70B implementation
│   ├── JwtAuthService.cs            # JWT token generation (IAuthService)
│   ├── OpenAiService.cs             # OpenAI GPT implementation
│   ├── PdfService.cs                # PDF text extraction (PdfPig)
│   └── TenantContext.cs             # Scoped tenant context (ITenantContext)
├── Messaging/
│   └── RabbitMqEventPublisher.cs    # RabbitMQ event publisher
├── Migrations/
│   └── *.cs                         # EF Core migrations
├── DependencyInjection.cs           # Service registration (10 services)
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

Generates JWT tokens with user and tenant claims using HMAC-SHA256:

```csharp
public class JwtAuthService : IAuthService
{
    public string GenerateToken(Guid userId, Guid tenantId, string email, string fullName)
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
            issuer: _issuer,       // "FolioForge"
            audience: _audience,   // "FolioForge.Client"
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes), // 1440 = 24h
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

| JWT Claim | Value | Purpose |
|-----------|-------|---------|
| `sub` | User ID (GUID) | User identification |
| `email` | User email | Display & lookup |
| `fullName` | User name | Display |
| `tenantId` | Tenant ID (GUID) | Tenant resolution by middleware |
| `jti` | Random GUID | Token unique ID |

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
