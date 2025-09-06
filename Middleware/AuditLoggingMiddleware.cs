using System.Diagnostics;
using System.Text;
using System.Text.Json;
using UserManagementAPI.Models.Audit;
using System.Security.Claims;

namespace UserManagementAPI.Middleware
{
  public class AuditLoggingMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly AuditConfiguration _config;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger,
        AuditConfiguration config)
    {
      _next = next;
      _logger = logger;
      _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      // Skip audit logging for excluded paths
      if (ShouldSkipLogging(context.Request.Path))
      {
        await _next(context);
        return;
      }

      var auditLog = new AuditLog
      {
        Method = context.Request.Method,
        Path = context.Request.Path.Value ?? string.Empty,
        QueryString = context.Request.QueryString.Value ?? string.Empty,
        UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty,
        ClientIpAddress = GetClientIpAddress(context),
        UserId = GetUserId(context),
        UserEmail = GetUserEmail(context)
      };

      // Log request headers
      if (_config.LogHeaders)
      {
        LogHeaders(context.Request.Headers, auditLog);
      }

      // Log request body
      string? requestBody = null;
      if (_config.LogRequestBody && HasContentBody(context.Request))
      {
        requestBody = await ReadRequestBodyAsync(context.Request);
        auditLog.RequestBody = TruncateIfNeeded(requestBody, _config.MaxBodyLogSize);
      }

      // Capture the original response stream
      var originalResponseStream = context.Response.Body;
      using var responseBodyStream = new MemoryStream();
      context.Response.Body = responseBodyStream;

      var stopwatch = Stopwatch.StartNew();
      Exception? exception = null;

      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        exception = ex;
        auditLog.ErrorMessage = ex.Message;
        throw;
      }
      finally
      {
        stopwatch.Stop();
        auditLog.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        auditLog.StatusCode = context.Response.StatusCode;

        // Log response body if configured
        if (_config.LogResponseBody && responseBodyStream.Length > 0)
        {
          responseBodyStream.Seek(0, SeekOrigin.Begin);
          var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
          auditLog.ResponseBody = TruncateIfNeeded(responseBody, _config.MaxBodyLogSize);
          auditLog.ResponseSizeBytes = responseBody.Length;
        }
        else
        {
          auditLog.ResponseSizeBytes = (int)responseBodyStream.Length;
        }

        // Copy response back to original stream
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseStream);
        context.Response.Body = originalResponseStream;

        // Log the audit entry
        LogAuditEntry(auditLog, exception);
      }
    }

    private bool ShouldSkipLogging(PathString path)
    {
      return _config.ExcludedPaths.Any(excludedPath =>
          path.Value?.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
      // Check for forwarded IP first (behind proxy/load balancer)
      var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
      if (!string.IsNullOrEmpty(forwardedFor))
      {
        return forwardedFor.Split(',')[0].Trim();
      }

      var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
      if (!string.IsNullOrEmpty(realIp))
      {
        return realIp;
      }

      // Fallback to connection remote IP
      return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static string? GetUserId(HttpContext context)
    {
      return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static string? GetUserEmail(HttpContext context)
    {
      return context.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    private void LogHeaders(IHeaderDictionary headers, AuditLog auditLog)
    {
      foreach (var header in headers)
      {
        if (!_config.SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
        {
          auditLog.Headers[header.Key] = string.Join(", ", header.Value);
        }
        else
        {
          auditLog.Headers[header.Key] = "[REDACTED]";
        }
      }
    }

    private static bool HasContentBody(HttpRequest request)
    {
      return request.ContentLength > 0 ||
             request.Headers.ContainsKey("Transfer-Encoding");
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
      request.EnableBuffering();

      using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
      var body = await reader.ReadToEndAsync();
      request.Body.Seek(0, SeekOrigin.Begin);

      return body;
    }

    private static string TruncateIfNeeded(string content, int maxLength)
    {
      if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
        return content;

      return content.Substring(0, maxLength) + "... [TRUNCATED]";
    }

    private void LogAuditEntry(AuditLog auditLog, Exception? exception)
    {
      var logLevel = GetLogLevel(auditLog.StatusCode, exception);
      var message = $"API Request: {auditLog.Method} {auditLog.Path} - Status: {auditLog.StatusCode} - Duration: {auditLog.ResponseTimeMs}ms";

      using var scope = _logger.BeginScope(new Dictionary<string, object>
      {
        ["RequestId"] = auditLog.RequestId,
        ["UserId"] = auditLog.UserId ?? "Anonymous",
        ["Method"] = auditLog.Method,
        ["Path"] = auditLog.Path,
        ["StatusCode"] = auditLog.StatusCode,
        ["ResponseTimeMs"] = auditLog.ResponseTimeMs,
        ["ClientIpAddress"] = auditLog.ClientIpAddress
      });

      _logger.Log(logLevel, exception, message);

      // Detailed audit log (could be sent to audit service or database)
      var auditJson = JsonSerializer.Serialize(auditLog, new JsonSerializerOptions
      {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      _logger.LogInformation("AUDIT: {AuditLog}", auditJson);
    }

    private static LogLevel GetLogLevel(int statusCode, Exception? exception)
    {
      if (exception != null)
        return LogLevel.Error;

      return statusCode switch
      {
        >= 500 => LogLevel.Error,
        >= 400 => LogLevel.Warning,
        _ => LogLevel.Information
      };
    }
  }

  // Extension method to register the middleware
  public static class AuditLoggingMiddlewareExtensions
  {
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
  }
}
