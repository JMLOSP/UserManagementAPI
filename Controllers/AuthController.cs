using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models.Auth;
using UserManagementAPI.Middleware;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Produces("application/json")]
  public class AuthController : ControllerBase
  {
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    // Simple in-memory user store for demo purposes
    private static readonly Dictionary<string, (string Password, UserInfo UserInfo)> _users = new()
        {
            {
                "admin@techhive.com",
                ("Admin123!", new UserInfo
                {
                    Id = "1",
                    Email = "admin@techhive.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    Roles = new List<string> { "Admin", "User" }
                })
            },
            {
                "user@techhive.com",
                ("User123!", new UserInfo
                {
                    Id = "2",
                    Email = "user@techhive.com",
                    FirstName = "Regular",
                    LastName = "User",
                    Roles = new List<string> { "User" }
                })
            },
            {
                "john.doe@company.com",
                ("Password123!", new UserInfo
                {
                    Id = "3",
                    Email = "john.doe@company.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Roles = new List<string> { "User" }
                })
            }
        };

    public AuthController(IJwtTokenService tokenService, ILogger<AuthController> logger)
    {
      _tokenService = tokenService;
      _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return Task.FromResult<ActionResult<LoginResponse>>(BadRequest(ModelState));
        }

        // Validate credentials
        if (!_users.TryGetValue(request.Email.ToLowerInvariant(), out var userData) ||
            userData.Password != request.Password)
        {
          _logger.LogWarning("Failed login attempt for email: {Email} from IP: {ClientIp}",
              request.Email, HttpContext.Connection.RemoteIpAddress);

          return Task.FromResult<ActionResult<LoginResponse>>(Unauthorized(new
          {
            type = "unauthorized",
            title = "Authentication Failed",
            status = 401,
            detail = "Invalid email or password",
            timestamp = DateTime.UtcNow
          }));
        }

        // Generate JWT token
        var token = _tokenService.GenerateToken(userData.UserInfo);
        var expiresAt = DateTime.UtcNow.AddMinutes(60); // Should match JWT settings

        var response = new LoginResponse
        {
          Token = token,
          TokenType = "Bearer",
          ExpiresAt = expiresAt,
          User = userData.UserInfo
        };

        _logger.LogInformation("Successful login for user: {Email} ({UserId})",
            userData.UserInfo.Email, userData.UserInfo.Id);

        return Task.FromResult<ActionResult<LoginResponse>>(Ok(response));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login process for email: {Email}", request.Email);
        return Task.FromResult<ActionResult<LoginResponse>>(StatusCode(500, new
        {
          type = "internal-server-error",
          title = "Authentication Error",
          status = 500,
          detail = "An error occurred during authentication",
          timestamp = DateTime.UtcNow
        }));
      }
    }

    /// <summary>
    /// Validates the current JWT token
    /// </summary>
    /// <returns>Token validation status and user information</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserInfo> ValidateToken()
    {
      try
      {
        // If we reach here, the token was validated by the middleware
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var firstName = User.FindFirst("given_name")?.Value;
        var lastName = User.FindFirst("family_name")?.Value;
        var roles = User.FindAll("roles").Select(c => c.Value).ToList();

        var userInfo = new UserInfo
        {
          Id = userId ?? string.Empty,
          Email = email ?? string.Empty,
          FirstName = firstName ?? string.Empty,
          LastName = lastName ?? string.Empty,
          Roles = roles
        };

        return Ok(userInfo);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during token validation");
        return Unauthorized(new
        {
          type = "unauthorized",
          title = "Token Validation Failed",
          status = 401,
          detail = "Invalid or expired token",
          timestamp = DateTime.UtcNow
        });
      }
    }

    /// <summary>
    /// Gets available test credentials for demo purposes
    /// </summary>
    /// <returns>List of test accounts</returns>
    [HttpGet("test-credentials")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetTestCredentials()
    {
      var credentials = _users.Select(kvp => new
      {
        Email = kvp.Key,
        Password = kvp.Value.Password,
        Roles = kvp.Value.UserInfo.Roles,
        Name = $"{kvp.Value.UserInfo.FirstName} {kvp.Value.UserInfo.LastName}"
      }).ToList();

      return Ok(new
      {
        Message = "Available test credentials for API authentication",
        Note = "This endpoint should be removed in production",
        Credentials = credentials
      });
    }

    /// <summary>
    /// Health check endpoint for authentication service
    /// </summary>
    /// <returns>Service status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult HealthCheck()
    {
      return Ok(new
      {
        Service = "Authentication Service",
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
      });
    }
  }
}
