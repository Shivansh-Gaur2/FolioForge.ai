using FluentAssertions;
using FolioForge.Application.Common.Interfaces;
using FolioForge.Domain.Entities;
using FolioForge.Infrastructure.Persistence;
using FolioForge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FolioForge.Tests.Repositories;

public class PortfolioRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PortfolioRepository _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PortfolioRepositoryTests()
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(t => t.TenantId).Returns(_tenantId);
        tenantContext.Setup(t => t.IsResolved).Returns(true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, tenantContext.Object);
        _sut = new PortfolioRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistPortfolio()
    {
        var portfolio = new Portfolio(_userId, _tenantId, "my-portfolio", "My Portfolio");

        await _sut.AddAsync(portfolio);
        await _sut.SaveChangesAsync();

        var result = await _context.Portfolios.FirstOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Title.Should().Be("My Portfolio");
        result.Slug.Should().Be("my-portfolio");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPortfolioWithSections()
    {
        var portfolio = new Portfolio(_userId, _tenantId, "test-slug", "Test");
        portfolio.AddSection(new PortfolioSection("hero", 0, "{\"name\":\"John\"}"));
        portfolio.AddSection(new PortfolioSection("about", 1, "{\"bio\":\"Hello\"}"));

        await _context.Portfolios.AddAsync(portfolio);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(portfolio.Id);

        result.Should().NotBeNull();
        result!.Sections.Should().HaveCount(2);
        result.Sections.Should().Contain(s => s.SectionType == "hero");
        result.Sections.Should().Contain(s => s.SectionType == "about");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForNonexistent()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnMatchingPortfolio()
    {
        var portfolio = new Portfolio(_userId, _tenantId, "unique-slug", "Unique");
        await _context.Portfolios.AddAsync(portfolio);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBySlugAsync("unique-slug");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Unique");
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnNullForNonexistent()
    {
        var result = await _sut.GetBySlugAsync("does-not-exist");
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyPortfolio()
    {
        var portfolio = new Portfolio(_userId, _tenantId, "update-test", "Original Title");
        await _context.Portfolios.AddAsync(portfolio);
        await _context.SaveChangesAsync();

        portfolio.UpdateCustomization(
            "dark", "#000000", "#111111", "#222222", "#FFFFFF",
            "Roboto", "Open Sans", "two-column");

        await _sut.UpdateAsync(portfolio);
        await _sut.SaveChangesAsync();

        var result = await _context.Portfolios.FindAsync(portfolio.Id);
        result!.Theme.Name.Should().Be("dark");
        result.Theme.PrimaryColor.Should().Be("#000000");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePortfolio()
    {
        var portfolio = new Portfolio(_userId, _tenantId, "delete-me", "Delete Me");
        await _context.Portfolios.AddAsync(portfolio);
        await _context.SaveChangesAsync();

        await _sut.DeleteAsync(portfolio);
        await _sut.SaveChangesAsync();

        var result = await _context.Portfolios.FindAsync(portfolio.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSetTenantIdOnNewEntities()
    {
        var otherTenantId = Guid.NewGuid();
        // Portfolio created with a specific tenantId, but SaveChangesAsync should
        // override with the current tenant context
        var portfolio = new Portfolio(_userId, otherTenantId, "tenant-test", "Tenant Test");

        await _sut.AddAsync(portfolio);
        await _sut.SaveChangesAsync();

        // The DbContext.SaveChangesAsync sets TenantId from ITenantContext for new entities
        var result = await _context.Portfolios.IgnoreQueryFilters().FirstAsync(p => p.Slug == "tenant-test");
        result.TenantId.Should().Be(_tenantId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
