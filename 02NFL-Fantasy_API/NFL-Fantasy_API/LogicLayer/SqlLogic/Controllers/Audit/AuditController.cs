using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Audit;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Audit;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Audit
{
    /// <summary>
    /// Controller de auditoría y mantenimiento del sistema.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para auditoría
    /// - Consultar logs de auditoría
    /// - Ver historial de acciones de usuarios
    /// - Obtener estadísticas del sistema
    /// - Ejecutar limpieza de sesiones expiradas
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Algunos requieren rol ADMIN (implementación futura)
    /// 
    /// ENDPOINTS:
    /// - GET /api/audit/logs - Logs del sistema (futuro: solo ADMIN)
    /// - GET /api/audit/my-history - Historial del usuario actual
    /// - GET /api/audit/users/{userId}/history - Historial de usuario específico (futuro: solo ADMIN)
    /// - GET /api/audit/stats - Estadísticas de auditoría (futuro: solo ADMIN)
    /// - POST /api/audit/cleanup - Limpieza de sesiones expiradas (futuro: solo ADMIN)
    /// </summary>
    [ApiController]
    [Route("api/audit")]
    [Authorize] // Todos los endpoints requieren autenticación
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene logs de auditoría con filtros opcionales.
        /// GET /api/audit/logs
        /// </summary>
        /// <param name="filter">Filtros de búsqueda (fechas, tipo de acción, usuario, etc.)</param>
        /// <returns>Lista de logs de auditoría</returns>
        /// <response code="200">Logs obtenidos exitosamente</response>
        /// <response code="401">No autenticado</response>
        /// <remarks>
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// </remarks>
        [HttpGet("logs")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> GetAuditLogs([FromQuery] AuditLogFilterDTO filter)
        {
            var logs = await _auditService.GetAuditLogsAsync(filter);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Logs de auditoría obtenidos exitosamente.",
                logs
            ));
        }

        /// <summary>
        /// Obtiene historial de auditoría del usuario autenticado.
        /// GET /api/audit/my-history
        /// </summary>
        /// <param name="top">Cantidad de registros a retornar (máximo 500, default 50)</param>
        /// <returns>Historial de acciones del usuario</returns>
        /// <response code="200">Historial obtenido exitosamente</response>
        /// <remarks>
        /// El usuario solo puede ver su propio historial.
        /// </remarks>
        [HttpGet("my-history")]
        public async Task<ActionResult<ApiResponseDTO>> GetMyAuditHistory([FromQuery] int top = 50)
        {
            // Validar rango de 'top'
            if (top < 1) top = 50;
            if (top > 500) top = 500;

            var userId = this.UserId();
            var history = await _auditService.GetUserAuditHistoryAsync(userId, top);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Historial de auditoría obtenido exitosamente.",
                history
            ));
        }

        /// <summary>
        /// Obtiene historial de auditoría de un usuario específico.
        /// GET /api/audit/users/{userId}/history
        /// </summary>
        /// <param name="userId">ID del usuario a consultar</param>
        /// <param name="top">Cantidad de registros a retornar (máximo 500, default 50)</param>
        /// <returns>Historial de acciones del usuario especificado</returns>
        /// <response code="200">Historial obtenido exitosamente</response>
        /// <remarks>
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// </remarks>
        [HttpGet("users/{userId}/history")]
        // [Authorize(Policy = "AdminOnly")] // Descomentar cuando se implemente
        public async Task<ActionResult<ApiResponseDTO>> GetUserAuditHistory(
            int userId,
            [FromQuery] int top = 50)
        {
            // Validar rango de 'top'
            if (top < 1) top = 50;
            if (top > 500) top = 500;

            var history = await _auditService.GetUserAuditHistoryAsync(userId, top);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Historial de auditoría obtenido exitosamente.",
                history
            ));
        }

        /// <summary>
        /// Obtiene estadísticas de auditoría del sistema.
        /// GET /api/audit/stats
        /// </summary>
        /// <param name="request">Parámetros de consulta (días a analizar)</param>
        /// <returns>Estadísticas agregadas de auditoría</returns>
        /// <response code="200">Estadísticas obtenidas exitosamente</response>
        /// <remarks>
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// 
        /// INCLUYE:
        /// - Total de acciones por tipo
        /// - Usuarios más activos
        /// - Distribución temporal de acciones
        /// </remarks>
        [HttpGet("stats")]
        // [Authorize(Policy = "AdminOnly")] // Descomentar cuando se implemente
        public async Task<ActionResult<ApiResponseDTO>> GetAuditStats([FromQuery] AuditStatsRequestDTO request)
        {
            var stats = await _auditService.GetAuditStatsAsync(request.Days);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Estadísticas de auditoría obtenidas exitosamente.",
                stats
            ));
        }

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados.
        /// POST /api/audit/cleanup
        /// </summary>
        /// <param name="request">Parámetros de limpieza (días de retención)</param>
        /// <returns>Resultado de la operación de limpieza</returns>
        /// <response code="200">Limpieza ejecutada exitosamente</response>
        /// <remarks>
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// 
        /// LIMPIA:
        /// - Sesiones expiradas
        /// - Tokens de restablecimiento de contraseña expirados
        /// - Logs de auditoría antiguos (según configuración)
        /// 
        /// PARÁMETROS:
        /// - RetentionDays: Días de retención (1-180, default 30)
        /// </remarks>
        [HttpPost("cleanup")]
        // [Authorize(Policy = "AdminOnly")] // Descomentar cuando se implemente
        public async Task<ActionResult<ApiResponseDTO>> CleanupSessions([FromBody] CleanupRequestDTO request)
        {
            var userId = this.UserId();

            _logger.LogInformation(
                "User {UserID} initiated cleanup with retention {Days} days",
                userId,
                request.RetentionDays
            );

            var result = await _auditService.CleanupExpiredSessionsAsync(request.RetentionDays);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Limpieza ejecutada exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene estadísticas generales del sistema.
        /// GET /api/audit/system-stats
        /// </summary>
        /// <returns>Objeto con estadísticas del sistema</returns>
        /// <response code="200">Estadísticas obtenidas exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// 
        /// INCLUYE:
        /// - Total de usuarios (activos/inactivos)
        /// - Total de ligas (por estado)
        /// - Total de equipos
        /// - Sesiones activas
        /// - Otras métricas del sistema
        /// 
        /// Dashboard con métricas para administradores.
        /// </remarks>
        [HttpGet("system-stats")]
        // [Authorize(Policy = "AdminOnly")] // Descomentar cuando se implemente
        public async Task<ActionResult<ApiResponseDTO>> GetSystemStats()
        {
            var stats = await _auditService.GetSystemStatsAsync();

            return Ok(ApiResponseDTO.SuccessResponse(
                "Estadísticas del sistema obtenidas exitosamente.",
                stats
            ));
        }
    }
}