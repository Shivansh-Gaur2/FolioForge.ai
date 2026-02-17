# FolioForge.Domain 💎

> **Domain Layer - The Heart of the Application**

This is the innermost layer of Clean Architecture, containing the core business entities, value objects, and domain logic. It has **zero external dependencies** - pure C# only.

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Entities** | Business objects with identity and lifecycle |
| **Value Objects** | Immutable objects defined by their attributes |
| **Domain Events** | Notifications about domain state changes |
| **Repository Interfaces** | Contracts for data access (not implementations) |
| **Business Rules** | Invariants that must always be true |

---

## 📂 Project Structure

```
FolioForge.Domain/
├── Entities/
│   ├── BaseEntity.cs              # Abstract base for all entities
│   ├── Portfolio.cs               # Aggregate root for portfolios
│   └── PortfolioSection.cs        # Widget/section entity
├── Common/
│   └── Result.cs                  # Result pattern for operations
├── Interfaces/
│   ├── IPortfolioRepository.cs    # Repository contract
│   └── IEventPublisher.cs         # Event publishing contract
└── FolioForge.Domain.csproj
```

---

## 🏛️ Domain Entities

### BaseEntity

All entities inherit from `BaseEntity`, providing common audit fields:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
}
```

**Why GUID for IDs?**
- Globally unique - no collision risk across distributed systems
- Can generate client-side before database insert
- No sequential guessing (security)
- Natural for multi-tenant systems

---

### Portfolio (Aggregate Root)

The primary aggregate root representing a user's portfolio:

```csharp
public class Portfolio : BaseEntity
{
    // Immutable after creation - URL stability
    public string Slug { get; private set; } = default!;
    
    // Owner reference
    public Guid UserId { get; private set; }
    
    // Editable properties
    public string Title { get; private set; } = default!;
    public bool IsPublished { get; private set; }
    
    // Child collection - managed through methods
    public List<PortfolioSection> Sections { get; private set; } = new();
    
    // Value object for theme configuration
    public ThemeConfig Theme { get; private set; } = default!;
    
    // Private constructor for EF Core
    private Portfolio() { }
    
    // Public constructor enforces invariants
    public Portfolio(Guid userId, string slug, string title)
    {
        UserId = userId;
        Slug = slug;
        Title = title;
        IsPublished = true;
        Theme = new ThemeConfig("default", "#000000", "Inter");
    }
    
    // Domain methods encapsulate business logic
    public void AddSection(PortfolioSection section)
    {
        Sections.Add(section);
    }
    
    public void UpdateTheme(string primaryColor, string font)
    {
        Theme = new ThemeConfig(Theme.Name, primaryColor, font);
    }
}
```

**Design Decisions:**

| Decision | Rationale |
|----------|-----------|
| `private set` on most properties | Enforce changes through domain methods |
| `private Portfolio()` constructor | Required for EF Core, prevents invalid state |
| Public constructor with required params | Ensures entity is always valid after creation |
| `AddSection` method | Controls how sections are added, could add validation |

---

### ThemeConfig (Value Object)

Embedded value object for theme settings:

```csharp
public record ThemeConfig(string Name, string PrimaryColor, string FontBody);
```

**Why Record?**
- Immutable by default
- Value-based equality
- Concise syntax
- Perfect for configuration/settings
- EF Core can store as JSON

---

### PortfolioSection

Generic widget container supporting multiple content types:

```csharp
public class PortfolioSection : BaseEntity
{
    // Foreign key (allows direct inserts without loading parent)
    public Guid PortfolioId { get; set; }
    
    // Widget type: "About", "Skills", "Timeline", "Projects"
    public string SectionType { get; private set; } = default!;
    
    // Display order
    public int SortOrder { get; set; }
    
    // Visibility flag
    public bool IsVisible { get; set; } = true;
    
    // JSON content storage
    public string Content { get; private set; } = default!;
    
    private PortfolioSection() { }
    
    public PortfolioSection(string sectionType, int order, string content)
    {
        SectionType = sectionType;
        SortOrder = order;
        Content = content;
    }
    
    // Factory method for cleaner creation
    public static PortfolioSection Create(string type, int order, object data)
    {
        var json = JsonSerializer.Serialize(data);
        return new PortfolioSection(type, order, json);
    }
    
    // Update content
    public void UpdateContent(object newData)
    {
        Content = JsonSerializer.Serialize(newData);
    }
    
    // Deserialize to specific type
    public T? GetContent<T>()
    {
        return JsonSerializer.Deserialize<T>(Content);
    }
}
```

**Factory Pattern Usage:**

```csharp
// Clean creation without manual JSON serialization
var section = PortfolioSection.Create("Skills", 2, new { 
    items = new[] { "C#", "React", "Azure" } 
});
```

---

## 📊 Section Types & JSON Schemas

The generic widget system supports multiple section types:

| Type | SortOrder | JSON Schema |
|------|-----------|-------------|
| **About** | 1 | `{ "content": "Professional summary..." }` |
| **Skills** | 2 | `{ "items": ["C#", "React", "Azure"] }` |
| **Timeline** | 3 | `{ "items": [{ "Company": "...", "Role": "...", "Points": [...] }] }` |
| **Projects** | 4 | `{ "items": [{ "Name": "...", "TechStack": "...", "Points": [...] }] }` |

**Why JSON Storage?**

- **Flexibility:** Add new section types without migrations
- **Schema-less:** Each type can have different structure
- **Open/Closed Principle:** Extend without modifying existing code

---

## 🎯 Result Pattern

Type-safe operation results without exceptions:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

**Usage in Handlers:**

```csharp
public async Task<Result<Guid>> Handle(CreatePortfolioCommand request, CancellationToken ct)
{
    // Check business rule
    var existing = await _repository.GetBySlugAsync(request.DesiredSlug);
    if (existing != null)
    {
        return Result<Guid>.Failure($"The URL '{request.DesiredSlug}' is already taken.");
    }
    
    // Success path
    var portfolio = new Portfolio(request.UserId, request.DesiredSlug, request.Title);
    await _repository.AddAsync(portfolio);
    await _repository.SaveChangesAsync();
    
    return Result<Guid>.Success(portfolio.Id);
}
```

**In Controller:**

```csharp
var result = await _mediator.Send(command);

if (!result.IsSuccess) 
    return BadRequest(new { error = result.Error });

return CreatedAtAction(..., new { id = result.Value });
```

---

## 🔌 Repository Interfaces

### IPortfolioRepository

```csharp
public interface IPortfolioRepository
{
    Task<Portfolio?> GetByIdAsync(Guid id);
    Task<Portfolio?> GetBySlugAsync(string slug);
    Task AddAsync(Portfolio portfolio);
    Task UpdateAsync(Portfolio portfolio);
    Task SaveChangesAsync();
}
```

**Why Interface in Domain Layer?**
- Domain defines what it needs (contract)
- Infrastructure provides implementation
- Enables testing with mocks
- Follows Dependency Inversion Principle

### IEventPublisher

```csharp
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event) where T : class;
}
```

---

## 🧪 Domain Rules (Invariants)

| Rule | Enforcement |
|------|-------------|
| Portfolio must have a slug | Constructor requires slug |
| Portfolio must have a title | Constructor requires title |
| Slug is immutable after creation | `private set` |
| Theme cannot be null | Initialized in constructor |
| Section must have a type | Constructor requires sectionType |

---

## 📝 Design Principles Applied

| Principle | Application |
|-----------|-------------|
| **Single Responsibility** | Each entity handles its own domain logic |
| **Open/Closed** | Add new section types via JSON, not code changes |
| **Liskov Substitution** | All entities extend BaseEntity consistently |
| **Interface Segregation** | Small, focused repository interface |
| **Dependency Inversion** | Domain defines interfaces, infra implements |

---

## 🔗 Dependencies

**This project has NO external dependencies** - only .NET Base Class Library:

```xml
<ItemGroup>
    <!-- No PackageReferences - pure domain -->
</ItemGroup>
```

This is intentional:
- Domain should be stable and change rarely
- No coupling to frameworks or libraries
- Easy to unit test
- Can be shared across projects

---

## 📚 Related Documentation

- [FolioForge.Application README](../FolioForge.Application/README.md) - Use cases using domain
- [FolioForge.Infrastructure README](../FolioForge.Infrastructure/README.md) - Repository implementations
