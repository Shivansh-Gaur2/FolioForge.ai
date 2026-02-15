using FolioForge.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
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
// 2. API & SWAGGER CONFIGURATION
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
app.UseHttpsRedirection();

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