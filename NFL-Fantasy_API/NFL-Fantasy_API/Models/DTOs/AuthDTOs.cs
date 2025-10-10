using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// DTO para registro de usuario (Feature 1.1 - Registro)
    /// Validaciones: 
    /// - Nombre: 1-50 caracteres
    /// - Email: m�x 50 caracteres, formato v�lido, �nico
    /// - Contrase�a: 8-12 caracteres, alfanum�rica, al menos 1 may�scula, 1 min�scula, 1 d�gito
    /// - Alias: m�x 50 caracteres (opcional)
    /// - Imagen: JPEG/PNG, m�x 5MB, 300x300 a 1024x1024 px
    /// </summary>
    public class RegisterUserDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electr�nico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electr�nico inv�lido.")]
        [StringLength(50, ErrorMessage = "El correo no puede superar 50 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El alias no puede superar 50 caracteres.")]
        public string? Alias { get; set; }

        [Required(ErrorMessage = "La contrase�a es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contrase�a debe tener entre 8 y 12 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmaci�n de contrase�a es obligatoria.")]
        [Compare("Password", ErrorMessage = "Las contrase�as no coinciden.")]
        public string PasswordConfirm { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "El c�digo de idioma no puede superar 10 caracteres.")]
        public string LanguageCode { get; set; } = "en";

        // Campos de imagen de perfil (opcionales)
        [StringLength(400, ErrorMessage = "La URL de imagen no puede superar 400 caracteres.")]
        public string? ProfileImageUrl { get; set; }

        [Range(300, 1024, ErrorMessage = "El ancho de imagen debe estar entre 300 y 1024 p�xeles.")]
        public short? ProfileImageWidth { get; set; }

        [Range(300, 1024, ErrorMessage = "El alto de imagen debe estar entre 300 y 1024 p�xeles.")]
        public short? ProfileImageHeight { get; set; }

        [Range(1, 5242880, ErrorMessage = "El tama�o de imagen debe estar entre 1 byte y 5MB (5,242,880 bytes).")]
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
    /// DTO para inicio de sesi�n (Feature 1.1 - Login)
    /// </summary>
    public class LoginDTO
    {
        [Required(ErrorMessage = "El correo electr�nico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electr�nico inv�lido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrase�a es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta del inicio de sesi�n exitoso
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
    /// Respuesta de validaci�n de sesi�n (usado por middleware)
    /// </summary>
    public class SessionValidationDTO
    {
        public bool IsValid { get; set; }
        public int UserID { get; set; }
    }

    /// <summary>
    /// DTO para solicitud de restablecimiento de contrase�a (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class RequestPasswordResetDTO
    {
        [Required(ErrorMessage = "El correo electr�nico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo electr�nico inv�lido.")]
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
    /// DTO para restablecer contrase�a con token (Feature 1.1 - Desbloqueo)
    /// </summary>
    public class ResetPasswordWithTokenDTO
    {
        [Required(ErrorMessage = "El token es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contrase�a es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contrase�a debe tener entre 8 y 12 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmaci�n de contrase�a es obligatoria.")]
        [Compare("NewPassword", ErrorMessage = "Las contrase�as no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para cerrar sesi�n (simple, el SessionID viene del Bearer token)
    /// </summary>
    public class LogoutDTO
    {
        // Vac�o intencionalmente, el SessionID se toma del contexto HTTP
    }

    /// <summary>
    /// DTO para cerrar todas las sesiones (Feature 1.1 - Cierre de sesi�n global)
    /// </summary>
    public class LogoutAllDTO
    {
        // Vac�o intencionalmente, el UserID se toma del contexto HTTP
    }
}