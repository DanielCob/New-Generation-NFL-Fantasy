// SharedSystems/Validators/PasswordValidator.cs
using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.SharedSystems.Validators
{
    /// <summary>
    /// Validador centralizado de contraseñas del sistema.
    /// Reglas: 8-12 caracteres, alfanumérica, al menos 1 mayúscula, 1 minúscula, 1 dígito
    /// </summary>
    public static class AuthPasswordValidator
    {
        public static List<string> ValidateComplexity(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("La contrasena es obligatoria.");
                return errors;
            }

            if (password.Length < 8 || password.Length > 12)
                errors.Add("La contrasena debe tener entre 8 y 12 caracteres.");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("La contrasena debe incluir al menos una letra mayuscula.");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("La contrasena debe incluir al menos una letra minuscula.");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("La contrasena debe incluir al menos un digito.");

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
                errors.Add("La contrasena debe ser alfanumerica (solo letras y numeros, sin caracteres especiales).");

            return errors;
        }
    }
}