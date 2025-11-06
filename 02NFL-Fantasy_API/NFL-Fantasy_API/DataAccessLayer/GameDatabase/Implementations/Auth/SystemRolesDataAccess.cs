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
    /// Capa de acceso a datos para operaciones de roles del sistema.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class SystemRolesDataAccess
    {
        private readonly IDatabaseHelper _db;

        public SystemRolesDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Get Roles

        /// <summary>
        /// Lista todos los roles del sistema desde la tabla auth.SystemRole.
        /// </summary>
        public async Task<List<SystemRoleDTO>> GetRolesAsync()
        {
            return await _db.ExecuteViewAsync(
                "auth.SystemRole",
                reader => new SystemRoleDTO
                {
                    RoleCode = reader.GetSafeString("RoleCode"),
                    Display = reader.GetSafeString("Display"),
                    Description = reader.GetSafeNullableString("Description")
                },
                orderBy: "RoleCode"
            );
        }

        #endregion

        #region Change User Role

        /// <summary>
        /// Cambia el rol de sistema de un usuario.
        /// SP: app.sp_ChangeUserSystemRole
        /// </summary>
        public async Task<ChangeUserRoleResultDTO?> ChangeUserRoleAsync(
            int actorUserId,
            int targetUserId,
            ChangeUserSystemRoleDTO dto,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TargetUserID", targetUserId),
                SqlParameterExtensions.CreateParameter("@NewRoleCode", dto.NewRoleCode),
                SqlParameterExtensions.CreateParameter("@Reason", dto.Reason),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_ChangeUserSystemRole",
                parameters,
                reader => new ChangeUserRoleResultDTO
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    OldRoleCode = reader.GetSafeString("OldRole"),
                    NewRoleCode = reader.GetSafeString("NewRole"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        #endregion

        #region Role Change History

        /// <summary>
        /// Obtiene el historial de cambios de rol de un usuario.
        /// SP: app.sp_GetSystemRoleHistory
        /// </summary>
        public async Task<List<UserRoleChangeHistoryDTO>> GetUserRoleChangesAsync(
            int actorUserId,
            int targetUserId,
            int top)
        {
            var parameters = new[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TargetUserID", targetUserId),
                SqlParameterExtensions.CreateParameter("@Top", top)
            };

            return await _db.ExecuteStoredProcedureListAsync(
                "app.sp_GetSystemRoleHistory",
                parameters,
                reader => new UserRoleChangeHistoryDTO
                {
                    ChangeID = reader.GetSafeInt64("ChangeID"),
                    UserID = reader.GetSafeInt32("UserID"),
                    ChangedByUserID = reader.GetSafeInt32("ChangedByUserID"),
                    OldRoleCode = reader.GetSafeString("OldRoleCode"),
                    NewRoleCode = reader.GetSafeString("NewRoleCode"),
                    ChangedAt = reader.GetSafeDateTime("ChangedAt"),
                    Reason = reader.GetSafeNullableString("Reason"),
                    SourceIp = reader.GetSafeNullableString("SourceIp"),
                    UserAgent = reader.GetSafeNullableString("UserAgent")
                }
            );
        }

        #endregion

        #region Users By Role (Paginado)

        /// <summary>
        /// Obtiene listado paginado de usuarios por rol.
        /// SP: app.sp_GetUsersBySystemRole
        /// Devuelve metadatos de paginación.
        /// </summary>
        public async Task<UsersByRolePageDTO> GetUsersBySystemRoleAsync(
            int actorUserId,
            string? filterRole,
            string? searchTerm,
            int pageNumber,
            int pageSize)
        {
            var result = new UsersByRolePageDTO();

            var parameters = new[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@FilterRole", filterRole),
                SqlParameterExtensions.CreateParameter("@SearchTerm", searchTerm),
                SqlParameterExtensions.CreateParameter("@PageNumber", pageNumber),
                SqlParameterExtensions.CreateParameter("@PageSize", pageSize)
            };

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetUsersBySystemRole", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Items.Add(new UserWithRoleVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode"),
                    SystemRoleDisplay = reader.GetSafeNullableString("SystemRoleDisplay") ?? string.Empty,
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                });

                // Leer metadatos una sola vez (vienen en cada fila)
                if (result.TotalRecords == 0)
                {
                    result.TotalRecords = reader.GetSafeInt32("TotalRecords");
                    result.CurrentPage = reader.GetSafeInt32("CurrentPage");
                    result.PageSize = reader.GetSafeInt32("PageSize");
                    result.TotalPages = reader.GetSafeInt32("TotalPages");
                }
            }

            return result;
        }

        #endregion

        #region Get System Roles View

        /// <summary>
        /// Obtiene lista de roles del sistema desde VIEW.
        /// VIEW: vw_SystemRoles
        /// </summary>
        public async Task<List<SystemRoleVM>> GetSystemRolesAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_SystemRoles",
                reader => new SystemRoleVM
                {
                    RoleCode = reader.GetSafeString("RoleCode"),
                    Display = reader.GetSafeString("Display"),
                    Description = reader.GetSafeNullableString("Description")
                },
                orderBy: "RoleCode"
            );
        }

        #endregion

        #region Get Users With Roles View

        /// <summary>
        /// Obtiene usuarios con roles completos desde VIEW.
        /// VIEW: vw_UsersWithRoles
        /// </summary>
        public async Task<List<UserWithFullRoleVM>> GetUsersWithRolesAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_UsersWithRoles",
                reader => new UserWithFullRoleVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode"),
                    SystemRoleDisplay = reader.GetSafeString("SystemRoleDisplay"),
                    SystemRoleDescription = reader.GetSafeNullableString("SystemRoleDescription"),
                    LanguageCode = reader.GetSafeString("LanguageCode"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    AccountStatusDisplay = reader.GetSafeString("AccountStatusDisplay"),
                    FailedLoginCount = reader.GetSafeInt32("FailedLoginCount"),
                    LockedUntil = reader.GetSafeNullableDateTime("LockedUntil"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    CommissionedLeaguesCount = reader.GetSafeInt32("CommissionedLeaguesCount")
                },
                orderBy: "Name"
            );
        }

        #endregion
    }
}