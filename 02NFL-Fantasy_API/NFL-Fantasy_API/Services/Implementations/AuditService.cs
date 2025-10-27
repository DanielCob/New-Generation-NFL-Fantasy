using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de auditoría
    /// Maneja logs, estadísticas y limpieza del sistema
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly DatabaseHelper _db;

        public AuditService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Audit Logs

        /// <summary>
        /// Obtiene logs de auditoría con filtros
        /// SP: app.sp_GetAuditLogs
        /// </summary>
        public async Task<List<AuditLogVM>> GetAuditLogsAsync(AuditLogFilterDTO filter)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@EntityType", DatabaseHelper.DbNullIfNull(filter.EntityType)),
                    new SqlParameter("@EntityID", DatabaseHelper.DbNullIfNull(filter.EntityID)),
                    new SqlParameter("@ActorUserID", DatabaseHelper.DbNullIfNull(filter.ActorUserID)),
                    new SqlParameter("@ActionCode", DatabaseHelper.DbNullIfNull(filter.ActionCode)),
                    new SqlParameter("@StartDate", DatabaseHelper.DbNullIfNull(filter.StartDate)),
                    new SqlParameter("@EndDate", DatabaseHelper.DbNullIfNull(filter.EndDate)),
                    new SqlParameter("@Top", filter.Top)
                };

                return await _db.ExecuteStoredProcedureListAsync<AuditLogVM>(
                    "app.sp_GetAuditLogs",
                    parameters,
                    reader => new AuditLogVM
                    {
                        ActionLogID = DatabaseHelper.GetSafeInt64(reader, "ActionLogID"),
                        ActorUserID = DatabaseHelper.GetSafeNullableInt32(reader, "ActorUserID"),
                        ActorName = DatabaseHelper.GetSafeNullableString(reader, "ActorName"),
                        ActorEmail = DatabaseHelper.GetSafeNullableString(reader, "ActorEmail"),
                        ImpersonatedByUserID = DatabaseHelper.GetSafeNullableInt32(reader, "ImpersonatedByUserID"),
                        ImpersonatedByName = DatabaseHelper.GetSafeNullableString(reader, "ImpersonatedByName"),
                        EntityType = DatabaseHelper.GetSafeString(reader, "EntityType"),
                        EntityID = DatabaseHelper.GetSafeString(reader, "EntityID"),
                        ActionCode = DatabaseHelper.GetSafeString(reader, "ActionCode"),
                        ActionAt = DatabaseHelper.GetSafeDateTime(reader, "ActionAt"),
                        SourceIp = DatabaseHelper.GetSafeNullableString(reader, "SourceIp"),
                        UserAgent = DatabaseHelper.GetSafeNullableString(reader, "UserAgent"),
                        Details = DatabaseHelper.GetSafeNullableString(reader, "Details")
                    }
                );
            }
            catch
            {
                return new List<AuditLogVM>();
            }
        }

        /// <summary>
        /// Obtiene historial de auditoría de un usuario
        /// SP: app.sp_GetUserAuditHistory
        /// </summary>
        public async Task<List<UserAuditHistoryVM>> GetUserAuditHistoryAsync(int userId, int top = 50)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@Top", top)
                };

                return await _db.ExecuteStoredProcedureListAsync<UserAuditHistoryVM>(
                    "app.sp_GetUserAuditHistory",
                    parameters,
                    reader => new UserAuditHistoryVM
                    {
                        ActionLogID = DatabaseHelper.GetSafeInt64(reader, "ActionLogID"),
                        EntityType = DatabaseHelper.GetSafeString(reader, "EntityType"),
                        EntityID = DatabaseHelper.GetSafeString(reader, "EntityID"),
                        ActionCode = DatabaseHelper.GetSafeString(reader, "ActionCode"),
                        ActionAt = DatabaseHelper.GetSafeDateTime(reader, "ActionAt"),
                        SourceIp = DatabaseHelper.GetSafeNullableString(reader, "SourceIp"),
                        UserAgent = DatabaseHelper.GetSafeNullableString(reader, "UserAgent"),
                        Details = DatabaseHelper.GetSafeNullableString(reader, "Details")
                    }
                );
            }
            catch
            {
                return new List<UserAuditHistoryVM>();
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados
        /// SP: app.sp_CleanupExpiredSessions
        /// </summary>
        public async Task<CleanupResultVM> CleanupExpiredSessionsAsync(int retentionDays = 30)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@RetentionDays", retentionDays)
                };

                var result = await _db.ExecuteStoredProcedureAsync<CleanupResultVM>(
                    "app.sp_CleanupExpiredSessions",
                    parameters,
                    reader => new CleanupResultVM
                    {
                        DeletedSessions = DatabaseHelper.GetSafeInt32(reader, "DeletedSessions"),
                        DeletedResetTokens = DatabaseHelper.GetSafeInt32(reader, "DeletedResetTokens"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return result ?? new CleanupResultVM
                {
                    DeletedSessions = 0,
                    DeletedResetTokens = 0,
                    Message = "Limpieza completada (sin registros para eliminar)."
                };
            }
            catch (Exception ex)
            {
                return new CleanupResultVM
                {
                    DeletedSessions = 0,
                    DeletedResetTokens = 0,
                    Message = $"Error en limpieza: {ex.Message}"
                };
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Obtiene estadísticas de auditoría
        /// SP: app.sp_GetAuditStats (retorna 3 result sets)
        /// </summary>
        public async Task<AuditStatsVM> GetAuditStatsAsync(int days = 30)
        {
            try
            {
                var stats = new AuditStatsVM();
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Days", days)
                };

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_GetAuditStats", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // Result Set 1: ActionsByEntity
                    while (await reader.ReadAsync())
                    {
                        stats.ActionsByEntity.Add(new EntityTypeStatVM
                        {
                            EntityType = DatabaseHelper.GetSafeString(reader, "EntityType"),
                            ActionCount = DatabaseHelper.GetSafeInt32(reader, "ActionCount")
                        });
                    }

                    // Result Set 2: ActionsByCode
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stats.ActionsByCode.Add(new ActionCodeStatVM
                            {
                                ActionCode = DatabaseHelper.GetSafeString(reader, "ActionCode"),
                                ActionCount = DatabaseHelper.GetSafeInt32(reader, "ActionCount")
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
                                ActorUserID = DatabaseHelper.GetSafeInt32(reader, "ActorUserID"),
                                Name = DatabaseHelper.GetSafeString(reader, "Name"),
                                Email = DatabaseHelper.GetSafeString(reader, "Email"),
                                ActionCount = DatabaseHelper.GetSafeInt32(reader, "ActionCount")
                            });
                        }
                    }
                }

                return stats;
            }
            catch
            {
                return new AuditStatsVM();
            }
        }

        #endregion

        #region Direct Logging

        /// <summary>
        /// Registra una acción de auditoría directamente
        /// Para casos especiales no cubiertos por SPs
        /// </summary>
        public async Task LogActionAsync(int? actorUserId, string entityType, string entityId,
            string actionCode, string? details = null, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var query = @"
                    INSERT INTO audit.UserActionLog
                    (ActorUserID, EntityType, EntityID, ActionCode, Details, SourceIp, UserAgent)
                    VALUES
                    (@ActorUserID, @EntityType, @EntityID, @ActionCode, @Details, @SourceIp, @UserAgent)";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", DatabaseHelper.DbNullIfNull(actorUserId)),
                    new SqlParameter("@EntityType", entityType),
                    new SqlParameter("@EntityID", entityId),
                    new SqlParameter("@ActionCode", actionCode),
                    new SqlParameter("@Details", DatabaseHelper.DbNullIfNull(details)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                await _db.ExecuteRawQueryAsync<object>(query, _ => new object(), parameters);
            }
            catch
            {
                // Log silently fails para no interrumpir el flujo
            }
        }

        #endregion
    }
}