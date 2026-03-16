using System.ComponentModel.DataAnnotations;

namespace FolioForge.Api.Contracts
{
    public record CreatePortfolioRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Title,
        [Required, StringLength(100, MinimumLength = 3),
         RegularExpression(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$",
            ErrorMessage = "Slug must be lowercase alphanumeric with hyphens, e.g. 'my-portfolio'.")]
        string Slug
    );
}
