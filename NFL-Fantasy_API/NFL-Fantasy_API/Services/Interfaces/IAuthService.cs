using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de autenticaci�n y gesti�n de sesiones
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
        /// Inicia sesi�n y genera un SessionID (Bearer token)
        /// SP: app.sp_Login (usa OUTPUT params)
        /// Feature 1.1 - Inicio de sesi�n
        /// </summary>
        /// <param name="dto">Email y contrase�a</param>
        /// <returns>SessionID si exitoso, mensaje de error si falla</returns>
        Task<ApiResponseDTO> LoginAsync(LoginDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Valida y refresca una sesi�n existente (sliding expiration)
        /// SP: app.sp_ValidateAndRefreshSession (usa OUTPUT params)
        /// Usado por: AuthenticationMiddleware
        /// </summary>
        /// <param name="sessionId">GUID del Bearer token</param>
        /// <returns>IsValid y UserID si es v�lida</returns>
        Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId);

        /// <summary>
        /// Cierra una sesi�n espec�fica
        /// SP: app.sp_Logout
        /// Feature 1.1 - Cierre de sesi�n
        /// </summary>
        /// <param name="sessionId">GUID de la sesi�n a cerrar</param>
        /// <returns>Mensaje de confirmaci�n</returns>
        Task<ApiResponseDTO> LogoutAsync(Guid sessionId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Cierra todas las sesiones activas de un usuario
        /// SP: app.sp_LogoutAllSessions
        /// Feature 1.1 - Cierre de sesi�n global
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Mensaje de confirmaci�n</returns>
        Task<ApiResponseDTO> LogoutAllAsync(int userId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Solicita un token de restablecimiento de contrase�a
        /// SP: app.sp_RequestPasswordReset
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// </summary>
        /// <param name="dto">Email del usuario</param>
        /// <returns>Token y fecha de expiraci�n (o mensaje gen�rico si el email no existe)</returns>
        Task<ApiResponseDTO> RequestPasswordResetAsync(RequestPasswordResetDTO dto, string? sourceIp = null);

        /// <summary>
        /// Restablece contrase�a usando un token v�lido
        /// SP: app.sp_ResetPasswordWithToken
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// Desbloquea la cuenta, resetea contador de fallos, invalida sesiones
        /// </summary>
        /// <param name="dto">Token, nueva contrase�a y confirmaci�n</param>
        /// <returns>Mensaje de confirmaci�n o error</returns>
        Task<ApiResponseDTO> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDTO dto, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Validaci�n de complejidad de contrase�a seg�n reglas del sistema
        /// Reglas: 8-12 caracteres, alfanum�rica, al menos 1 may�scula, 1 min�scula, 1 d�gito
        /// </summary>
        /// <param name="password">Contrase�a a validar</param>
        /// <returns>Lista de errores (vac�a si es v�lida)</returns>
        List<string> ValidatePasswordComplexity(string password);
    }
}