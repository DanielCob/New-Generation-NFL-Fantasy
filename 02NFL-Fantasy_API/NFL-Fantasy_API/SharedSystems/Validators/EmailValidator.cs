// SharedSystems/Validators/EmailValidator.cs
using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.SharedSystems.Validators
{
    /// <summary>
    /// Validador centralizado de emails.
    /// </summary>
    public static class EmailValidator
    {
        public static bool IsValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}