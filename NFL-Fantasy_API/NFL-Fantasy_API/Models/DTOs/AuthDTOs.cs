using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// DTO para registro de usuario (Feature 1.1 - Registro)
    /// Validaciones: 
    /// - Nombre: 1-50 caracteres
    /// - Email: máx 50 caracteres, formato válido, único
    /// - Contraseña: 8-12 caracteres, alfanumérica, al menos 1 mayúscula, 1 minúscula, 1 dígito
    /// - Alias: máx 50 caracteres (opcional)
    /// - Imagen: JPEG/PNG, máx 5MB, 300x300 a 1024x1024 px
    /// </summary>
    public class RegisterUserDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        [StringLength(50, ErrorMessage = "El correo no puede superar 50 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El alias no puede superar 50 caracteres.")]
        public string? Alias { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 12 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string PasswordConfirm { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "El código de idioma no puede superar 10 caracteres.")]
        public string LanguageCode { get; set; } = "en";

        // Campos de imagen de perfil (opcionales)
        [StringLength(400, ErrorMessage = "La URL de imagen no puede superar 400 caracteres.")]
        public string? ProfileImageUrl { get; set; }

        [Range(300, 1024, ErrorMessage = "El ancho de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? ProfileImageWidth { get; set; }

        [Range(300, 1024, ErrorMessage = "El alto de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? ProfileImageHeight { get; set; }

        [Range(1, 5242880, ErrorMessage = "El tamaño de imagen debe estar entre 1 byte y 5MB (5,242,880 bytes).")]
        public int? ProfileImageBytes { get; set; }
    }

    /// <summary>
    /// Respuesta del registro exitoso
    /// </summary>
    public class RegisterResponseDTO
    {
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para inicio de sesión (Feature 1.1 - Login)
    /// </summary>
    public class LoginDTO
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta del inicio de sesión exitoso
    /// </summary>
    public class LoginResponseDTO
    {
        public Guid SessionID { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de validación de sesión (usado por middleware)
    /// </summary>
    public class SessionValidationDTO
    {
        public bool IsValid { get; set; }
        public int UserID { get; set; }
    }

    /// <summary>
    /// DTO para solicitud de restablecimiento de contraseña (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class RequestPasswordResetDTO
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de solicitud de reset
    /// </summary>
    public class PasswordResetRequestResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = "Si el correo existe, se ha enviado un enlace de restablecimiento.";
    }

    /// <summary>
    /// DTO para restablecer contraseña con token (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class ResetPasswordWithTokenDTO
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 12 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para cerrar sesión (simple, el SessionID viene del Bearer token)
    /// </summary>
    public class LogoutDTO
    {
        // Vacío intencionalmente, el SessionID se toma del contexto HTTP
    }

    /// <summary>
    /// DTO para cerrar todas las sesiones (Feature 1.1 - Cierre de sesión global)
    /// </summary>
    public class LogoutAllDTO
    {
        // Vacío intencionalmente, el UserID se toma del contexto HTTP
    }
}