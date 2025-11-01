using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Auth
{
    /// <summary>
    /// DTO para registro de usuario (Feature 1.1 - Registro)
    /// Validaciones: 
    /// - Nombre: 1-50 caracteres
    /// - Email: max 50 caracteres, formato valido, unico
    /// - Contrasena: 8-12 caracteres, alfanumerica, al menos 1 mayuscula, 1 minuscula, 1 digito
    /// - Alias: max 50 caracteres (opcional)
    /// - Imagen: JPEG/PNG, max 5MB, 300x300 a 1024x1024 px
    /// </summary>
    public class RegisterUserDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electronico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electronico invalido.")]
        [StringLength(50, ErrorMessage = "El correo no puede superar 50 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El alias no puede superar 50 caracteres.")]
        public string? Alias { get; set; }

        [Required(ErrorMessage = "La contrasena es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contrasena debe tener entre 8 y 12 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmacion de contrasena es obligatoria.")]
        [Compare("Password", ErrorMessage = "Las contrasenas no coinciden.")]
        public string PasswordConfirm { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "El codigo de idioma no puede superar 10 caracteres.")]
        public string LanguageCode { get; set; } = "en";

        // Campos de imagen de perfil (opcionales)
        [StringLength(400, ErrorMessage = "La URL de imagen no puede superar 400 caracteres.")]
        public string? ProfileImageUrl { get; set; }

        [Range(300, 1024, ErrorMessage = "El ancho de imagen debe estar entre 300 y 1024 pixeles.")]
        public short? ProfileImageWidth { get; set; }

        [Range(300, 1024, ErrorMessage = "El alto de imagen debe estar entre 300 y 1024 pixeles.")]
        public short? ProfileImageHeight { get; set; }

        [Range(1, 5242880, ErrorMessage = "El tamano de imagen debe estar entre 1 byte y 5MB (5,242,880 bytes).")]
        public int? ProfileImageBytes { get; set; }
    }

    /// <summary>
    /// Respuesta del registro exitoso
    /// </summary>
    public class RegisterResponseDTO
    {
        public int UserID { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para inicio de sesion (Feature 1.1 - Login)
    /// </summary>
    public class LoginDTO
    {
        [Required(ErrorMessage = "El correo electronico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electronico invalido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta del inicio de sesion exitoso
    /// </summary>
    public class LoginResponseDTO
    {
        public Guid SessionID { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SystemRoleCode { get; set; } = "USER";
    }

    /// <summary>
    /// Respuesta de validacion de sesion (usado por middleware)
    /// </summary>
    public class SessionValidationDTO
    {
        public bool IsValid { get; set; }
        public int UserID { get; set; }
    }

    /// <summary>
    /// DTO para solicitud de restablecimiento de contrasena (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class RequestPasswordResetDTO
    {
        [Required(ErrorMessage = "El correo electronico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electronico invalido.")]
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
    /// DTO para restablecer contrasena con token (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class ResetPasswordWithTokenDTO
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contrasena es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contrasena debe tener entre 8 y 12 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmacion de contrasena es obligatoria.")]
        [Compare("NewPassword", ErrorMessage = "Las contrasenas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para cerrar sesion (simple, el SessionID viene del Bearer token)
    /// </summary>
    public class LogoutDTO
    {
        // Vacio intencionalmente, el SessionID se toma del contexto HTTP
    }

    /// <summary>
    /// DTO para cerrar todas las sesiones (Feature 1.1 - Cierre de sesion global)
    /// </summary>
    public class LogoutAllDTO
    {
        // Vacio intencionalmente, el UserID se toma del contexto HTTP
    }

    public class LoginResultDataAccess
    {
        public bool Success { get; set; }
        public Guid SessionId { get; set; }
        public string? Message { get; set; }
    }

    public class PasswordResetTokenResult
    {
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}