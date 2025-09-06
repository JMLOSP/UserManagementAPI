using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Validation
{
  public class NoSpecialCharactersAttribute : ValidationAttribute
  {
    private readonly string _allowedSpecialChars;

    public NoSpecialCharactersAttribute(string allowedSpecialChars = "")
    {
      _allowedSpecialChars = allowedSpecialChars;
    }

    public override bool IsValid(object? value)
    {
      if (value == null) return true; // Let Required attribute handle null values

      string stringValue = value.ToString() ?? string.Empty;

      // Define forbidden characters (excluding explicitly allowed ones)
      var forbiddenChars = new char[] { '<', '>', '"', '\'', '&', ';', '(', ')', '[', ']', '{', '}', '=', '+', '*', '%', '$', '#', '@', '!', '?', '|', '\\', '/', '^', '~', '`' };
      var allowedChars = _allowedSpecialChars.ToCharArray();
      var actualForbiddenChars = forbiddenChars.Except(allowedChars);

      return !stringValue.Any(c => actualForbiddenChars.Contains(c));
    }

    public override string FormatErrorMessage(string name)
    {
      return $"{name} contains forbidden special characters";
    }
  }
}
