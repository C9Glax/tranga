using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

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
    /// <summary>
    /// Creates a PagedResponse from a sorted <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PagedResponse<T> CreatePagedResponse<T>(this IEnumerable<T> data, int page, int pageSize) where T : class
    {
        int totalCount = data.Count();
        IEnumerable<T> pageData = data.Take(new Range(pageSize * (page - 1), pageSize * page));
        return new PagedResponse<T>(pageData, page, (totalCount - 1) / pageSize + 1, totalCount);
    }

    /// <summary>
    /// Creates a Paged result from a Query. Results are sorted by key-selector in descending order
    /// </summary>
    /// <param name="queryable"></param>
    /// <param name="keySelector"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static async Task<PagedResponse<T>> CreatePagedResponse<T, TKey>(this IQueryable<T> queryable, Expression<Func<T, TKey>> keySelector, int page, int pageSize, CancellationToken ct)
        where T : class
    {
        int totalResults = await queryable.CountAsync(ct);
        List<T> listAsync = await queryable.OrderByDescending(keySelector).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new (listAsync, page, (totalResults - 1) / pageSize + 1, totalResults);
    }

    /// <summary>
    /// Converts the Datatype of a Pagedresult to desired new type using a conversion function
    /// </summary>
    /// <param name="pagedResponse"></param>
    /// <param name="mapper">conversion funciton</param>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <returns></returns>
    public static PagedResponse<TOut> ToType<TIn, TOut>(this PagedResponse<TIn> pagedResponse, Func<TIn, TOut> mapper)
        where TIn : class where TOut : class => new PagedResponse<TOut>(pagedResponse.Data.Select(mapper), pagedResponse.Page, pagedResponse.TotalPages, pagedResponse.TotalCount);
}
