# FolioForge.Application 📋

> **Application Layer - Use Cases, Commands, Queries, and DTOs**

This layer contains the application's business logic orchestration. It defines **what** the application does without knowing **how** it's implemented (database, AI services, etc.).

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Use Case Orchestration** | Coordinate domain operations for specific user actions |
| **CQRS Implementation** | Separate Commands (writes) from Queries (reads) |
| **DTO Definitions** | Data structures for API responses |
| **Interface Contracts** | Define what infrastructure services must implement |
| **Validation** | Business rule validation (planned: FluentValidation) |

---

## 📂 Project Structure

```
FolioForge.Application/
├── Commands/
│   └── CreatePortfolio/
│       ├── CreatePortfolioCommand.cs        # Command definition
│       └── CreatePortfolioCommandHandler.cs # Command handler
├── Portfolios/
│   └── Queries/
│       ├── GetPortfolioByIdQuery.cs         # Query definition
│       └── GetPortfolioByIdHandler.cs       # Query handler
├── Common/
│   ├── Events/
│   │   └── ResumeUploadedEvent.cs           # Domain events
│   └── Interfaces/
│       ├── IAiService.cs                    # AI service contract
│       ├── IApplicationDbContext.cs         # DbContext contract
│       └── IPdfService.cs                   # PDF service contract
├── DTOs/
│   ├── PortfolioDto.cs                      # Portfolio response DTO
│   └── PortfolioSectionDto.cs               # Section response DTO
└── FolioForge.Application.csproj
```

---

## 🔧 CQRS with MediatR

We use **MediatR** to implement the Command Query Responsibility Segregation (CQRS) pattern.

### Why CQRS?

```
┌─────────────────────────────────────────────────────────┐
│                    Traditional Approach                  │
│  Read & Write through same models = Complex, Coupled    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                      CQRS Approach                       │
│                                                          │
│  ┌──────────────┐              ┌──────────────┐         │
│  │   Commands   │              │   Queries    │         │
│  │  (Writes)    │              │   (Reads)    │         │
│  │              │              │              │         │
│  │ • Create     │              │ • GetById    │         │
│  │ • Update     │              │ • GetBySlug  │         │
│  │ • Delete     │              │ • List       │         │
│  └──────────────┘              └──────────────┘         │
│         │                             │                  │
│         ▼                             ▼                  │
│  ┌──────────────┐              ┌──────────────┐         │
│  │  Repository  │              │  DbContext   │         │
│  │  (Complex)   │              │  (Simple)    │         │
│  └──────────────┘              └──────────────┘         │
└─────────────────────────────────────────────────────────┘
```

---

## 📝 Commands

### CreatePortfolioCommand

**Purpose:** Create a new portfolio for a user.

```csharp
// Command Definition (Immutable Record)
public record CreatePortfolioCommand(
    Guid UserId, 
    string Title, 
    string DesiredSlug
) : IRequest<Result<Guid>>;
```

**Handler Implementation:**

```csharp
public class CreatePortfolioCommandHandler : IRequestHandler<CreatePortfolioCommand, Result<Guid>>
{
    private readonly IPortfolioRepository _repository;

    public CreatePortfolioCommandHandler(IPortfolioRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken ct)
    {
        // 1. Business Rule: Check slug uniqueness
        var existing = await _repository.GetBySlugAsync(request.DesiredSlug);
        if (existing != null)
        {
            return Result<Guid>.Failure($"The URL '{request.DesiredSlug}' is already taken.");
        }

        // 2. Create domain entity
        var portfolio = new Portfolio(request.UserId, request.DesiredSlug, request.Title);

        // 3. Add default section
        var defaultBio = new { text = "Welcome to my portfolio!" };
        portfolio.AddSection(PortfolioSection.Create("Markdown", 0, defaultBio));

        // 4. Persist
        await _repository.AddAsync(portfolio);
        await _repository.SaveChangesAsync();

        // 5. Return success with new ID
        return Result<Guid>.Success(portfolio.Id);
    }
}
```

**Key Points:**
- Uses `Result<T>` pattern for explicit success/failure handling
- Validates business rules before creating entity
- Adds default section for new portfolios
- Repository handles persistence details

---

## 🔍 Queries

### GetPortfolioByIdQuery

**Purpose:** Fetch a portfolio with all its sections for display.

```csharp
// Query Definition
public record GetPortfolioByIdQuery(Guid Id) : IRequest<PortfolioDto?>;
```

**Handler Implementation:**

```csharp
public class GetPortfolioByIdHandler : IRequestHandler<GetPortfolioByIdQuery, PortfolioDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPortfolioByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioDto?> Handle(GetPortfolioByIdQuery request, CancellationToken ct)
    {
        // 1. Fetch with eager loading
        var entity = await _context.Portfolios
            .Include(p => p.Sections)  // CRITICAL: Load related sections
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (entity == null) return null;

        // 2. Map to DTO
        return new PortfolioDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            Theme = new ThemeConfigDto
            {
                Name = entity.Theme.Name,
                PrimaryColor = entity.Theme.PrimaryColor,
                FontBody = entity.Theme.FontBody
            },
            Sections = entity.Sections.Select(s => new PortfolioSectionDto
            {
                Id = s.Id,
                SectionType = s.SectionType,
                Content = s.Content,
                SortOrder = s.SortOrder
            }).ToList()
        };
    }
}
```

**Why Query Uses DbContext Directly?**

| Commands | Queries |
|----------|---------|
| Complex business logic | Simple data retrieval |
| Require domain entities | Return DTOs directly |
| Use repositories | Use DbContext (simpler) |
| May have side effects | Read-only, no side effects |

---

## 📦 DTOs (Data Transfer Objects)

### PortfolioDto

```csharp
public class PortfolioDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ThemeConfigDto Theme { get; set; } = new();
    public bool IsPublished { get; set; }
    public List<PortfolioSectionDto> Sections { get; set; } = new();
}

public class ThemeConfigDto
{
    public string Name { get; set; } = "default";
    public string PrimaryColor { get; set; } = "#000000";
    public string FontBody { get; set; } = "Inter";
}
```

### PortfolioSectionDto

```csharp
public class PortfolioSectionDto
{
    public Guid Id { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;  // JSON string
    public int SortOrder { get; set; }
}
```

---

## 🔌 Interface Contracts

### IAiService

Defines what AI capabilities the application needs:

```csharp
public interface IAiService
{
    /// <summary>
    /// Takes resume text and returns structured JSON portfolio data.
    /// </summary>
    Task<string> GeneratePortfolioDataAsync(string resumeText);
}
```

**Implementations:** `GroqAiService`, `OpenAiService`, `GeminiAiService`

### IPdfService

Defines PDF processing capabilities:

```csharp
public interface IPdfService
{
    /// <summary>
    /// Extracts text content from a PDF file.
    /// </summary>
    string ExtractText(string filePath);
}
```

### IApplicationDbContext

Exposes DbSets for queries to consume:

```csharp
public interface IApplicationDbContext
{
    DbSet<Portfolio> Portfolios { get; }
    DbSet<PortfolioSection> Sections { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

---

## 📡 Domain Events

### ResumeUploadedEvent

Published when a user uploads a resume PDF:

```csharp
public record ResumeUploadedEvent(Guid PortfolioId, string FilePath);
```

**Flow:**
1. API receives PDF upload
2. API publishes `ResumeUploadedEvent` to RabbitMQ
3. Worker service consumes event
4. Worker processes PDF with AI
5. Worker updates database

---

## 🔗 Dependencies

This project only depends on:
- **FolioForge.Domain** - For entity types and repository interfaces
- **MediatR** - For CQRS implementation
- **Microsoft.EntityFrameworkCore** - For DbContext interface

```xml
<ItemGroup>
    <ProjectReference Include="..\FolioForge.Domain\FolioForge.Domain.csproj" />
    <PackageReference Include="MediatR" Version="12.x" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.x" />
</ItemGroup>
```

---

## 📚 Related Documentation

- [FolioForge.Api README](../FolioForge.Api/README.md) - HTTP entry point
- [FolioForge.Domain README](../FolioForge.Domain/README.md) - Domain entities
- [FolioForge.Infrastructure README](../FolioForge.Infrastructure/README.md) - Service implementations
