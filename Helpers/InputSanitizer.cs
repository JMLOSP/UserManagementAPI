using System.Text.RegularExpressions;

namespace UserManagementAPI.Helpers
{
  public static class InputSanitizer
  {
    /// <summary>
    /// Sanitizes input by trimming whitespace and removing potential security threats
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>Sanitized string</returns>
    public static string SanitizeString(string? input)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;

      // Trim whitespace
      string sanitized = input.Trim();

      // Remove null characters and control characters
      sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

      // Normalize multiple spaces to single space
      sanitized = Regex.Replace(sanitized, @"\s+", " ");

      return sanitized;
    }

    /// <summary>
    /// Sanitizes name input (first name, last name) with specific rules
    /// </summary>
    /// <param name="name">The name to sanitize</param>
    /// <returns>Sanitized name</returns>
    public static string SanitizeName(string? name)
    {
      if (string.IsNullOrEmpty(name))
        return string.Empty;

      string sanitized = SanitizeString(name);

      // Remove any characters that aren't letters, spaces, hyphens, apostrophes, or periods
      sanitized = Regex.Replace(sanitized, @"[^a-zA-Z\s\-'\.]+", "");

      // Ensure proper capitalization (first letter uppercase, rest lowercase)
      if (!string.IsNullOrEmpty(sanitized))
      {
        sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1).ToLower();
      }

      return sanitized;
    }

    /// <summary>
    /// Sanitizes email input
    /// </summary>
    /// <param name="email">The email to sanitize</param>
    /// <returns>Sanitized email</returns>
    public static string SanitizeEmail(string? email)
    {
      if (string.IsNullOrEmpty(email))
        return string.Empty;

      // Trim and convert to lowercase
      string sanitized = email.Trim().ToLowerInvariant();

      // Remove null characters and control characters
      sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

      return sanitized;
    }

    /// <summary>
    /// Sanitizes phone number input
    /// </summary>
    /// <param name="phoneNumber">The phone number to sanitize</param>
    /// <returns>Sanitized phone number</returns>
    public static string SanitizePhoneNumber(string? phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber))
        return string.Empty;

      string sanitized = SanitizeString(phoneNumber);

      // Keep only digits, spaces, hyphens, parentheses, periods, and plus sign
      sanitized = Regex.Replace(sanitized, @"[^\d\s\-\(\)\.+]", "");

      return sanitized;
    }

    /// <summary>
    /// Sanitizes department or position input
    /// </summary>
    /// <param name="text">The text to sanitize</param>
    /// <returns>Sanitized text</returns>
    public static string SanitizeDepartmentOrPosition(string? text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      string sanitized = SanitizeString(text);

      // Remove any characters that aren't letters, spaces, hyphens, ampersands, or periods
      sanitized = Regex.Replace(sanitized, @"[^a-zA-Z\s\-&\.]+", "");

      // Proper case for each word
      if (!string.IsNullOrEmpty(sanitized))
      {
        var words = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        sanitized = string.Join(" ", words.Select(word =>
            char.ToUpper(word[0]) + word.Substring(1).ToLower()));
      }

      return sanitized;
    }
  }
}
