using FluentAssertions;
using FolioForge.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FolioForge.Tests.Services;

public class JwtAuthServiceTests
{
    private readonly JwtAuthService _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string Email = "test@example.com";
    private const string FullName = "Test User";

    public JwtAuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "SuperSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "FolioForge.Tests",
                ["Jwt:Audience"] = "FolioForge.Tests.Client",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
            })
            .Build();

        _sut = new JwtAuthService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var token = _sut.GenerateAccessToken(_userId, _tenantId, Email, FullName);

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("FolioForge.Tests");
        jwt.Audiences.Should().Contain("FolioForge.Tests.Client");
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainExpectedClaims()
    {
        var token = _sut.GenerateAccessToken(_userId, _tenantId, Email, FullName);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == _userId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == Email);
        jwt.Claims.Should().Contain(c => c.Type == "fullName" && c.Value == FullName);
        jwt.Claims.Should().Contain(c => c.Type == "tenantId" && c.Value == _tenantId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ShouldExpireIn15Minutes()
    {
        var before = DateTime.UtcNow;
        var token = _sut.GenerateAccessToken(_userId, _tenantId, Email, FullName);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(before.AddMinutes(14));
        jwt.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void GenerateToken_ShouldDelegateToGenerateAccessToken()
    {
        var token1 = _sut.GenerateToken(_userId, _tenantId, Email, FullName);
        token1.Should().NotBeNullOrWhiteSpace();

        // Should produce a valid JWT
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token1).Should().BeTrue();
    }

    [Fact]
    public void GenerateRefreshTokenString_ShouldReturnBase64String()
    {
        var refreshToken = _sut.GenerateRefreshTokenString();

        refreshToken.Should().NotBeNullOrWhiteSpace();

        // Should be valid Base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Length.Should().Be(64); // 64 random bytes
    }

    [Fact]
    public void GenerateRefreshTokenString_ShouldBeUnique()
    {
        var token1 = _sut.GenerateRefreshTokenString();
        var token2 = _sut.GenerateRefreshTokenString();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnClaimsForValidToken()
    {
        var token = _sut.GenerateAccessToken(_userId, _tenantId, Email, FullName);

        var principal = _sut.GetPrincipalFromExpiredToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(_userId.ToString());
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnNullForInvalidToken()
    {
        var principal = _sut.GetPrincipalFromExpiredToken("totally.invalid.token");

        principal.Should().BeNull();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnNullForWrongSigningKey()
    {
        // Generate token with a different secret
        var otherConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ACompletelyDifferentSecretKey12345678!!",
                ["Jwt:Issuer"] = "FolioForge.Tests",
                ["Jwt:Audience"] = "FolioForge.Tests.Client",
            })
            .Build();

        var otherService = new JwtAuthService(otherConfig);
        var token = otherService.GenerateAccessToken(_userId, _tenantId, Email, FullName);

        // Try to validate with our service (different key)
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowIfSecretMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "FolioForge",
            })
            .Build();

        var act = () => new JwtAuthService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT Secret*");
    }
}
