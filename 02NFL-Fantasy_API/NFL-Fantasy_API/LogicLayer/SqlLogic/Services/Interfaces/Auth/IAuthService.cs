using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth
{
    /// <summary>
    /// Servicio de autenticacion y gestion de sesiones
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
        /// Inicia sesion y genera un SessionID (Bearer token)
        /// SP: app.sp_Login (usa OUTPUT params)
        /// Feature 1.1 - Inicio de sesion
        /// </summary>
        /// <param name="dto">Email y contrasena</param>
        /// <returns>SessionID si exitoso, mensaje de error si falla</returns>
        Task<ApiResponseDTO> LoginAsync(LoginDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Valida y refresca una sesion existente (sliding expiration)
        /// SP: app.sp_ValidateAndRefreshSession (usa OUTPUT params)
        /// Usado por: AuthenticationMiddleware
        /// </summary>
        /// <param name="sessionId">GUID del Bearer token</param>
        /// <returns>IsValid y UserID si es valida</returns>
        Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId);

        /// <summary>
        /// Cierra una sesion especifica
        /// SP: app.sp_Logout
        /// Feature 1.1 - Cierre de sesion
        /// </summary>
        /// <param name="sessionId">GUID de la sesion a cerrar</param>
        /// <returns>Mensaje de confirmacion</returns>
        Task<ApiResponseDTO> LogoutAsync(Guid sessionId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Cierra todas las sesiones activas de un usuario
        /// SP: app.sp_LogoutAllSessions
        /// Feature 1.1 - Cierre de sesion global
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Mensaje de confirmacion</returns>
        Task<ApiResponseDTO> LogoutAllAsync(int userId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Solicita un token de restablecimiento de contrasena
        /// SP: app.sp_RequestPasswordReset
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// </summary>
        /// <param name="dto">Email del usuario</param>
        /// <returns>Token y fecha de expiracion (o mensaje generico si el email no existe)</returns>
        Task<ApiResponseDTO> RequestPasswordResetAsync(RequestPasswordResetDTO dto, string? sourceIp = null);

        /// <summary>
        /// Restablece contrasena usando un token valido
        /// SP: app.sp_ResetPasswordWithToken
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// Desbloquea la cuenta, resetea contador de fallos, invalida sesiones
        /// </summary>
        /// <param name="dto">Token, nueva contrasena y confirmacion</param>
        /// <returns>Mensaje de confirmacion o error</returns>
        Task<ApiResponseDTO> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDTO dto, string? sourceIp = null, string? userAgent = null);
    }
}
