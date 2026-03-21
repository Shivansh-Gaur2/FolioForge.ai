using FluentAssertions;
using FolioForge.Domain.Entities;

namespace FolioForge.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void NewToken_ShouldBeActive()
    {
        var token = new RefreshToken("abc123", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        token.IsActive.Should().BeTrue();
        token.RevokedAt.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void ExpiredToken_ShouldBeInactive()
    {
        var token = new RefreshToken("abc123", Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldMarkInactive()
    {
        var token = new RefreshToken("abc123", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        token.Revoke("newToken456");

        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByToken.Should().Be("newToken456");
    }

    [Fact]
    public void Revoke_WithoutReplacement_ShouldStillDeactivate()
    {
        var token = new RefreshToken("abc123", Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.IsActive.Should().BeFalse();
        token.RevokedAt.Should().NotBeNull();
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = new RefreshToken("tokenValue", userId, expiresAt);

        token.Token.Should().Be("tokenValue");
        token.UserId.Should().Be(userId);
        token.ExpiresAt.Should().Be(expiresAt);
        token.Id.Should().NotBeEmpty();
    }
}

public class PagedResultTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var result = new FolioForge.Application.Common.PagedResult<string>
        {
            Items = ["a", "b"],
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        result.TotalPages.Should().Be(3); // ceil(25/10) = 3
    }

    [Fact]
    public void TotalPages_SinglePage()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Items = [1, 2, 3],
            Page = 1,
            PageSize = 10,
            TotalCount = 3
        };

        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void HasNextPage_ShouldBeTrueWhenNotOnLastPage()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_ShouldBeFalseOnLastPage()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Page = 3,
            PageSize = 10,
            TotalCount = 25
        };

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalseOnFirstPage()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrueAfterFirstPage()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25
        };

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ExactlyDivisible_TotalPages_ShouldBeExact()
    {
        var result = new FolioForge.Application.Common.PagedResult<int>
        {
            Page = 1,
            PageSize = 5,
            TotalCount = 20
        };

        result.TotalPages.Should().Be(4); // 20/5 = 4 exactly
    }
}
