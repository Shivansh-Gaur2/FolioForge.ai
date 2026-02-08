namespace FolioForge.Api.Contracts
{
    public record CreatePortfolioRequest(
        string Title, 
        string Slug
    );
}
