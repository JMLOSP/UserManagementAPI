namespace UserManagementAPI.Models.Error
{
  public class ErrorResponse
  {
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Extensions { get; set; } = new();
  }

  public class ValidationErrorResponse : ErrorResponse
  {
    public Dictionary<string, string[]> Errors { get; set; } = new();
  }

  public static class ErrorTypes
  {
    public const string ValidationError = "validation-error";
    public const string NotFound = "not-found";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string Conflict = "conflict";
    public const string InternalServerError = "internal-server-error";
    public const string BadRequest = "bad-request";
  }
}
