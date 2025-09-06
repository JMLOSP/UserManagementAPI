using System.ComponentModel.DataAnnotations;
using UserManagementAPI.Validation;

namespace UserManagementAPI.DTOs
{
  public class CreateUserDto
  {
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [NoWhitespaceOnly]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [NoWhitespaceOnly]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please provide a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [RegularExpression(@"^[\+]?[1-9][\d\-\s\(\)\.]+$", ErrorMessage = "Please provide a valid phone number format")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Department is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-&]+$", ErrorMessage = "Department can only contain letters, spaces, hyphens, and ampersands")]
    [AllowedDepartments]
    [NoWhitespaceOnly]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-&\.]+$", ErrorMessage = "Position can only contain letters, spaces, hyphens, ampersands, and periods")]
    [NoWhitespaceOnly]
    public string Position { get; set; } = string.Empty;
  }

  public class UpdateUserDto
  {
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [NoWhitespaceOnly]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, apostrophes, and periods")]
    [NoWhitespaceOnly]
    public string? LastName { get; set; }

    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Please provide a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [RegularExpression(@"^[\+]?[1-9][\d\-\s\(\)\.]+$", ErrorMessage = "Please provide a valid phone number format")]
    public string? PhoneNumber { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-&]+$", ErrorMessage = "Department can only contain letters, spaces, hyphens, and ampersands")]
    [AllowedDepartments]
    [NoWhitespaceOnly]
    public string? Department { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Position must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-&\.]+$", ErrorMessage = "Position can only contain letters, spaces, hyphens, ampersands, and periods")]
    [NoWhitespaceOnly]
    public string? Position { get; set; }

    public bool? IsActive { get; set; }
  }

  public class UserDto
  {
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public bool IsActive { get; set; }
  }
}
