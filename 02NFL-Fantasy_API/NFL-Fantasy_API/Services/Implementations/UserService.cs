using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de usuarios
    /// Maneja actualización de perfil, consultas de perfil y sesiones
    /// </summary>
    public class UserService : IUserService
    {
        private readonly DatabaseHelper _db;

        public UserService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Update Profile

        /// <summary>
        /// Actualiza el perfil de un usuario
        /// SP: app.sp_UpdateUserProfile
        /// Feature 1.1 - Gestión de perfil de usuario
        /// </summary>
        public async Task<ApiResponseDTO> UpdateProfileAsync(int actorUserId, int targetUserId, UpdateUserProfileDTO dto, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validación de imagen si viene
                if (dto.ProfileImageBytes.HasValue)
                {
                    if (!dto.ProfileImageWidth.HasValue || !dto.ProfileImageHeight.HasValue)
                    {
                        return ApiResponseDTO.ErrorResponse("Si proporciona tamaño de imagen, debe incluir ancho y alto.");
                    }

                    if (dto.ProfileImageWidth.Value < 300 || dto.ProfileImageWidth.Value > 1024)
                    {
                        return ApiResponseDTO.ErrorResponse("El ancho de imagen debe estar entre 300 y 1024 píxeles.");
                    }

                    if (dto.ProfileImageHeight.Value < 300 || dto.ProfileImageHeight.Value > 1024)
                    {
                        return ApiResponseDTO.ErrorResponse("El alto de imagen debe estar entre 300 y 1024 píxeles.");
                    }

                    if (dto.ProfileImageBytes.Value > 5242880) // 5 MB
                    {
                        return ApiResponseDTO.ErrorResponse("El tamaño de imagen no puede superar 5MB.");
                    }
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@TargetUserID", targetUserId),
                    new SqlParameter("@Name", DatabaseHelper.DbNullIfNull(dto.Name)),
                    new SqlParameter("@Alias", DatabaseHelper.DbNullIfNull(dto.Alias)),
                    new SqlParameter("@LanguageCode", DatabaseHelper.DbNullIfNull(dto.LanguageCode)),
                    new SqlParameter("@ProfileImageUrl", DatabaseHelper.DbNullIfNull(dto.ProfileImageUrl)),
                    new SqlParameter("@ProfileImageWidth", DatabaseHelper.DbNullIfNull(dto.ProfileImageWidth)),
                    new SqlParameter("@ProfileImageHeight", DatabaseHelper.DbNullIfNull(dto.ProfileImageHeight)),
                    new SqlParameter("@ProfileImageBytes", DatabaseHelper.DbNullIfNull(dto.ProfileImageBytes)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_UpdateUserProfile",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al actualizar perfil: {ex.Message}");
            }
        }

        #endregion

        #region Get Profile

        /// <summary>
        /// Obtiene el perfil completo de un usuario
        /// SP: app.sp_GetUserProfile (retorna 3 result sets)
        /// Feature 1.1 - Ver perfil de usuario
        /// Result Set 1: Datos del usuario
        /// Result Set 2: Ligas donde es comisionado
        /// Result Set 3: Equipos del usuario
        /// </summary>
        public async Task<UserProfileResponseDTO?> GetUserProfileAsync(int userId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", userId)
                };

                // Necesitamos un approach diferente porque ExecuteStoredProcedureMultipleResultSetsAsync
                // espera todos los mappers del mismo tipo
                // Vamos a usar conexión manual para este caso complejo

                var profile = new UserProfileResponseDTO();

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_GetUserProfile", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // Result Set 1: Perfil del usuario
                    if (await reader.ReadAsync())
                    {
                        profile.UserID = DatabaseHelper.GetSafeInt32(reader, "UserID");
                        profile.Email = DatabaseHelper.GetSafeString(reader, "Email");
                        profile.Name = DatabaseHelper.GetSafeString(reader, "Name");
                        profile.Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias");
                        profile.LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode");
                        profile.ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl");
                        profile.AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus");
                        profile.CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt");
                        profile.UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt");
                        profile.SystemRoleCode = DatabaseHelper.GetSafeString(reader, "SystemRoleCode");
                    }

                    // Result Set 2: Ligas como comisionado
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            profile.CommissionedLeagues.Add(new UserCommissionedLeagueDTO
                            {
                                LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                                LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                                Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                                TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                                RoleCode = DatabaseHelper.GetSafeString(reader, "RoleCode"),
                                IsPrimaryCommissioner = DatabaseHelper.GetSafeBool(reader, "IsPrimaryCommissioner"),
                                JoinedAt = DatabaseHelper.GetSafeDateTime(reader, "JoinedAt")
                            });
                        }
                    }

                    // Result Set 3: Equipos del usuario
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            profile.Teams.Add(new UserTeamDTO
                            {
                                TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                                LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                                LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                                TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                                CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                            });
                        }
                    }
                }

                return profile.UserID > 0 ? profile : null;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error getting user profile: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region View-based Queries

        /// <summary>
        /// Obtiene encabezado del perfil
        /// VIEW: vw_UserProfileHeader
        /// </summary>
        public async Task<UserProfileHeaderVM?> GetUserHeaderAsync(int userId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<UserProfileHeaderVM>(
                    "vw_UserProfileHeader",
                    reader => new UserProfileHeaderVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias"),
                        LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode"),
                        ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                        AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus"),
                        SystemRoleCode = DatabaseHelper.GetSafeString(reader, "SystemRoleCode"),
                        SystemRoleDisplay = DatabaseHelper.GetSafeString(reader, "SystemRoleDisplay"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                    },
                    whereClause: $"UserID = {userId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene sesiones activas de un usuario
        /// VIEW: vw_UserActiveSessions
        /// </summary>
        public async Task<List<UserActiveSessionVM>> GetActiveSessionsAsync(int userId)
        {
            try
            {
                return await _db.ExecuteViewAsync<UserActiveSessionVM>(
                    "vw_UserActiveSessions",
                    reader => new UserActiveSessionVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        SessionID = DatabaseHelper.GetSafeGuid(reader, "SessionID"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        LastActivityAt = DatabaseHelper.GetSafeDateTime(reader, "LastActivityAt"),
                        ExpiresAt = DatabaseHelper.GetSafeDateTime(reader, "ExpiresAt"),
                        IsValid = DatabaseHelper.GetSafeBool(reader, "IsValid")
                    },
                    whereClause: $"UserID = {userId}"
                );
            }
            catch
            {
                return new List<UserActiveSessionVM>();
            }
        }

        /// <summary>
        /// Obtiene perfil básico desde VIEW
        /// VIEW: vw_UserProfileBasic
        /// </summary>
        public async Task<UserProfileBasicVM?> GetUserBasicAsync(int userId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<UserProfileBasicVM>(
                    "vw_UserProfileBasic",
                    reader => new UserProfileBasicVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias"),
                        LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode"),
                        ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                        ProfileImageWidth = DatabaseHelper.GetSafeInt16(reader, "ProfileImageWidth") == 0
                            ? null : DatabaseHelper.GetSafeInt16(reader, "ProfileImageWidth"),
                        ProfileImageHeight = DatabaseHelper.GetSafeInt16(reader, "ProfileImageHeight") == 0
                            ? null : DatabaseHelper.GetSafeInt16(reader, "ProfileImageHeight"),
                        ProfileImageBytes = DatabaseHelper.GetSafeInt32(reader, "ProfileImageBytes") == 0
                            ? null : DatabaseHelper.GetSafeInt32(reader, "ProfileImageBytes"),
                        AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        SystemRoleDisplay = DatabaseHelper.GetSafeString(reader, "SystemRoleDisplay"),
                        SystemRoleCode = DatabaseHelper.GetSafeString(reader, "SystemRoleCode")
                    },
                    whereClause: $"UserID = {userId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}