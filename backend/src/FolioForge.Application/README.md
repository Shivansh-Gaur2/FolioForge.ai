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
│   ├── CreatePortfolio/
│   │   ├── CreatePortfolioCommand.cs        # Command definition
│   │   └── CreatePortfolioCommandHandler.cs # Command handler (uses ITenantContext)
│   ├── DeletePortfolio/
│   │   ├── DeletePortfolioCommand.cs        # Command definition
│   │   └── DeletePortfolioCommandHandler.cs # Owner-only delete, returns bool
│   └── UpdateCustomization/
│       ├── UpdateCustomizationCommand.cs    # Theme/section update command
│       └── UpdateCustomizationCommandHandler.cs
├── Portfolios/
│   └── Queries/
│       ├── GetPortfolioByIdQuery.cs         # Query definition
│       ├── GetPortfolioByIdHandler.cs       # Query handler
│       ├── GetPortfoliosByUserQuery.cs      # Paginated list by userId
│       └── GetPortfoliosByUserHandler.cs
├── Common/
│   ├── Events/
│   │   └── ResumeUploadedEvent.cs           # Domain events
│   ├── RateLimiting/
│   │   └── IRateLimiter.cs                  # Rate limiter abstraction
│   └── Interfaces/
│       ├── IAiService.cs                    # AI service contract
│       ├── IApplicationDbContext.cs         # DbContext contract (5 DbSets incl. RefreshTokens)
│       ├── IAuthService.cs                  # JWT token generation + refresh helpers contract
│       ├── ICacheService.cs                 # Distributed cache contract
│       ├── IPdfService.cs                   # PDF extraction contract
│       ├── ITenantContext.cs                # Scoped tenant context
│       ├── ITenantRepository.cs             # Tenant data access contract
│       └── IUserRepository.cs              # User data access contract
├── DTOs/
│   ├── PortfolioDto.cs                      # Portfolio + Theme DTOs
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
public record CreatePortfolioCommand(Guid UserId, string Title, string DesiredSlug)
    : IRequest<Result<Guid>>;
```

**Flow:** validate slug uniqueness → create `Portfolio` entity with tenant from `ITenantContext` → add default section → persist.

---

### DeletePortfolioCommand

**Purpose:** Delete a portfolio, ensuring only the owning user can do so.

```csharp
public record DeletePortfolioCommand(Guid PortfolioId, Guid UserId)
    : IRequest<bool>;
```

Returns `false` if the portfolio does not exist or the `UserId` does not match → controller returns `404 Not Found`.

---

### UpdateCustomizationCommand

**Purpose:** Update a portfolio's theme (colors, fonts, layout) and section visibility/order.

```csharp
public record UpdateCustomizationCommand(
    Guid PortfolioId,
    Guid UserId,
    string PrimaryColor,
    string SecondaryColor,
    string BackgroundColor,
    string TextColor,
    string FontHeading,
    string FontBody,
    string Layout,
    IReadOnlyList<SectionUpdateDto> Sections
) : IRequest<bool>;
```

The handler updates the `ThemeConfig` value object on the portfolio and applies `sortOrder`/`isVisible`/`variant` changes to each section, then saves.

---

## 🔍 Queries

### GetPortfolioByIdQuery

**Purpose:** Fetch a single portfolio with all its sections by ID.

```csharp
public record GetPortfolioByIdQuery(Guid Id) : IRequest<PortfolioDto?>;
```

Handler uses `IApplicationDbContext` directly (no repository) for a simple EF Core read with `Include(p => p.Sections)`, then maps to `PortfolioDto`.

---

### GetPortfoliosByUserQuery

**Purpose:** Paginated list of portfolios owned by the current user.

```csharp
public record GetPortfoliosByUserQuery(Guid UserId, int Page, int PageSize)
    : IRequest<PagedResult<PortfolioDto>>;
```

Returns a `PagedResult<PortfolioDto>` with total count, allowing the frontend dashboard to render pagination controls.

---

## 📦 DTOs (Data Transfer Objects)

### PortfolioDto

```csharp
public class PortfolioDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public ThemeConfigDto Theme { get; set; } = new();
    public List<PortfolioSectionDto> Sections { get; set; } = new();
}

public class ThemeConfigDto
{
    public string Name { get; set; } = "default";
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string SecondaryColor { get; set; } = "#10B981";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#1F2937";
    public string FontHeading { get; set; } = "Inter";
    public string FontBody { get; set; } = "Inter";
    public string Layout { get; set; } = "single-column";
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
    public bool IsVisible { get; set; }
    public string Variant { get; set; } = "default";
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
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Portfolio> Portfolios { get; }
    DbSet<PortfolioSection> Sections { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

### IAuthService

JWT token generation and refresh token helpers:

```csharp
public interface IAuthService
{
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, string fullName);
    string GenerateRefreshTokenString();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

**Implementation:** `JwtAuthService` in Infrastructure layer.

### ICacheService

Distributed cache abstraction (implemented by `RedisCacheService`):

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, ...);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, ...);
    Task RemoveAsync(string key, ...);
    Task RemoveByPrefixAsync(string prefixKey, ...);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, ...);
    Task<bool> ExistsAsync(string key, ...);
}
```

### ITenantContext

Scoped service providing the current tenant for each request:

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantIdentifier { get; }
    bool IsResolved { get; }
    void SetTenant(Guid tenantId, string identifier);
}
```

**Purpose:** Set by `TenantMiddleware`, consumed by handlers and the `SaveChangesAsync` override to auto-assign `TenantId` on tenant entities.

### ITenantRepository

```csharp
public interface ITenantRepository
{
    Task<Tenant?> GetByIdentifierAsync(string identifier);
    Task<Tenant?> GetByIdAsync(Guid id);
    Task AddAsync(Tenant tenant);
    Task SaveChangesAsync();
}
```

### IUserRepository

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    /// <summary>
    /// Check if email exists across ALL tenants (bypasses query filters).
    /// Used during registration to prevent duplicate emails globally.
    /// </summary>
    Task<bool> EmailExistsGloballyAsync(string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
```

**Note:** `GetByEmailAsync` and `EmailExistsGloballyAsync` both bypass tenant query filters using `IgnoreQueryFilters()` — login and registration are cross-tenant operations.

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
