using FolioForge.Infrastructure;
using FolioForge.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// CORS CONFIGURATION
// Trade-off: Development uses permissive origins for flexibility
// Production should use explicit, restrictive origins
// ==================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Development: Allow common Vite ports
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
                // Production: Explicit origin from configuration
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


// ==================================================================
// 1. DATABASE SETUP (Your MySQL Config)
// ==================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// We use the 'AddInfrastructure' method we created earlier to keep this file clean.
// This registers the DbContext AND the Repositories automatically.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(); // Registers MediatR commands

// ==================================================================
// 2. JWT AUTHENTICATION
// ==================================================================
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
        // Map "sub" claim correctly
        NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
    };
});
builder.Services.AddAuthorization();

// ==================================================================
// 3. API & SWAGGER CONFIGURATION
// ==================================================================

// Add support for Controllers (since we are using Clean Architecture, not Minimal APIs)
builder.Services.AddControllers();

// Add Swagger Generator (The UI definition)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FolioForge API",
        Version = "v1",
        Description = "AI-Powered Portfolio Builder API"
    });

    // Add JWT Bearer support to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ==================================================================
// 3. HTTP REQUEST PIPELINE
// ==================================================================

// Enable Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Generates the JSON
    app.UseSwaggerUI(options => // Generates the UI Page
    {
        // This makes the Swagger UI load at the root URL (localhost:5000/)
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}
app.UseCors("AllowReactApp");

// Multi-Tenancy: Resolve tenant from JWT or X-Tenant-Id header
app.UseMiddleware<TenantMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map the Controllers (connects your PortfoliosController)
try
{
    app.MapControllers();
}
catch (System.Reflection.ReflectionTypeLoadException ex)
{
    foreach (var loaderException in ex.LoaderExceptions)
    {
        Console.WriteLine($"MISSING DEPENDENCY: {loaderException?.Message}");
    }
    throw;
}
app.Run();