using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.Audit;
using NFL_Fantasy_API.Models.ViewModels.Audit;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.Audit
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de auditoría.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class AuditDataAccess
    {
        private readonly IDatabaseHelper _db;

        public AuditDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Audit Logs

        /// <summary>
        /// Obtiene logs de auditoría con filtros.
        /// SP: app.sp_GetAuditLogs
        /// </summary>
        public async Task<List<AuditLogVM>> GetAuditLogsAsync(AuditLogFilterDTO filter)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@EntityType", filter.EntityType),
                SqlParameterExtensions.CreateParameter("@EntityID", filter.EntityID),
                SqlParameterExtensions.CreateParameter("@ActorUserID", filter.ActorUserID),
                SqlParameterExtensions.CreateParameter("@ActionCode", filter.ActionCode),
                SqlParameterExtensions.CreateParameter("@StartDate", filter.StartDate),
                SqlParameterExtensions.CreateParameter("@EndDate", filter.EndDate),
                SqlParameterExtensions.CreateParameter("@Top", filter.Top)
            };

            return await _db.ExecuteStoredProcedureListAsync(
                "app.sp_GetAuditLogs",
                parameters,
                reader => new AuditLogVM
                {
                    ActionLogID = reader.GetSafeInt64("ActionLogID"),
                    ActorUserID = reader.GetSafeNullableInt32("ActorUserID"),
                    ActorName = reader.GetSafeNullableString("ActorName"),
                    ActorEmail = reader.GetSafeNullableString("ActorEmail"),
                    ImpersonatedByUserID = reader.GetSafeNullableInt32("ImpersonatedByUserID"),
                    ImpersonatedByName = reader.GetSafeNullableString("ImpersonatedByName"),
                    EntityType = reader.GetSafeString("EntityType"),
                    EntityID = reader.GetSafeString("EntityID"),
                    ActionCode = reader.GetSafeString("ActionCode"),
                    ActionAt = reader.GetSafeDateTime("ActionAt"),
                    SourceIp = reader.GetSafeNullableString("SourceIp"),
                    UserAgent = reader.GetSafeNullableString("UserAgent"),
                    Details = reader.GetSafeNullableString("Details")
                }
            );
        }

        /// <summary>
        /// Obtiene historial de auditoría de un usuario.
        /// SP: app.sp_GetUserAuditHistory
        /// </summary>
        public async Task<List<UserAuditHistoryVM>> GetUserAuditHistoryAsync(int userId, int top)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@UserID", userId),
                SqlParameterExtensions.CreateParameter("@Top", top)
            };

            return await _db.ExecuteStoredProcedureListAsync(
                "app.sp_GetUserAuditHistory",
                parameters,
                reader => new UserAuditHistoryVM
                {
                    ActionLogID = reader.GetSafeInt64("ActionLogID"),
                    EntityType = reader.GetSafeString("EntityType"),
                    EntityID = reader.GetSafeString("EntityID"),
                    ActionCode = reader.GetSafeString("ActionCode"),
                    ActionAt = reader.GetSafeDateTime("ActionAt"),
                    SourceIp = reader.GetSafeNullableString("SourceIp"),
                    UserAgent = reader.GetSafeNullableString("UserAgent"),
                    Details = reader.GetSafeNullableString("Details")
                }
            );
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados.
        /// SP: app.sp_CleanupExpiredSessions
        /// </summary>
        public async Task<CleanupResultVM?> CleanupExpiredSessionsAsync(int retentionDays)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@RetentionDays", retentionDays)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_CleanupExpiredSessions",
                parameters,
                reader => new CleanupResultVM
                {
                    DeletedSessions = reader.GetSafeInt32("DeletedSessions"),
                    DeletedResetTokens = reader.GetSafeInt32("DeletedResetTokens"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Obtiene estadísticas de auditoría.
        /// SP: app.sp_GetAuditStats (retorna 3 result sets)
        /// RS1: ActionsByEntity
        /// RS2: ActionsByCode
        /// RS3: TopUsers
        /// </summary>
        public async Task<AuditStatsVM> GetAuditStatsAsync(int days)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@Days", days)
            };

            var stats = new AuditStatsVM();

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetAuditStats", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1: ActionsByEntity
            while (await reader.ReadAsync())
            {
                stats.ActionsByEntity.Add(new EntityTypeStatVM
                {
                    EntityType = reader.GetSafeString("EntityType"),
                    ActionCount = reader.GetSafeInt32("ActionCount")
                });
            }

            // Result Set 2: ActionsByCode
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    stats.ActionsByCode.Add(new ActionCodeStatVM
                    {
                        ActionCode = reader.GetSafeString("ActionCode"),
                        ActionCount = reader.GetSafeInt32("ActionCount")
                    });
                }
            }

            // Result Set 3: TopUsers
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    stats.TopUsers.Add(new TopUserStatVM
                    {
                        ActorUserID = reader.GetSafeInt32("ActorUserID"),
                        Name = reader.GetSafeString("Name"),
                        Email = reader.GetSafeString("Email"),
                        ActionCount = reader.GetSafeInt32("ActionCount")
                    });
                }
            }

            return stats;
        }

        #endregion

        #region Direct Logging

        /// <summary>
        /// Registra una acción de auditoría directamente.
        /// Usa INSERT directo en la tabla audit.UserActionLog.
        /// </summary>
        public async Task LogActionAsync(
            int? actorUserId,
            string entityType,
            string entityId,
            string actionCode,
            string? details,
            string? sourceIp,
            string? userAgent)
        {
            var query = @"
                INSERT INTO audit.UserActionLog
                (ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
                VALUES
                (@ActorUserID, @EntityType, @EntityID, @ActionCode, @Details, @SourceIp, @UserAgent)";

            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@EntityType", entityType),
                SqlParameterExtensions.CreateParameter("@EntityID", entityId),
                SqlParameterExtensions.CreateParameter("@ActionCode", actionCode),
                SqlParameterExtensions.CreateParameter("@Details", details),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            await _db.ExecuteRawQueryAsync(query, _ => new object(), parameters);
        }

        #endregion

        #region Métodos adicionales (antes en ViewsService/AuditViewMapper)

        /// <summary>
        /// Obtiene estadísticas generales del sistema.
        /// Combina múltiples queries para dashboard administrativo.
        /// </summary>
        public async Task<object> GetSystemStatsAsync()
        {
            try
            {
                // Total de usuarios
                var totalUsers = await _db.ExecuteViewAsync(
                    "auth.UserAccount",
                    reader => 1 // contador
                );

                // Usuarios activos
                var activeUsers = await _db.ExecuteViewAsync(
                    "auth.UserAccount",
                    reader => 1,
                    whereClause: "AccountStatus = 1"
                );

                // Total de ligas
                var totalLeagues = await _db.ExecuteViewAsync(
                    "league.League",
                    reader => 1
                );

                // Ligas activas
                var activeLeagues = await _db.ExecuteViewAsync(
                    "league.League",
                    reader => 1,
                    whereClause: "Status = 1"
                );

                // Ligas en Pre-Draft
                var preDraftLeagues = await _db.ExecuteViewAsync(
                    "league.League",
                    reader => 1,
                    whereClause: "Status = 0"
                );

                // Total de equipos
                var totalTeams = await _db.ExecuteViewAsync(
                    "league.Team",
                    reader => 1
                );

                // Sesiones activas
                var activeSessions = await _db.ExecuteViewAsync(
                    "vw_UserActiveSessions",
                    reader => 1
                );

                return new
                {
                    Users = new
                    {
                        Total = totalUsers.Count,
                        Active = activeUsers.Count,
                        Inactive = totalUsers.Count - activeUsers.Count
                    },
                    Leagues = new
                    {
                        Total = totalLeagues.Count,
                        Active = activeLeagues.Count,
                        PreDraft = preDraftLeagues.Count,
                        InactiveOrClosed = totalLeagues.Count - activeLeagues.Count - preDraftLeagues.Count
                    },
                    Teams = new { Total = totalTeams.Count },
                    Sessions = new { ActiveNow = activeSessions.Count },
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = $"Error al obtener estadísticas: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        #endregion
    }
}