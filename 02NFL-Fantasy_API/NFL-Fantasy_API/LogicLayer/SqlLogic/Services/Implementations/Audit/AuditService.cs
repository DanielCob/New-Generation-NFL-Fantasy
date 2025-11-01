using NFL_Fantasy_API.SharedSystems.Validators;
using NFL_Fantasy_API.Models.DTOs.Audit;
using NFL_Fantasy_API.Models.ViewModels.Audit;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Audit;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Audit;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Audit
{
    /// <summary>
    /// Implementación del servicio de auditoría.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a AuditDataAccess).
    /// Maneja logs, estadísticas y limpieza del sistema.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly AuditDataAccess _dataAccess;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            AuditDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<AuditService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Audit Logs

        /// <summary>
        /// Obtiene logs de auditoría con filtros.
        /// SP: app.sp_GetAuditLogs
        /// </summary>
        public async Task<List<AuditLogVM>> GetAuditLogsAsync(AuditLogFilterDTO filter)
        {
            try
            {
                // VALIDACIÓN: Delegada a AuditParametersValidator
                var filterErrors = AuditParametersValidator.ValidateAuditLogFilter(filter);

                if (filterErrors.Any())
                {
                    _logger.LogWarning(
                        "Filtros de auditoría inválidos: {Errors}",
                        string.Join(", ", filterErrors)
                    );
                    return new List<AuditLogVM>();
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetAuditLogsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener logs de auditoría con filtros");
                return new List<AuditLogVM>();
            }
        }

        /// <summary>
        /// Obtiene historial de auditoría de un usuario.
        /// SP: app.sp_GetUserAuditHistory
        /// </summary>
        public async Task<List<UserAuditHistoryVM>> GetUserAuditHistoryAsync(int userId, int top = 50)
        {
            try
            {
                // VALIDACIÓN: Delegada a AuditParametersValidator
                var topErrors = AuditParametersValidator.ValidateTopRecords(top);

                if (topErrors.Any())
                {
                    _logger.LogWarning(
                        "Parámetro TOP inválido para historial de usuario {UserId}: {Errors}",
                        userId,
                        string.Join(", ", topErrors)
                    );
                    return new List<UserAuditHistoryVM>();
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserAuditHistoryAsync(userId, top);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener historial de auditoría del usuario {UserId}",
                    userId
                );
                return new List<UserAuditHistoryVM>();
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados.
        /// SP: app.sp_CleanupExpiredSessions
        /// </summary>
        public async Task<CleanupResultVM> CleanupExpiredSessionsAsync(int retentionDays = 30)
        {
            try
            {
                // VALIDACIÓN: Delegada a AuditParametersValidator
                var retentionErrors = AuditParametersValidator.ValidateRetentionDays(retentionDays);

                if (retentionErrors.Any())
                {
                    _logger.LogWarning(
                        "Días de retención inválidos: {Errors}",
                        string.Join(", ", retentionErrors)
                    );

                    return new CleanupResultVM
                    {
                        DeletedSessions = 0,
                        DeletedResetTokens = 0,
                        Message = string.Join(" ", retentionErrors)
                    };
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.CleanupExpiredSessionsAsync(retentionDays);

                if (result != null)
                {
                    _logger.LogInformation(
                        "Limpieza completada: {DeletedSessions} sesiones, {DeletedTokens} tokens",
                        result.DeletedSessions,
                        result.DeletedResetTokens
                    );

                    return result;
                }

                return new CleanupResultVM
                {
                    DeletedSessions = 0,
                    DeletedResetTokens = 0,
                    Message = "Limpieza completada (sin registros para eliminar)."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar limpieza de sesiones expiradas");

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
        /// Obtiene estadísticas de auditoría.
        /// SP: app.sp_GetAuditStats (retorna 3 result sets)
        /// </summary>
        public async Task<AuditStatsVM> GetAuditStatsAsync(int days = 30)
        {
            try
            {
                // VALIDACIÓN: Delegada a AuditParametersValidator
                var daysErrors = AuditParametersValidator.ValidateStatsDays(days);

                if (daysErrors.Any())
                {
                    _logger.LogWarning(
                        "Días para estadísticas inválidos: {Errors}",
                        string.Join(", ", daysErrors)
                    );
                    return new AuditStatsVM();
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetAuditStatsAsync(days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de auditoría");
                return new AuditStatsVM();
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales del sistema.
        /// Combina múltiples VIEWs para dashboard administrativo.
        /// </summary>
        public async Task<object> GetSystemStatsAsync()
        {
            try
            {
                // TODO: Implementar cuando se definan las VIEWs específicas
                // Ejemplo de estructura esperada:
                // - Total de usuarios activos
                // - Total de ligas activas
                // - Total de equipos
                // - Total de sesiones activas
                // - Acciones en las últimas 24h

                _logger.LogInformation("GetSystemStatsAsync llamado (pendiente de implementación completa)");

                return new
                {
                    Message = "Estadísticas del sistema - Implementación pendiente",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del sistema");
                return new { Error = "Error al obtener estadísticas" };
            }
        }

        #endregion

        #region Direct Logging

        /// <summary>
        /// Registra una acción de auditoría directamente.
        /// Para casos especiales no cubiertos por SPs.
        /// </summary>
        public async Task LogActionAsync(
            int? actorUserId,
            string entityType,
            string entityId,
            string actionCode,
            string? details = null,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                await _dataAccess.LogActionAsync(
                    actorUserId,
                    entityType,
                    entityId,
                    actionCode,
                    details,
                    sourceIp,
                    userAgent
                );

                _logger.LogDebug(
                    "Acción registrada directamente: {ActionCode} en {EntityType}:{EntityId}",
                    actionCode,
                    entityType,
                    entityId
                );
            }
            catch (Exception ex)
            {
                // Log silently fails para no interrumpir el flujo
                _logger.LogWarning(
                    ex,
                    "Error al registrar acción de auditoría: {ActionCode} en {EntityType}:{EntityId}",
                    actionCode,
                    entityType,
                    entityId
                );
            }
        }

        #endregion
    }
}