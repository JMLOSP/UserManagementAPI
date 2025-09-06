using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models.Auth
{
  public class LoginRequest
  {
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; } = string.Empty;
  }

  public class LoginResponse
  {
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
  }

  public class UserInfo
  {
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
  }

  public class TokenClaims
  {
    public const string UserId = "sub";
    public const string Email = "email";
    public const string FirstName = "given_name";
    public const string LastName = "family_name";
    public const string Roles = "roles";
    public const string Jti = "jti";
    public const string Iat = "iat";
    public const string Exp = "exp";
    public const string Iss = "iss";
    public const string Aud = "aud";
  }

  public class JwtSettings
  {
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "TechHive.UserManagementAPI";
    public string Audience { get; set; } = "TechHive.UserManagementAPI.Users";
    public int ExpirationMinutes { get; set; } = 60;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 30;
  }
}
