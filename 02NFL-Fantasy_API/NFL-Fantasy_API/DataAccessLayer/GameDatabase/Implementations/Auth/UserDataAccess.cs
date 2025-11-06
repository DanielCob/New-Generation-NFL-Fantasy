using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.Models.ViewModels.Auth;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.Auth
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de usuarios.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/Views.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class UserDataAccess
    {
        private readonly IDatabaseHelper _db;

        public UserDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Update Profile

        /// <summary>
        /// Actualiza el perfil de un usuario.
        /// SP: app.sp_UpdateUserProfile
        /// </summary>
        public async Task<string> UpdateProfileAsync(
            int actorUserId,
            int targetUserId,
            UpdateUserProfileDTO dto,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TargetUserID", targetUserId),
                SqlParameterExtensions.CreateParameter("@Name", dto.Name),
                SqlParameterExtensions.CreateParameter("@Alias", dto.Alias),
                SqlParameterExtensions.CreateParameter("@LanguageCode", dto.LanguageCode),
                SqlParameterExtensions.CreateParameter("@ProfileImageUrl", dto.ProfileImageUrl),
                SqlParameterExtensions.CreateParameter("@ProfileImageWidth", dto.ProfileImageWidth),
                SqlParameterExtensions.CreateParameter("@ProfileImageHeight", dto.ProfileImageHeight),
                SqlParameterExtensions.CreateParameter("@ProfileImageBytes", dto.ProfileImageBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_UpdateUserProfile",
                parameters
            );
        }

        #endregion

        #region Get Profile

        /// <summary>
        /// Obtiene el perfil completo de un usuario.
        /// SP: app.sp_GetUserProfile (retorna 3 result sets)
        /// Result Set 1: Datos del usuario
        /// Result Set 2: Ligas donde es comisionado
        /// Result Set 3: Equipos del usuario
        /// </summary>
        public async Task<UserProfileResponseDTO?> GetUserProfileAsync(int userId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@UserID", userId)
            };

            var profile = new UserProfileResponseDTO();

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetUserProfile", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1: Perfil del usuario
            if (await reader.ReadAsync())
            {
                profile.UserID = reader.GetSafeInt32("UserID");
                profile.Email = reader.GetSafeString("Email");
                profile.Name = reader.GetSafeString("Name");
                profile.Alias = reader.GetSafeNullableString("Alias");
                profile.LanguageCode = reader.GetSafeString("LanguageCode");
                profile.ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl");
                profile.AccountStatus = reader.GetSafeByte("AccountStatus");
                profile.CreatedAt = reader.GetSafeDateTime("CreatedAt");
                profile.UpdatedAt = reader.GetSafeDateTime("UpdatedAt");
                profile.SystemRoleCode = reader.GetSafeString("SystemRoleCode");
            }

            // Result Set 2: Ligas como comisionado
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    profile.CommissionedLeagues.Add(new UserCommissionedLeagueDTO
                    {
                        LeagueID = reader.GetSafeInt32("LeagueID"),
                        LeagueName = reader.GetSafeString("LeagueName"),
                        Status = reader.GetSafeByte("Status"),
                        TeamSlots = reader.GetSafeByte("TeamSlots"),
                        RoleCode = reader.GetSafeString("RoleCode"),
                        JoinedAt = reader.GetSafeDateTime("JoinedAt")
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
                        TeamID = reader.GetSafeInt32("TeamID"),
                        LeagueID = reader.GetSafeInt32("LeagueID"),
                        LeagueName = reader.GetSafeString("LeagueName"),
                        TeamName = reader.GetSafeString("TeamName"),
                        CreatedAt = reader.GetSafeDateTime("CreatedAt")
                    });
                }
            }

            return profile.UserID > 0 ? profile : null;
        }

        #endregion

        #region View Queries

        /// <summary>
        /// Obtiene encabezado del perfil desde VIEW.
        /// VIEW: vw_UserProfileHeader
        /// </summary>
        public async Task<UserProfileHeaderVM?> GetUserHeaderAsync(int userId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_UserProfileHeader",
                reader => new UserProfileHeaderVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    LanguageCode = reader.GetSafeString("LanguageCode"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode"),
                    SystemRoleDisplay = reader.GetSafeString("SystemRoleDisplay"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt")
                },
                whereClause: $"UserID = {userId}"
            );

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene sesiones activas de un usuario desde VIEW.
        /// VIEW: vw_UserActiveSessions
        /// </summary>
        public async Task<List<UserActiveSessionVM>> GetActiveSessionsAsync(int userId)
        {
            return await _db.ExecuteViewAsync(
                "vw_UserActiveSessions",
                reader => new UserActiveSessionVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    SessionID = reader.GetSafeGuid("SessionID"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    LastActivityAt = reader.GetSafeDateTime("LastActivityAt"),
                    ExpiresAt = reader.GetSafeDateTime("ExpiresAt"),
                    IsValid = reader.GetSafeBool("IsValid")
                },
                whereClause: $"UserID = {userId}"
            );
        }

        /// <summary>
        /// Obtiene perfil básico desde VIEW.
        /// VIEW: vw_UserProfileBasic
        /// </summary>
        public async Task<UserProfileBasicVM?> GetUserBasicAsync(int userId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_UserProfileBasic",
                reader => new UserProfileBasicVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    LanguageCode = reader.GetSafeString("LanguageCode"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    ProfileImageWidth = reader.GetSafeInt16("ProfileImageWidth") == 0
                        ? null : reader.GetSafeInt16("ProfileImageWidth"),
                    ProfileImageHeight = reader.GetSafeInt16("ProfileImageHeight") == 0
                        ? null : reader.GetSafeInt16("ProfileImageHeight"),
                    ProfileImageBytes = reader.GetSafeInt32("ProfileImageBytes") == 0
                        ? null : reader.GetSafeInt32("ProfileImageBytes"),
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    SystemRoleDisplay = reader.GetSafeString("SystemRoleDisplay"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode")
                },
                whereClause: $"UserID = {userId}"
            );

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene todos los usuarios activos del sistema.
        /// VIEW: vw_UserProfileBasic con WHERE AccountStatus=1
        /// </summary>
        public async Task<List<UserProfileBasicVM>> GetActiveUsersAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_UserProfileBasic",
                reader => new UserProfileBasicVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    LanguageCode = reader.GetSafeString("LanguageCode"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    ProfileImageWidth = reader.GetSafeInt16("ProfileImageWidth") == 0
                        ? null : reader.GetSafeInt16("ProfileImageWidth"),
                    ProfileImageHeight = reader.GetSafeInt16("ProfileImageHeight") == 0
                        ? null : reader.GetSafeInt16("ProfileImageHeight"),
                    ProfileImageBytes = reader.GetSafeInt32("ProfileImageBytes") == 0
                        ? null : reader.GetSafeInt32("ProfileImageBytes"),
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    SystemRoleDisplay = reader.GetSafeString("SystemRoleDisplay"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode")
                },
                whereClause: "AccountStatus = 1",
                orderBy: "CreatedAt DESC"
            );
        }

        #endregion
    }
}