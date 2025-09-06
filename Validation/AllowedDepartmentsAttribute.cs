using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Validation
{
  public class AllowedDepartmentsAttribute : ValidationAttribute
  {
    private readonly string[] _allowedDepartments = {
            "IT", "HR", "Finance", "Marketing", "Sales", "Operations",
            "Legal", "R&D", "Customer Service", "Administration"
        };

    public override bool IsValid(object? value)
    {
      if (value == null) return true; // Let Required attribute handle null values

      string department = value.ToString() ?? string.Empty;
      return _allowedDepartments.Contains(department, StringComparer.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
      return $"{name} must be one of the following: {string.Join(", ", _allowedDepartments)}";
    }
  }
}
