# FolioForge.Api 🌐

> **API Layer - The Entry Point for HTTP Requests**

This is the outermost layer of the Clean Architecture, responsible for handling HTTP requests, configuring dependency injection, and serving as the composition root for the entire application.

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **HTTP Handling** | Receive and respond to HTTP requests via ASP.NET Core Controllers |
| **Authentication** | JWT Bearer token validation and user identity resolution |
| **Tenant Resolution** | Middleware to resolve tenant from JWT or X-Tenant-Id header |
| **Request Validation** | Validate incoming request DTOs (contracts) |
| **Dependency Injection** | Configure and wire up all services at startup |
| **Middleware Pipeline** | CORS, tenant resolution, authentication, authorization |
| **API Documentation** | Swagger/OpenAPI spec with JWT security definitions |

---

## 📂 Project Structure

```
FolioForge.Api/
├── Controllers/
│   ├── AuthController.cs          # Register, Login, Refresh, Revoke, Me endpoints
│   ├── PortfoliosController.cs    # Portfolio CRUD + Resume Upload + Customization
│   ├── TenantsController.cs       # Tenant creation & lookup
│   └── ResilienceController.cs    # Circuit breaker + bulkhead live status (ops dashboard)
├── Contracts/
│   ├── CreatePortfolioRequest.cs  # Request DTOs
│   └── UpdateCustomizationRequest.cs
├── Properties/
│   └── launchSettings.json        # Development settings (5090 HTTP)
├── Uploads/                       # Uploaded PDF storage
├── Program.cs                     # Application entry point & DI setup
├── appsettings.json              # Production configuration (JWT, DB, AI, Redis)
└── appsettings.Development.json  # Development configuration
```

---

## 🔧 Key Components

### Program.cs - Composition Root

The `Program.cs` file serves as the composition root where all dependencies are wired together:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Infrastructure Layer (DbContext, Repos, AI services, RabbitMQ, JWT, Tenant)
builder.Services.AddInfrastructure(builder.Configuration);

// Application Layer (MediatR commands/queries)
builder.Services.AddApplication();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* TokenValidationParameters */ });

// API Layer (Controllers + Swagger with JWT security)
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c => { /* Bearer security definition */ });
```

#### JWT Authentication Configuration

```csharp
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FolioForge";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FolioForge.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        NameClaimType = JwtRegisteredClaimNames.Sub,
    };
});
```

#### Middleware Pipeline Order

The order of middleware is critical for correct tenant and auth resolution:

```csharp
app.UseCors("AllowReactApp");
app.UseMiddleware<TenantMiddleware>();  // Resolve tenant from JWT or header
app.UseMiddleware<RateLimitMiddleware>();  // Distributed token bucket rate limiting
app.UseMiddleware<BulkheadMiddleware>();   // Per-endpoint concurrency isolation
app.UseHttpsRedirection();
app.UseAuthentication();               // Validate JWT token
app.UseAuthorization();                // Enforce [Authorize] attribute
app.MapControllers();
app.MapHealthChecks("/health");
app.UseOpenTelemetryPrometheusScrapingEndpoint(); // /metrics
```

#### Swagger JWT Configuration

Swagger UI is configured with a Bearer token input for authenticated endpoint testing:

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FolioForge API",
        Version = "v1",
        Description = "AI-Powered Portfolio Builder API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { /* ... */ });
});
```

#### CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow common Vite development ports
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://localhost:5175",
                    "http://127.0.0.1:5173",
                    "http://127.0.0.1:5174"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            // Production: Explicit origins from configuration
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();
            
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
```

---

### AuthController.cs

Handles registration, login, token refresh, revocation, and the `/me` endpoint. Decorated with **rate limiting** (`Auth` policy: 5 burst / 2/s sustained) and **bulkhead** isolation to prevent credential stuffing from starving other operations.

```csharp
[ApiController]
[Route("api/[controller]")]
[RateLimit("Auth")]   // Strict: 5 burst, 2/s sustained
[Bulkhead("Auth")]    // Concurrency isolation
public class AuthController : ControllerBase { ... }
```

#### Auth Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/auth/register` | Public | Register a new user under a tenant |
| `POST` | `/api/auth/login` | Public | Login with email & password |
| `POST` | `/api/auth/refresh` | Public | Exchange expired access token + refresh token for a new pair (token rotation) |
| `POST` | `/api/auth/revoke` | `[Authorize]` | Revoke a refresh token (logout) |
| `GET` | `/api/auth/me` | `[Authorize]` | Get current user profile from JWT |

#### Register / Login Flow

```
1. Validate tenant exists and is active
2. Check email uniqueness globally (cross-tenant)
3. Hash password with BCrypt (register) / verify BCrypt hash (login)
4. Create User entity (register only)
5. Generate access token (15 min JWT) + refresh token (7-day opaque string, stored in DB)
6. Return AuthResponse { accessToken, refreshToken, userId, email, fullName, tenantId }
```

#### Token Refresh Flow

```
POST /api/auth/refresh { accessToken (expired OK), refreshToken }
1. Validate JWT signature (lifetime validation OFF)
2. Look up refreshToken in DB for the userId from JWT sub
3. If token already revoked → revoke ALL user tokens (token theft detection) → 401
4. Rotate: revoke old refreshToken, issue new access + refresh pair
← { accessToken (new, 15 min), refreshToken (new, 7 days), ... }
```

#### Request/Response DTOs

```csharp
// Register
public record RegisterRequest(string Email, string FullName,
    string Password, string TenantIdentifier);

// Login
public record LoginRequest(string Email, string Password);

// Refresh
public record RefreshTokenRequest(string AccessToken, string RefreshToken);

// Response (register / login / refresh)
public class AuthResponse
{
    public string AccessToken { get; set; }   // Short-lived JWT (15 min)
    public string RefreshToken { get; set; }  // Long-lived opaque token (7 days)
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public Guid TenantId { get; set; }
    public string TenantIdentifier { get; set; }
}
```

---

### TenantsController.cs

Handles tenant management. These endpoints are **excluded from tenant middleware** (no `X-Tenant-Id` header required).

```csharp
[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
}
```

#### Tenant Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/tenants` | Public | Create a new tenant |
| `GET` | `/api/tenants/{id:guid}` | Public | Get tenant by ID |

#### Create Tenant Flow

```
1. Check if identifier already exists → 409 Conflict
2. Create Tenant entity (Name, Identifier)
3. Return 201 Created with tenant details
```

---

### PortfoliosController.cs

The main API controller handling all portfolio-related operations. **All endpoints require JWT authentication** via the `[Authorize]` attribute.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfoliosController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IEventPublisher _publisher;

    public PortfoliosController(ISender mediator, IEventPublisher publisher)
    {
        _mediator = mediator;
        _publisher = publisher;
    }
}
```

#### Portfolio Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/portfolios` | Create new portfolio via MediatR command |
| `GET` | `/api/portfolios/mine?page=1&pageSize=10` | List portfolios for current user (paginated) |
| `GET` | `/api/portfolios/{id:guid}` | Fetch portfolio with sections via MediatR query |
| `GET` | `/api/portfolios/{slug}` | Fetch portfolio by URL slug |
| `DELETE` | `/api/portfolios/{id:guid}` | Delete portfolio (owner-only check in handler) |
| `PUT` | `/api/portfolios/{id:guid}/customization` | Update theme, colors, fonts, section order/visibility |
| `POST` | `/api/portfolios/{id}/upload-resume` | Upload PDF (magic-byte validated, 10 MB limit) and publish to RabbitMQ |

The upload endpoint applies a stricter `[RateLimit("Upload")]` + `[Bulkhead("Upload")]` policy on top of the controller-level `[Authorize]`.

#### User ID Extraction

The controller extracts the authenticated user's ID from JWT claims:

```csharp
private Guid GetUserId()
{
    var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.Parse(sub!);
}
```

---

### Resume Upload Flow

The `UploadResume` endpoint demonstrates the event-driven architecture with security hardening:

```csharp
[HttpPost("{id}/upload-resume")]
[RateLimit("Upload")]                   // Stricter rate limit
[Bulkhead("Upload")]                    // Dedicated concurrency partition
[RequestSizeLimit(10 * 1024 * 1024)]   // 10 MB hard limit at Kestrel level
public async Task<IActionResult> UploadResume(Guid id, IFormFile file)
{
    // 1. Validate size + extension
    // 2. Verify %PDF- magic bytes (not just the extension)
    // 3. Save PDF to disk (Uploads/ folder)
    // 4. Publish ResumeUploadedEvent to RabbitMQ (fire-and-forget)
    // 5. Return 202 Accepted immediately
    return Accepted(new { message = "Resume queued for processing", portfolioId = id });
}
```

**Security hardening applied:**
- File extension allowlist (`[.pdf]` only)
- Magic byte validation (reads first 5 bytes, must be `%PDF-`)
- 10 MB `RequestSizeLimit` applied at the Kestrel layer, not just application layer

---

### ResilienceController.cs

Exposes live observability data for the resilience infrastructure. Secured behind `[Authorize]` — for ops dashboards and alerting.

```
GET /api/resilience
← {
     circuitBreakers: [{ name, state, consecutiveFailures, failureThreshold, retryAfter }],
     bulkheadPartitions: [{ name, activeCount, queuedCount, maxConcurrency, utilizationPercent }]
   }
```

---

## 🔄 Request/Response Flow

```mermaid
sequenceDiagram
    participant Client
    participant TenantMiddleware
    participant Auth
    participant Controller
    participant MediatR
    participant Handler
    participant Repository
    participant Database
    
    Client->>TenantMiddleware: HTTP Request
    TenantMiddleware->>TenantMiddleware: Resolve tenant (JWT → Header)
    TenantMiddleware->>Auth: Pass to auth middleware
    Auth->>Auth: Validate JWT token
    Auth->>Controller: Authenticated request
    Controller->>MediatR: Send(Command/Query)
    MediatR->>Handler: Handle(request)
    Handler->>Repository: Data operation
    Repository->>Database: SQL Query (tenant-filtered)
    Database-->>Repository: Result
    Repository-->>Handler: Entity
    Handler-->>MediatR: Result<T>
    MediatR-->>Controller: Response
    Controller-->>Client: HTTP Response
```

### Tenant Middleware Resolution

The `TenantMiddleware` resolves the current tenant for each request:

```
1. Check if path is excluded (/api/auth/*, /api/tenants/*, /swagger/*)
   → If excluded, skip tenant resolution
2. Try to extract tenantId from JWT claim "tenantId"
   → If found, set tenant context
3. Fall back to X-Tenant-Id header
   → If found, set tenant context
4. If neither found → return 400 Bad Request
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=folioforge;Integrated Security=True;TrustServerCertificate=True;Pooling=True",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "FolioForge-SuperSecret-Key-That-Is-At-Least-32-Chars-Long!!",
    "Issuer": "FolioForge",
    "Audience": "FolioForge.Client",
    "ExpirationMinutes": "15",
    "RefreshTokenExpirationDays": "7"
  },
  "RabbitMq": {
    "HostName": "localhost"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key"
  },
  "OpenTelemetry": {
    "ServiceName": "FolioForge.Api",
    "OtlpEndpoint": "http://localhost:4317",
    "Sampling": { "SuccessRatio": 1.0 }
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

| Setting | Purpose |
|---------|----------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection (Integrated Security for Windows Auth) |
| `ConnectionStrings:Redis` | Redis connection string (rate limiting + caching) |
| `Jwt:Secret` | HMAC-SHA256 signing key (min 32 chars) |
| `Jwt:Issuer` | Token issuer claim |
| `Jwt:Audience` | Token audience claim |
| `Jwt:ExpirationMinutes` | Access token lifetime (`15` = 15 minutes) |
| `Jwt:RefreshTokenExpirationDays` | Refresh token lifetime (default `7` days) |
| `RabbitMq:HostName` | RabbitMQ broker hostname |
| `Groq:ApiKey` | API key for Groq AI (Llama 3.3-70B) |
| `OpenTelemetry:OtlpEndpoint` | Jaeger OTLP gRPC endpoint |
| `OpenTelemetry:Sampling:SuccessRatio` | Head-based sampling ratio (1.0 = 100%) |
| `Cors:AllowedOrigins` | Production allowed origins |

---

## 🧪 Running the API

```bash
# Navigate to API project
cd backend/src/FolioForge.Api

# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Start the API
dotnet run
```

**Default URLs:**
- HTTP: http://localhost:5090
- Swagger UI: http://localhost:5090 (root)

---

## 📝 Design Decisions

### Why Controllers over Minimal APIs?

| Consideration | Decision |
|--------------|----------|
| **Team familiarity** | Controllers are well-understood by most .NET developers |
| **Swagger integration** | Better out-of-box Swagger support with attributes |
| **Organization** | Natural grouping of related endpoints |
| **Testability** | Easy to mock with dependency injection |
| **Auth attributes** | Clean `[Authorize]` attribute support per controller or action |

### Why Return 202 for Resume Upload?

Long-running AI operations should not block HTTP requests:
- PDF parsing: ~500ms
- AI processing: ~2-5 seconds
- Database updates: ~100ms

Returning `202 Accepted` allows the client to continue while processing happens asynchronously.

### Why Exclude Auth/Tenant Routes from Tenant Middleware?

| Route | Reason |
|-------|--------|
| `/api/auth/register` | Tenant is provided in the request body (`tenantIdentifier`) |
| `/api/auth/login` | User lookup is cross-tenant (bypasses query filters) |
| `/api/tenants/*` | Tenant management is a global operation |
| `/swagger/*` | API docs are not tenant-scoped |

---

## 🔗 Dependencies

| Package | Purpose |
|---------|---------|
| `MediatR` | CQRS command/query dispatch |
| `Swashbuckle.AspNetCore` | Swagger/OpenAPI generation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT Bearer token authentication |
| `Microsoft.IdentityModel.Tokens` | Token validation parameters |
| `Microsoft.EntityFrameworkCore.SqlServer` | Database provider |

---

## 📚 Related Documentation

- [FolioForge.Application README](../FolioForge.Application/README.md) - Commands & Queries
- [FolioForge.Domain README](../FolioForge.Domain/README.md) - Domain Entities
- [FolioForge.Infrastructure README](../FolioForge.Infrastructure/README.md) - Data Access & Services
