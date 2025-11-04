using NFL_Fantasy_API.SharedSystems.Validators;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.Models.ViewModels.Auth;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Auth;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Auth
{
    /// <summary>
    /// Implementación del servicio de gestión de usuarios.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a UserDataAccess).
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserDataAccess _dataAccess;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<UserService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Update Profile

        /// <summary>
        /// Actualiza el perfil de un usuario.
        /// SP: app.sp_UpdateUserProfile
        /// </summary>
        public async Task<ApiResponseDTO> UpdateProfileAsync(
            int actorUserId,
            int targetUserId,
            UpdateUserProfileDTO dto,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a ProfileImageValidator
                var imageErrors = ProfileImageValidator.ValidateProfileImage(
                    dto.ProfileImageWidth,
                    dto.ProfileImageHeight,
                    dto.ProfileImageBytes
                );

                if (imageErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", imageErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.UpdateProfileAsync(
                    actorUserId,
                    targetUserId,
                    dto,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} updated profile of User {TargetUserId}",
                    actorUserId,
                    targetUserId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al actualizar perfil: Actor={ActorUserId}, Target={TargetUserId}",
                    actorUserId,
                    targetUserId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar perfil: Target={TargetUserId}",
                    targetUserId
                );
                return ApiResponseDTO.ErrorResponse($"Error al actualizar perfil: {ex.Message}");
            }
        }

        #endregion

        #region Get Profile

        /// <summary>
        /// Obtiene el perfil completo de un usuario.
        /// SP: app.sp_GetUserProfile (3 result sets)
        /// </summary>
        public async Task<UserProfileResponseDTO?> GetUserProfileAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserProfileAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil de usuario {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region View Queries

        /// <summary>
        /// Obtiene encabezado del perfil.
        /// VIEW: vw_UserProfileHeader
        /// </summary>
        public async Task<UserProfileHeaderVM?> GetUserHeaderAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserHeaderAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener header de usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene sesiones activas de un usuario.
        /// VIEW: vw_UserActiveSessions
        /// </summary>
        public async Task<List<UserActiveSessionVM>> GetActiveSessionsAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetActiveSessionsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sesiones activas de usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene perfil básico desde VIEW.
        /// VIEW: vw_UserProfileBasic
        /// </summary>
        public async Task<UserProfileBasicVM?> GetUserBasicAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserBasicAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil básico de usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los usuarios activos del sistema.
        /// VIEW: vw_UserProfileBasic con WHERE AccountStatus=1
        /// </summary>
        public async Task<List<UserProfileBasicVM>> GetActiveUsersAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetActiveUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios activos");
                throw;
            }
        }

        #endregion
    }
}