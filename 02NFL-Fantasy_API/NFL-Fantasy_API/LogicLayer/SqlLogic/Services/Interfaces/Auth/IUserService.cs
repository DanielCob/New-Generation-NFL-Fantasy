using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.Models.ViewModels.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth
{
    /// <summary>
    /// Servicio de gestión de perfiles de usuario.
    /// 
    /// STORED PROCEDURES:
    /// - app.sp_UpdateUserProfile - Actualiza perfil de usuario
    /// - app.sp_GetUserProfile - Obtiene perfil completo (3 result sets)
    /// 
    /// VIEWS:
    /// - vw_UserProfileHeader - Información básica para header
    /// - vw_UserActiveSessions - Sesiones activas del usuario
    /// - vw_UserProfileBasic - Perfil básico completo
    /// 
    /// Feature 1.1: Gestión de perfiles de usuarios
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Actualiza el perfil de un usuario.
        /// </summary>
        /// <param name="actorUserId">ID del usuario que realiza el cambio</param>
        /// <param name="targetUserId">ID del usuario a modificar</param>
        /// <param name="dto">Campos a actualizar (todos opcionales)</param>
        /// <param name="sourceIp">IP desde donde se realiza la actualización</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Respuesta con resultado de la actualización</returns>
        /// <remarks>
        /// SP: app.sp_UpdateUserProfile
        /// 
        /// CAMPOS EDITABLES:
        /// - Username, FirstName, LastName
        /// - Bio, Avatar
        /// - Preferencias de notificaciones
        /// 
        /// CAMPOS NO EDITABLES:
        /// - Email (requiere verificación separada)
        /// - UserID, CreatedAt (inmutables)
        /// - AccountStatus, SystemRole (solo admin)
        /// </remarks>
        Task<ApiResponseDTO> UpdateProfileAsync(
            int actorUserId,
            int targetUserId,
            UpdateUserProfileDTO dto,
            string sourceIp,
            string userAgent);

        /// <summary>
        /// Obtiene el perfil completo de un usuario.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Perfil completo con ligas y equipos, o null si no existe</returns>
        /// <remarks>
        /// SP: app.sp_GetUserProfile (retorna 3 result sets)
        /// 
        /// INCLUYE:
        /// - Datos del usuario
        /// - Ligas donde es comisionado
        /// - Equipos que posee en diferentes ligas
        /// </remarks>
        Task<UserProfileResponseDTO?> GetUserProfileAsync(int userId);

        /// <summary>
        /// Obtiene información básica de encabezado del perfil.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Información básica para mostrar en header/navbar, o null si no existe</returns>
        /// <remarks>
        /// VIEW: vw_UserProfileHeader
        /// 
        /// Versión ligera del perfil para uso frecuente en UI.
        /// </remarks>
        Task<UserProfileHeaderVM?> GetUserHeaderAsync(int userId);

        /// <summary>
        /// Obtiene todas las sesiones activas de un usuario.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de sesiones activas (vacía si no hay sesiones)</returns>
        /// <remarks>
        /// VIEW: vw_UserActiveSessions
        /// 
        /// Útil para ver desde qué dispositivos está conectado el usuario.
        /// Feature 1.1: Ver sesiones activas
        /// </remarks>
        Task<List<UserActiveSessionVM>> GetActiveSessionsAsync(int userId);

        /// <summary>
        /// Obtiene perfil básico completo desde VIEW.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Perfil básico con todos los campos visibles, o null si no existe</returns>
        /// <remarks>
        /// VIEW: vw_UserProfileBasic
        /// 
        /// Alternativa a sp_GetUserProfile cuando no se necesitan
        /// los result sets adicionales (ligas y equipos).
        /// </remarks>
        Task<UserProfileBasicVM?> GetUserBasicAsync(int userId);

        /// <summary>
        /// Obtiene todos los usuarios activos del sistema.
        /// VIEW: vw_UserProfileBasic con WHERE AccountStatus=1
        /// Para reportes administrativos.
        /// </summary>
        /// <returns>Lista de usuarios activos</returns>
        Task<List<UserProfileBasicVM>> GetActiveUsersAsync();
    }
}