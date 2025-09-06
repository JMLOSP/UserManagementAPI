using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagementAPI.Models.Auth;

namespace UserManagementAPI.Middleware
{
  public class JwtAuthenticationMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly JwtSettings _jwtSettings;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        JwtSettings jwtSettings)
    {
      _next = next;
      _logger = logger;
      _jwtSettings = jwtSettings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      // Skip authentication for public endpoints
      if (IsPublicEndpoint(context.Request.Path))
      {
        await _next(context);
        return;
      }

      try
      {
        var token = ExtractTokenFromHeader(context.Request);
        if (!string.IsNullOrEmpty(token))
        {
          var principal = ValidateToken(token);
          if (principal != null)
          {
            context.User = principal;
            _logger.LogDebug("JWT authentication successful for user {UserId}",
                principal.FindFirst(TokenClaims.UserId)?.Value);
          }
          else
          {
            _logger.LogWarning("JWT token validation failed");
            await HandleUnauthorized(context, "Invalid or expired token");
            return;
          }
        }
        else
        {
          _logger.LogWarning("No JWT token provided for protected endpoint: {Path}", context.Request.Path);
          await HandleUnauthorized(context, "Authorization token is required");
          return;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during JWT authentication");
        await HandleUnauthorized(context, "Authentication failed");
        return;
      }

      await _next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
      var publicPaths = new[]
      {
                "/api/auth/login",
                "/health",
                "/swagger",
                "/api/auth/refresh"
            };

      return publicPaths.Any(publicPath =>
          path.Value?.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
      var authHeader = request.Headers["Authorization"].FirstOrDefault();
      if (string.IsNullOrEmpty(authHeader))
        return null;

      // Bearer token format: "Bearer {token}"
      if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      {
        return authHeader.Substring(7); // Remove "Bearer " prefix
      }

      return null;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var validationParameters = new TokenValidationParameters
        {
          ValidateIssuer = _jwtSettings.ValidateIssuer,
          ValidateAudience = _jwtSettings.ValidateAudience,
          ValidateLifetime = _jwtSettings.ValidateLifetime,
          ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
          ValidIssuer = _jwtSettings.Issuer,
          ValidAudience = _jwtSettings.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ClockSkew = TimeSpan.FromSeconds(_jwtSettings.ClockSkewSeconds)
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

        // Additional validation: ensure it's a JWT token
        if (validatedToken is JwtSecurityToken jwtToken &&
            jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
        {
          return principal;
        }

        return null;
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Token validation failed");
        return null;
      }
    }

    private static async Task HandleUnauthorized(HttpContext context, string message)
    {
      context.Response.StatusCode = 401;
      context.Response.ContentType = "application/json";

      var errorResponse = new
      {
        type = "unauthorized",
        title = "Unauthorized",
        status = 401,
        detail = message,
        instance = context.Request.Path.Value,
        traceId = context.TraceIdentifier,
        timestamp = DateTime.UtcNow
      };

      await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse,
          new System.Text.Json.JsonSerializerOptions
          {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
          }));
    }
  }

  // JWT Token Service for generating tokens
  public interface IJwtTokenService
  {
    string GenerateToken(UserInfo userInfo);
    ClaimsPrincipal? ValidateToken(string token);
    bool IsTokenExpired(string token);
  }

  public class JwtTokenService : IJwtTokenService
  {
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(JwtSettings jwtSettings, ILogger<JwtTokenService> logger)
    {
      _jwtSettings = jwtSettings;
      _logger = logger;
    }

    public string GenerateToken(UserInfo userInfo)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

      var claims = new List<Claim>
            {
                new(TokenClaims.UserId, userInfo.Id),
                new(TokenClaims.Email, userInfo.Email),
                new(TokenClaims.FirstName, userInfo.FirstName),
                new(TokenClaims.LastName, userInfo.LastName),
                new(TokenClaims.Jti, Guid.NewGuid().ToString()),
                new(TokenClaims.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

      // Add roles
      foreach (var role in userInfo.Roles)
      {
        claims.Add(new Claim(TokenClaims.Roles, role));
      }

      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
        Issuer = _jwtSettings.Issuer,
        Audience = _jwtSettings.Audience,
        SigningCredentials = new SigningCredentials(
              new SymmetricSecurityKey(key),
              SecurityAlgorithms.HmacSha256Signature)
      };

      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var validationParameters = new TokenValidationParameters
        {
          ValidateIssuer = _jwtSettings.ValidateIssuer,
          ValidateAudience = _jwtSettings.ValidateAudience,
          ValidateLifetime = _jwtSettings.ValidateLifetime,
          ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
          ValidIssuer = _jwtSettings.Issuer,
          ValidAudience = _jwtSettings.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ClockSkew = TimeSpan.FromSeconds(_jwtSettings.ClockSkewSeconds)
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Token validation failed");
        return null;
      }
    }

    public bool IsTokenExpired(string token)
    {
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo < DateTime.UtcNow;
      }
      catch
      {
        return true; // If we can't read the token, consider it expired
      }
    }
  }

  // Extension method to register the middleware
  public static class JwtAuthenticationMiddlewareExtensions
  {
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
  }
}
