using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FolioForge.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<PortfolioSection> Sections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // 1. Configure Portfolio (The Core Entity)
        // ============================================================
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios"); // Explicit table name

            entity.HasKey(e => e.Id);

            // Create a UNIQUE index on the Slug so two users can't claim "shivansh"
            entity.HasIndex(e => e.Slug)
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
}