using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Auth;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.Models.ViewModels.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Auth
{
    /// <summary>
    /// Implementación del servicio de roles del sistema.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a SystemRolesDataAccess).
    /// </summary>
    public class SystemRolesService : ISystemRolesService
    {
        private readonly SystemRolesDataAccess _dataAccess;
        private readonly ILogger<SystemRolesService> _logger;

        public SystemRolesService(
            SystemRolesDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<SystemRolesService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Get Roles

        /// <summary>
        /// Obtiene todos los roles disponibles en el sistema.
        /// </summary>
        public async Task<List<SystemRoleDTO>> GetRolesAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles del sistema");
                throw;
            }
        }

        #endregion

        #region Change User Role

        /// <summary>
        /// Cambia el rol de sistema de un usuario.
        /// </summary>
        public async Task<ChangeUserRoleResultDTO> ChangeUserRoleAsync(
            int actorUserId,
            int targetUserId,
            ChangeUserSystemRoleDTO dto,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Lógica de negocio básica
                if (string.IsNullOrWhiteSpace(dto.NewRoleCode))
                {
                    throw new ArgumentException("El código de rol es requerido.");
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.ChangeUserRoleAsync(
                    actorUserId,
                    targetUserId,
                    dto,
                    sourceIp,
                    userAgent);

                if (result == null)
                {
                    // Fallback si el SP no retorna datos (no debería pasar)
                    return new ChangeUserRoleResultDTO
                    {
                        UserID = targetUserId,
                        OldRoleCode = string.Empty,
                        NewRoleCode = dto.NewRoleCode,
                        Message = "Rol del sistema actualizado."
                    };
                }

                _logger.LogInformation(
                    "User {ActorUserId} changed role of User {TargetUserId} from {OldRole} to {NewRole}",
                    actorUserId,
                    targetUserId,
                    result.OldRoleCode,
                    result.NewRoleCode
                );

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al cambiar rol: Actor={ActorUserId}, Target={TargetUserId}, NewRole={NewRole}",
                    actorUserId,
                    targetUserId,
                    dto.NewRoleCode
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar rol: Actor={ActorUserId}, Target={TargetUserId}",
                    actorUserId,
                    targetUserId
                );
                throw;
            }
        }

        #endregion

        #region Role Change History

        /// <summary>
        /// Obtiene el historial de cambios de rol de un usuario.
        /// </summary>
        public async Task<List<UserRoleChangeHistoryDTO>> GetUserRoleChangesAsync(
            int actorUserId,
            int targetUserId,
            int top = 50)
        {
            try
            {
                // VALIDACIÓN: Lógica de negocio
                if (top < 1) top = 50;
                if (top > 200) top = 200;

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserRoleChangesAsync(actorUserId, targetUserId, top);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al obtener historial de roles: Actor={ActorUserId}, Target={TargetUserId}",
                    actorUserId,
                    targetUserId
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener historial de roles: Target={TargetUserId}",
                    targetUserId
                );
                throw;
            }
        }

        #endregion

        #region Users By Role (Paginado)

        /// <summary>
        /// Obtiene listado paginado de usuarios filtrado por rol.
        /// </summary>
        public async Task<UsersByRolePageDTO> GetUsersBySystemRoleAsync(
            int actorUserId,
            string? filterRole = null,
            string? searchTerm = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            try
            {
                // VALIDACIÓN: Lógica de negocio
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 50;
                if (pageSize > 100) pageSize = 100;

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUsersBySystemRoleAsync(
                    actorUserId,
                    filterRole,
                    searchTerm,
                    pageNumber,
                    pageSize
                );
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al obtener usuarios por rol: Actor={ActorUserId}, FilterRole={FilterRole}",
                    actorUserId,
                    filterRole
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener usuarios por rol: FilterRole={FilterRole}",
                    filterRole
                );
                throw;
            }
        }

        #endregion

        #region Get System Roles View

        /// <summary>
        /// Obtiene lista de roles del sistema desde VIEW.
        /// VIEW: vw_SystemRoles
        /// </summary>
        public async Task<List<SystemRoleVM>> GetSystemRolesAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetSystemRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles del sistema desde VIEW");
                throw;
            }
        }

        #endregion

        #region Get Users With Roles View

        /// <summary>
        /// Obtiene usuarios con roles completos desde VIEW.
        /// VIEW: vw_UsersWithRoles
        /// </summary>
        public async Task<List<UserWithFullRoleVM>> GetUsersWithRolesAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUsersWithRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios con roles completos");
                throw;
            }
        }

        #endregion
    }
}