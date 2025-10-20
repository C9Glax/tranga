using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

public sealed record PagedResponse<T>(IEnumerable<T> Data, int Page, int TotalPages, int TotalCount) where T : class
{
    /// <summary>
    /// The data
    /// </summary>
    [Required]
    public IEnumerable<T> Data { get; init; } = Data;
    
    /// <summary>
    /// The page-number
    /// </summary>
    [Required]
    public int Page { get; init; } = Page;
    
    /// <summary>
    /// The total number of pages (max page)
    /// </summary>
    [Required]
    public int TotalPages{ get; init; } = TotalPages;
    
    /// <summary>
    /// The total number of entries for this request
    /// </summary>
    [Required]
    public int TotalCount{ get; init; } = TotalCount;
}

public static class PagedResponseHelper
{
    public static PagedResponse<T> CreatePagedResponse<T>(this IEnumerable<T> data, int page, int pageSize) where T : class
    {
        int totalCount = data.Count();
        IEnumerable<T> pageData = data.Take(new Range(pageSize * (page - 1), pageSize * page));
        return new PagedResponse<T>(pageData, page, (totalCount - 1) / pageSize + 1, totalCount);
    }
}
