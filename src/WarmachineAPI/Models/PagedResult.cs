namespace WarmachineAPI.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public static PagedResult<T> Create(IEnumerable<T> source, int? page, int? pageSize)
    {
        var normalizedPage = page is > 0 ? page.Value : 1;
        var normalizedPageSize = pageSize switch
        {
            > 0 and <= 200 => pageSize.Value,
            > 200 => 200,
            _ => 50
        };

        var materialized = source.ToList();

        return new PagedResult<T>
        {
            Items = materialized.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = materialized.Count
        };
    }
}
