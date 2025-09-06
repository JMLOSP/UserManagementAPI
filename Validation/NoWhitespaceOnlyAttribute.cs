using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Validation
{
  public class NoWhitespaceOnlyAttribute : ValidationAttribute
  {
    public override bool IsValid(object? value)
    {
      if (value == null) return true; // Let Required attribute handle null values

      string stringValue = value.ToString() ?? string.Empty;
      return !string.IsNullOrWhiteSpace(stringValue);
    }

    public override string FormatErrorMessage(string name)
    {
      return $"{name} cannot be empty or contain only whitespace";
    }
  }
}
