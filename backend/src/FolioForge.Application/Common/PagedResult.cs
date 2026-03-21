namespace FolioForge.Application.Common;

/// <summary>
/// A generic paginated response wrapper.
/// Returns items for the current page plus metadata for the client to build pagination UI.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
