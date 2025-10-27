using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de auditoría y mantenimiento del sistema
    /// Mapea a: sp_GetAuditLogs, sp_GetUserAuditHistory, sp_CleanupExpiredSessions, sp_GetAuditStats
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Obtiene logs de auditoría con filtros
        /// SP: app.sp_GetAuditLogs
        /// Requiere rol ADMIN (verificar en controller)
        /// </summary>
        /// <param name="filter">Filtros opcionales</param>
        /// <returns>Lista de logs de auditoría</returns>
        Task<List<AuditLogVM>> GetAuditLogsAsync(AuditLogFilterDTO filter);

        /// <summary>
        /// Obtiene historial de auditoría de un usuario específico
        /// SP: app.sp_GetUserAuditHistory
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="top">Cantidad de registros (máx 500)</param>
        /// <returns>Historial de acciones del usuario</returns>
        Task<List<UserAuditHistoryVM>> GetUserAuditHistoryAsync(int userId, int top = 50);

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados
        /// SP: app.sp_CleanupExpiredSessions
        /// Requiere rol ADMIN
        /// </summary>
        /// <param name="retentionDays">Días de retención (1-180)</param>
        /// <returns>Resultado de limpieza</returns>
        Task<CleanupResultVM> CleanupExpiredSessionsAsync(int retentionDays = 30);

        /// <summary>
        /// Obtiene estadísticas de auditoría
        /// SP: app.sp_GetAuditStats
        /// Requiere rol ADMIN
        /// </summary>
        /// <param name="days">Días a analizar (1-365)</param>
        /// <returns>Estadísticas agregadas</returns>
        Task<AuditStatsVM> GetAuditStatsAsync(int days = 30);

        /// <summary>
        /// Registra una acción de auditoría directamente
        /// Para casos especiales no cubiertos por SPs
        /// </summary>
        Task LogActionAsync(int? actorUserId, string entityType, string entityId,
            string actionCode, string? details = null, string? sourceIp = null, string? userAgent = null);
    }
}