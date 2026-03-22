using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Well-known default tenant ID. Seeded via HasData so the same GUID
    /// is deterministic across environments.
    /// </summary>
    public static readonly Guid DefaultTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<PortfolioSection> Sections { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Plan> Plans { get; set; }

    /// <summary>Well-known plan IDs. Seeded via HasData.</summary>
    public static readonly Guid FreePlanId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid ProPlanId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // 0. Configure Tenant (Multi-Tenancy Root)
        // ============================================================
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Identifier)
                  .IsUnique();

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Identifier)
                  .IsRequired()
                  .HasMaxLength(50);

            // Seed the default tenant so the app works out-of-the-box
            entity.HasData(new
            {
                Id = DefaultTenantId,
                Name = "FolioForge",
                Identifier = "folioforge",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        // ============================================================
        // 0.5. Configure Plan (Subscription Plans)
        // ============================================================
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StripePriceMonthlyId).HasMaxLength(100);
            entity.Property(e => e.StripePriceYearlyId).HasMaxLength(100);

            // Seed Free and Pro plans
            entity.HasData(
                new
                {
                    Id = FreePlanId,
                    Name = "Free",
                    Slug = "free",
                    PriceMonthlyInCents = 0,
                    PriceYearlyInCents = 0,
                    MaxPortfolios = 1,
                    MaxAiParsesPerMonth = 1,
                    CustomDomain = false,
                    RemoveWatermark = false,
                    PasswordProtection = false,
                    Analytics = false,
                    StripePriceMonthlyId = (string?)null,
                    StripePriceYearlyId = (string?)null,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new
                {
                    Id = ProPlanId,
                    Name = "Pro",
                    Slug = "pro",
                    PriceMonthlyInCents = 999,
                    PriceYearlyInCents = 9990,
                    MaxPortfolios = 100,
                    MaxAiParsesPerMonth = 100,
                    CustomDomain = true,
                    RemoveWatermark = true,
                    PasswordProtection = true,
                    Analytics = true,
                    StripePriceMonthlyId = "plan_SUATD5lUCBKQnG",
                    StripePriceYearlyId = (string?)null,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }
            );
        });

        // ============================================================
        // 0.6. Configure User (Tenant-Scoped)
        // ============================================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            // Email must be unique globally (not per-tenant)
            entity.HasIndex(e => e.Email)
                  .IsUnique();

            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(e => e.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                  .IsRequired();

            // Billing fields
            entity.Property(e => e.PlanId)
                  .HasDefaultValue(FreePlanId);

            entity.Property(e => e.StripeCustomerId)
                  .HasMaxLength(100);

            entity.Property(e => e.StripeSubscriptionId)
                  .HasMaxLength(100);

            entity.Property(e => e.SubscriptionStatus)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasDefaultValue("active");

            entity.HasIndex(e => e.TenantId);

            // Tenant query filter — users only see their own tenant's data
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        // ============================================================
        // 1. Configure Portfolio (The Core Entity)
        // ============================================================
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios"); // Explicit table name

            entity.HasKey(e => e.Id);

            // Create a UNIQUE index on Slug + TenantId 
            // so slugs are unique per tenant, not globally
            entity.HasIndex(e => new { e.TenantId, e.Slug })
                  .IsUnique();

            entity.Property(e => e.Slug)
                  .IsRequired()
                  .HasMaxLength(50); // URL-friendly length

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(100);

            // SQL Server JSON Column Configuration
            // We store the ThemeConfig object as a raw JSON string
            entity.Property(e => e.Theme)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Portfolio.ThemeConfig>(v, (System.Text.Json.JsonSerializerOptions?)null)
                          ?? new Portfolio.ThemeConfig("default", "#3B82F6", "#10B981", "#FFFFFF", "#1F2937", "Inter", "Inter", "single-column")
                  )
                  .HasColumnType("nvarchar(max)")
                  .IsRequired();

            // Setup the relationship: One Portfolio -> Many Sections
            entity.HasMany(e => e.Sections)
                  .WithOne()
                  .HasForeignKey(s => s.PortfolioId)
                  .OnDelete(DeleteBehavior.Cascade); // If Portfolio is deleted, Sections vanish

            // Index on TenantId for fast tenant-scoped queries
            entity.HasIndex(e => e.TenantId);

            // ============================================================
            // MULTI-TENANCY: Global Query Filter
            // This ensures ALL queries on Portfolios are automatically
            // scoped to the current tenant. No data leaks possible.
            // ============================================================
            entity.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        });

        // ============================================================
        // 2. Configure PortfolioSection (The Generic Widget)
        // ============================================================
        modelBuilder.Entity<PortfolioSection>(entity =>
        {
            entity.ToTable("portfolio_sections");

            entity.HasKey(e => e.Id);

            // Discriminator for the Widget Type (e.g., "Timeline", "Grid")
            entity.Property(e => e.SectionType)
                  .IsRequired()
                  .HasMaxLength(50);

            // Display variant for customization
            entity.Property(e => e.Variant)
                  .HasMaxLength(50)
                  .HasDefaultValue("default");

            // Performance: Index the SectionType
            // This allows fast queries like: "Find all Grid widgets"
            entity.HasIndex(e => e.SectionType);

            // THE CRITICAL PART: Generic JSON Content
            // Store JSON as nvarchar(max) for SQL Server compatibility
            entity.Property(e => e.Content)
                  .HasColumnType("nvarchar(max)")
                  .IsRequired();

            entity.Property(e => e.SortOrder)
                  .HasDefaultValue(0);
        });

        // ============================================================
        // 3. Configure RefreshToken (Auth Token Rotation)
        // ============================================================
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Token)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(e => e.ReplacedByToken)
                  .HasMaxLength(256);
        });
    }

    /// <summary>
    /// Automatically sets TenantId on new entities that implement ITenantEntity.
    /// This prevents forgetting to set TenantId when creating new records.
    /// </summary>
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
}