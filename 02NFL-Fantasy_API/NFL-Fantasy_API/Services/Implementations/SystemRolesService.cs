using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class SystemRolesService : ISystemRolesService
    {
        private readonly DatabaseHelper _db;

        public SystemRolesService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        // --------------------------------------------------------------------
        // 1) Lista de roles del sistema  ->  auth.SystemRole
        // --------------------------------------------------------------------
        public async Task<List<SystemRoleDTO>> GetRolesAsync()
        {
            return await _db.ExecuteViewAsync<SystemRoleDTO>(
                "auth.SystemRole",
                r => new SystemRoleDTO
                {
                    RoleCode = DatabaseHelper.GetSafeString(r, "RoleCode"),
                    Display = DatabaseHelper.GetSafeString(r, "Display"),
                    Description = DatabaseHelper.GetSafeNullableString(r, "Description")
                },
                orderBy: "RoleCode"
            );
        }

        // --------------------------------------------------------------------
        // 2) Cambiar rol de usuario  ->  app.sp_ChangeUserSystemRole
        //     (El SP devuelve columnas: UserID, OldRole, NewRole, Message)
        // --------------------------------------------------------------------
        public async Task<ChangeUserSystemRoleResponseDTO> ChangeUserRoleAsync(
            int actorUserId, int targetUserId, ChangeUserSystemRoleDTO dto, string? sourceIp = null, string? userAgent = null)
        {
            var p = new[]
            {
                new SqlParameter("@ActorUserID", actorUserId),
                new SqlParameter("@TargetUserID", targetUserId),
                new SqlParameter("@NewRoleCode", dto.NewRoleCode),
                new SqlParameter("@Reason",    DatabaseHelper.DbNullIfNull(dto.Reason)),
                new SqlParameter("@SourceIp",  DatabaseHelper.DbNullIfNull(sourceIp)),
                new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent)),
            };

            var row = await _db.ExecuteStoredProcedureAsync<ChangeUserSystemRoleResponseDTO>(
                "app.sp_ChangeUserSystemRole",
                p,
                r => new ChangeUserSystemRoleResponseDTO
                {
                    UserID = DatabaseHelper.GetSafeInt32(r, "UserID"),
                    OldRoleCode = DatabaseHelper.GetSafeString(r, "OldRole"),
                    NewRoleCode = DatabaseHelper.GetSafeString(r, "NewRole"),
                    Message = DatabaseHelper.GetSafeString(r, "Message")
                });

            return row ?? new ChangeUserSystemRoleResponseDTO
            {
                UserID = targetUserId,
                OldRoleCode = "",
                NewRoleCode = dto.NewRoleCode,
                Message = "Rol del sistema actualizado."
            };
        }

        // --------------------------------------------------------------------
        // 3) Historial de cambios de rol  ->  app.sp_GetSystemRoleHistory
        // --------------------------------------------------------------------
        public async Task<List<SystemRoleChangeLogDTO>> GetUserRoleChangesAsync(int actorUserId, int targetUserId, int top = 50)
        {
            var p = new[]
            {
                new SqlParameter("@ActorUserID", actorUserId),
                new SqlParameter("@TargetUserID", targetUserId),
                new SqlParameter("@Top", top)
            };

            return await _db.ExecuteStoredProcedureListAsync<SystemRoleChangeLogDTO>(
                "app.sp_GetSystemRoleHistory",
                p,
                r => new SystemRoleChangeLogDTO
                {
                    ChangeID = DatabaseHelper.GetSafeInt64(r, "ChangeID"),
                    UserID = DatabaseHelper.GetSafeInt32(r, "UserID"),
                    ChangedByUserID = DatabaseHelper.GetSafeInt32(r, "ChangedByUserID"),
                    OldRoleCode = DatabaseHelper.GetSafeString(r, "OldRoleCode"),
                    NewRoleCode = DatabaseHelper.GetSafeString(r, "NewRoleCode"),
                    ChangedAt = DatabaseHelper.GetSafeDateTime(r, "ChangedAt"),
                    Reason = DatabaseHelper.GetSafeNullableString(r, "Reason"),
                    SourceIp = DatabaseHelper.GetSafeNullableString(r, "SourceIp")
                });
        }

        // --------------------------------------------------------------------
        // 4) (OPCIONAL – Punto 2) Listado paginado de usuarios por rol
        //     -> app.sp_GetUsersBySystemRole
        //     Devuelve metadatos (TotalRecords, CurrentPage, PageSize, TotalPages)
        // --------------------------------------------------------------------
        public async Task<UsersByRolePageDTO> GetUsersBySystemRoleAsync(
            int actorUserId, string? filterRole, string? searchTerm, int pageNumber = 1, int pageSize = 50)
        {
            var result = new UsersByRolePageDTO();

            var p = new[]
            {
                new SqlParameter("@ActorUserID", actorUserId),
                new SqlParameter("@FilterRole",  DatabaseHelper.DbNullIfNull(filterRole)),
                new SqlParameter("@SearchTerm",  DatabaseHelper.DbNullIfNull(searchTerm)),
                new SqlParameter("@PageNumber",  pageNumber),
                new SqlParameter("@PageSize",    pageSize),
            };

            // Igual que en tu NFLTeamService: obtener el connection string desde el helper
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            using var cn = new SqlConnection(connStr);
            using var cmd = new SqlCommand("app.sp_GetUsersBySystemRole", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddRange(p);

            await cn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                result.Items.Add(new UserWithRoleVM
                {
                    UserID = DatabaseHelper.GetSafeInt32(r, "UserID"),
                    Email = DatabaseHelper.GetSafeString(r, "Email"),
                    Name = DatabaseHelper.GetSafeString(r, "Name"),
                    Alias = DatabaseHelper.GetSafeNullableString(r, "Alias"),
                    SystemRoleCode = DatabaseHelper.GetSafeString(r, "SystemRoleCode"),
                    SystemRoleDisplay = DatabaseHelper.GetSafeNullableString(r, "SystemRoleDisplay"),
                    AccountStatus = DatabaseHelper.GetSafeByte(r, "AccountStatus"),
                    CreatedAt = DatabaseHelper.GetSafeDateTime(r, "CreatedAt"),
                });

                // Leer metadatos una sola vez (vienen en cada fila)
                if (result.TotalRecords == 0)
                {
                    result.TotalRecords = DatabaseHelper.GetSafeInt32(r, "TotalRecords");
                    result.CurrentPage = DatabaseHelper.GetSafeInt32(r, "CurrentPage");
                    result.PageSize = DatabaseHelper.GetSafeInt32(r, "PageSize");
                    result.TotalPages = DatabaseHelper.GetSafeInt32(r, "TotalPages");
                }
            }

            return result;
        }
    }
}
