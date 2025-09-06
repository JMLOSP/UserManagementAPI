namespace UserManagementAPI.Models.Audit
{
  public class AuditLog
  {
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public int ResponseSizeBytes { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
  }

  public class AuditConfiguration
  {
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = false;
    public bool LogHeaders { get; set; } = true;
    public List<string> SensitiveHeaders { get; set; } = new()
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key"
        };
    public List<string> ExcludedPaths { get; set; } = new()
        {
            "/health",
            "/metrics",
            "/swagger"
        };
    public int MaxBodyLogSize { get; set; } = 4096; // 4KB
  }
}
