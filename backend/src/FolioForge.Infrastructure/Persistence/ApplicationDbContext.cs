using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<PortfolioSection> Sections { get; set; }

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
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Portfolio.ThemeConfig>(v, (System.Text.Json.JsonSerializerOptions)null)
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