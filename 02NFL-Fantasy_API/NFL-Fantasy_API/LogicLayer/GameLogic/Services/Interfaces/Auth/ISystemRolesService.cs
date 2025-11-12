using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.Models.ViewModels.Auth;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.Auth
{
    /// <summary>
    /// Servicio para gestión de roles del sistema.
    /// </summary>
    public interface ISystemRolesService
    {
        /// <summary>
        /// Obtiene todos los roles disponibles en el sistema.
        /// </summary>
        Task<List<SystemRoleDTO>> GetRolesAsync();

        /// <summary>
        /// Cambia el rol de sistema de un usuario.
        /// </summary>
        Task<ChangeUserRoleResultDTO> ChangeUserRoleAsync(
            int actorUserId,
            int targetUserId,
            ChangeUserSystemRoleDTO dto,
            string? sourceIp = null,
            string? userAgent = null);

        /// <summary>
        /// Obtiene el historial de cambios de rol de un usuario.
        /// </summary>
        Task<List<UserRoleChangeHistoryDTO>> GetUserRoleChangesAsync(
            int actorUserId,
            int targetUserId,
            int top = 50);

        /// <summary>
        /// Obtiene listado paginado de usuarios filtrado por rol.
        /// </summary>
        Task<UsersByRolePageDTO> GetUsersBySystemRoleAsync(
            int actorUserId,
            string? filterRole = null,
            string? searchTerm = null,
            int pageNumber = 1,
            int pageSize = 50);

        /// <summary>
        /// Obtiene lista de roles del sistema desde VIEW.
        /// VIEW: vw_SystemRoles
        /// </summary>
        Task<List<SystemRoleVM>> GetSystemRolesAsync();

        /// <summary>
        /// Obtiene usuarios con roles completos desde VIEW.
        /// VIEW: vw_UsersWithRoles
        /// </summary>
        Task<List<UserWithFullRoleVM>> GetUsersWithRolesAsync();
    }
}