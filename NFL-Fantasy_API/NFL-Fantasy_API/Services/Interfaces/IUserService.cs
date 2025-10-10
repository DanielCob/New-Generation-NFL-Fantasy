using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de gestión de perfiles de usuario
    /// Mapea a: sp_UpdateUserProfile, sp_GetUserProfile, vw_UserProfileHeader, 
    /// vw_UserActiveSessions, vw_UserProfileBasic
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Actualiza el perfil de un usuario
        /// SP: app.sp_UpdateUserProfile
        /// Feature 1.1 - Gestión de perfil de usuario
        /// IMPORTANTE: No permite editar Email, UserID, CreatedAt, AccountStatus, Role
        /// </summary>
        /// <param name="actorUserId">ID del usuario que realiza el cambio (normalmente el mismo)</param>
        /// <param name="targetUserId">ID del usuario a modificar</param>
        /// <param name="dto">Campos a actualizar (todos opcionales)</param>
        /// <returns>Mensaje de confirmación</returns>
        Task<ApiResponseDTO> UpdateProfileAsync(int actorUserId, int targetUserId, UpdateUserProfileDTO dto);

        /// <summary>
        /// Obtiene el perfil completo de un usuario
        /// SP: app.sp_GetUserProfile (retorna 3 result sets)
        /// Feature 1.1 - Ver perfil de usuario
        /// Incluye: datos del usuario + ligas como comisionado + equipos del usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Perfil completo con ligas y equipos</returns>
        Task<UserProfileResponseDTO?> GetUserProfileAsync(int userId);

        /// <summary>
        /// Obtiene información básica de encabezado del perfil
        /// VIEW: vw_UserProfileHeader
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Información básica del perfil</returns>
        Task<UserProfileHeaderVM?> GetUserHeaderAsync(int userId);

        /// <summary>
        /// Obtiene todas las sesiones activas de un usuario
        /// VIEW: vw_UserActiveSessions
        /// Feature 1.1 - Ver sesiones activas (para logout selectivo futuro)
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de sesiones activas</returns>
        Task<List<UserActiveSessionVM>> GetActiveSessionsAsync(int userId);

        /// <summary>
        /// Obtiene perfil básico completo desde VIEW
        /// VIEW: vw_UserProfileBasic
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Perfil básico con todos los campos visibles</returns>
        Task<UserProfileBasicVM?> GetUserBasicAsync(int userId);
    }
}