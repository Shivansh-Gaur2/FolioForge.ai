# FolioForge.Infrastructure 🔧

> **Infrastructure Layer - External Concerns Implementation**

This layer contains all implementations for external dependencies: databases, message queues, AI services, and file handling. It implements interfaces defined in Application and Domain layers.

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Database Access** | Entity Framework Core DbContext and configurations |
| **Repository Implementations** | Concrete implementations of domain repositories |
| **External Services** | AI providers (Groq, OpenAI, Gemini), PDF parsing |
| **Message Queues** | RabbitMQ event publishing |
| **Dependency Injection** | Service registration extensions |

---

## 📂 Project Structure

```
FolioForge.Infrastructure/
├── Persistence/
│   └── ApplicationDbContext.cs      # EF Core DbContext
├── Repositories/
│   └── PortfolioRepository.cs       # IPortfolioRepository implementation
├── Services/
│   ├── GroqAiService.cs             # Groq (Llama 3.3) AI implementation
│   ├── OpenAiService.cs             # OpenAI GPT implementation
│   ├── GeminiAiService.cs           # Google Gemini implementation
│   └── PdfService.cs                # PDF text extraction (PdfPig)
├── Messaging/
│   └── RabbitMqEventPublisher.cs    # RabbitMQ event publisher
├── Migrations/
│   └── *.cs                         # EF Core migrations
├── DependencyInjection.cs           # Service registration
└── FolioForge.Infrastructure.csproj
```

---

## 🗄️ Database Access

### ApplicationDbContext

The EF Core DbContext managing all database operations:

```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioSection> Sections => Set<PortfolioSection>();
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) 
    { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Portfolio configuration
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Slug).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Title).IsRequired().HasMaxLength(100);
            
            // Theme stored as JSON string
            entity.Property(p => p.Theme)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Portfolio.ThemeConfig>(v, (JsonSerializerOptions)null)
                );
            
            // One-to-many relationship
            entity.HasMany(p => p.Sections)
                .WithOne()
                .HasForeignKey(s => s.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Section configuration
        modelBuilder.Entity<PortfolioSection>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.SectionType).IsRequired().HasMaxLength(50);
            entity.Property(s => s.Content).IsRequired();
        });
    }
}
```

**Key Configurations:**
- JSON serialization for Theme value object
- Cascade delete for sections when portfolio is deleted
- Unique index on slug for fast lookups

---

### PortfolioRepository

Implementation of `IPortfolioRepository`:

```csharp
public class PortfolioRepository : IPortfolioRepository
{
    private readonly ApplicationDbContext _context;
    
    public PortfolioRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Portfolio?> GetByIdAsync(Guid id)
    {
        return await _context.Portfolios
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<Portfolio?> GetBySlugAsync(string slug)
    {
        return await _context.Portfolios
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }
    
    public async Task AddAsync(Portfolio portfolio)
    {
        await _context.Portfolios.AddAsync(portfolio);
    }
    
    public async Task UpdateAsync(Portfolio portfolio)
    {
        _context.Portfolios.Update(portfolio);
    }
    
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
```

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
        
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        
        // Register DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
        
        // Repositories
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        
        // Event Publishing
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        
        // External Services
        services.AddScoped<IPdfService, PdfService>();
        services.AddHttpClient<IAiService, GroqAiService>();
        
        return services;
    }
    
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR - scans for all Commands/Queries
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(CreatePortfolioCommand).Assembly));
        
        return services;
    }
}
```

**Usage in Program.cs:**

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
```

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
    
    <!-- Message Queue -->
    <PackageReference Include="RabbitMQ.Client" Version="7.x" />
    
    <!-- PDF Processing -->
    <PackageReference Include="PdfPig" Version="0.x" />
    
    <!-- HTTP Client -->
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.x" />
</ItemGroup>
```

---

## 📚 Related Documentation

- [FolioForge.Api README](../FolioForge.Api/README.md) - HTTP entry point
- [FolioForge.Application README](../FolioForge.Application/README.md) - Interface definitions
- [FolioForge.Domain README](../FolioForge.Domain/README.md) - Entity definitions
- [FolioForge.Worker README](../FolioForge.Worker/README.md) - Message consumer
