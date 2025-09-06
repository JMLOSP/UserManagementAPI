using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models.Pagination
{
  public class PaginationRequest
  {
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
      get => _pageSize;
      set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public string? Filter { get; set; }
    public string? Department { get; set; }
    public bool? IsActive { get; set; } = true;
  }

  public class PaginatedResult<T>
  {
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
  }
}
