using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de autenticación y gestión de sesiones
    /// Mapea a SPs: sp_RegisterUser, sp_Login, sp_ValidateAndRefreshSession, 
    /// sp_Logout, sp_LogoutAllSessions, sp_RequestPasswordReset, sp_ResetPasswordWithToken
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// SP: app.sp_RegisterUser
        /// Feature 1.1 - Registro de usuario
        /// </summary>
        /// <param name="dto">Datos del nuevo usuario</param>
        /// <returns>Respuesta con UserID creado</returns>
        Task<ApiResponseDTO> RegisterAsync(RegisterUserDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Inicia sesión y genera un SessionID (Bearer token)
        /// SP: app.sp_Login (usa OUTPUT params)
        /// Feature 1.1 - Inicio de sesión
        /// </summary>
        /// <param name="dto">Email y contraseña</param>
        /// <returns>SessionID si exitoso, mensaje de error si falla</returns>
        Task<ApiResponseDTO> LoginAsync(LoginDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Valida y refresca una sesión existente (sliding expiration)
        /// SP: app.sp_ValidateAndRefreshSession (usa OUTPUT params)
        /// Usado por: AuthenticationMiddleware
        /// </summary>
        /// <param name="sessionId">GUID del Bearer token</param>
        /// <returns>IsValid y UserID si es válida</returns>
        Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId);

        /// <summary>
        /// Cierra una sesión específica
        /// SP: app.sp_Logout
        /// Feature 1.1 - Cierre de sesión
        /// </summary>
        /// <param name="sessionId">GUID de la sesión a cerrar</param>
        /// <returns>Mensaje de confirmación</returns>
        Task<ApiResponseDTO> LogoutAsync(Guid sessionId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Cierra todas las sesiones activas de un usuario
        /// SP: app.sp_LogoutAllSessions
        /// Feature 1.1 - Cierre de sesión global
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Mensaje de confirmación</returns>
        Task<ApiResponseDTO> LogoutAllAsync(int userId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Solicita un token de restablecimiento de contraseña
        /// SP: app.sp_RequestPasswordReset
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// </summary>
        /// <param name="dto">Email del usuario</param>
        /// <returns>Token y fecha de expiración (o mensaje genérico si el email no existe)</returns>
        Task<ApiResponseDTO> RequestPasswordResetAsync(RequestPasswordResetDTO dto, string? sourceIp = null);

        /// <summary>
        /// Restablece contraseña usando un token válido
        /// SP: app.sp_ResetPasswordWithToken
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// Desbloquea la cuenta, resetea contador de fallos, invalida sesiones
        /// </summary>
        /// <param name="dto">Token, nueva contraseña y confirmación</param>
        /// <returns>Mensaje de confirmación o error</returns>
        Task<ApiResponseDTO> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Validación de complejidad de contraseña según reglas del sistema
        /// Reglas: 8-12 caracteres, alfanumérica, al menos 1 mayúscula, 1 minúscula, 1 dígito
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Lista de errores (vacía si es válida)</returns>
        List<string> ValidatePasswordComplexity(string password);
    }
}