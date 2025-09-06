using System.Net;
using System.Text.Json;
using UserManagementAPI.Models.Error;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UserManagementAPI.Middleware
{
  public class ErrorHandlingMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
      _next = next;
      _logger = logger;
      _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unhandled exception occurred during request processing");
        await HandleExceptionAsync(context, ex);
      }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
      context.Response.ContentType = "application/json";

      var errorResponse = CreateErrorResponse(context, exception);

      // Set the status code
      context.Response.StatusCode = errorResponse.Status;

      // Serialize and write the error response
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = _environment.IsDevelopment()
      };

      var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
      await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(HttpContext context, Exception exception)
    {
      var requestId = context.TraceIdentifier;
      var requestPath = context.Request.Path.Value ?? string.Empty;

      return exception switch
      {
        ValidationException validationEx => new ValidationErrorResponse
        {
          Type = ErrorTypes.ValidationError,
          Title = "Validation Failed",
          Status = (int)HttpStatusCode.BadRequest,
          Detail = validationEx.Message,
          Instance = requestPath,
          TraceId = requestId,
          Errors = validationEx.Errors
        },

        UnauthorizedAccessException => new ErrorResponse
        {
          Type = ErrorTypes.Unauthorized,
          Title = "Unauthorized",
          Status = (int)HttpStatusCode.Unauthorized,
          Detail = "Authentication is required to access this resource",
          Instance = requestPath,
          TraceId = requestId
        },

        ForbiddenException => new ErrorResponse
        {
          Type = ErrorTypes.Forbidden,
          Title = "Forbidden",
          Status = (int)HttpStatusCode.Forbidden,
          Detail = "You don't have permission to access this resource",
          Instance = requestPath,
          TraceId = requestId
        },

        NotFoundException notFoundEx => new ErrorResponse
        {
          Type = ErrorTypes.NotFound,
          Title = "Resource Not Found",
          Status = (int)HttpStatusCode.NotFound,
          Detail = notFoundEx.Message,
          Instance = requestPath,
          TraceId = requestId
        },

        ConflictException conflictEx => new ErrorResponse
        {
          Type = ErrorTypes.Conflict,
          Title = "Conflict",
          Status = (int)HttpStatusCode.Conflict,
          Detail = conflictEx.Message,
          Instance = requestPath,
          TraceId = requestId
        },

        ArgumentException argEx => new ErrorResponse
        {
          Type = ErrorTypes.BadRequest,
          Title = "Bad Request",
          Status = (int)HttpStatusCode.BadRequest,
          Detail = argEx.Message,
          Instance = requestPath,
          TraceId = requestId
        },

        InvalidOperationException invalidOpEx => new ErrorResponse
        {
          Type = ErrorTypes.Conflict,
          Title = "Invalid Operation",
          Status = (int)HttpStatusCode.Conflict,
          Detail = invalidOpEx.Message,
          Instance = requestPath,
          TraceId = requestId
        },

        _ => new ErrorResponse
        {
          Type = ErrorTypes.InternalServerError,
          Title = "Internal Server Error",
          Status = (int)HttpStatusCode.InternalServerError,
          Detail = _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please try again later.",
          Instance = requestPath,
          TraceId = requestId,
          Extensions = _environment.IsDevelopment()
                ? new Dictionary<string, object>
                {
                  ["stackTrace"] = exception.StackTrace ?? string.Empty,
                  ["exceptionType"] = exception.GetType().Name
                }
                : new Dictionary<string, object>()
        }
      };
    }
  }

  // Custom exception classes for better error handling
  public class ValidationException : Exception
  {
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
      Errors = errors;
    }

    public ValidationException(ModelStateDictionary modelState)
        : base("One or more validation errors occurred")
    {
      Errors = modelState
          .Where(x => x.Value?.Errors?.Count > 0)
          .ToDictionary(
              kvp => kvp.Key,
              kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
          );
    }
  }

  public class NotFoundException : Exception
  {
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resourceType, object id)
        : base($"{resourceType} with ID '{id}' was not found") { }
  }

  public class ConflictException : Exception
  {
    public ConflictException(string message) : base(message) { }
  }

  public class ForbiddenException : Exception
  {
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException() : base("Access to this resource is forbidden") { }
  }

  // Extension method to register the middleware
  public static class ErrorHandlingMiddlewareExtensions
  {
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
  }
}
