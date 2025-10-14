using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de auditoría y mantenimiento del sistema
    /// Endpoints: GetAuditLogs, GetUserHistory, GetStats, CleanupSessions
    /// Todos requieren autenticación; algunos requieren ADMIN (futuro)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        /// Obtiene logs de auditoría con filtros opcionales
        /// GET /api/audit/logs
        /// Requiere autenticación (en futuro, solo ADMIN)
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult> GetAuditLogs([FromQuery] AuditLogFilterDTO filter)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            // TODO: Verificar rol ADMIN cuando se implemente
            // if (!IsAdmin()) return Forbid();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var logs = await _auditService.GetAuditLogsAsync(filter);
                return Ok(ApiResponseDTO.SuccessResponse("Logs de auditoría obtenidos.", logs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener logs."));
            }
        }

        /// <summary>
        /// Obtiene historial de auditoría del usuario autenticado
        /// GET /api/audit/my-history
        /// El usuario solo puede ver su propio historial
        /// </summary>
        [HttpGet("my-history")]
        public async Task<ActionResult> GetMyAuditHistory([FromQuery] int top = 50)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var history = await _auditService.GetUserAuditHistoryAsync(userId, top);
                return Ok(ApiResponseDTO.SuccessResponse("Historial de auditoría obtenido.", history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user audit history for {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener historial."));
            }
        }

        /// <summary>
        /// Obtiene historial de auditoría de un usuario específico
        /// GET /api/audit/users/{userId}/history
        /// Requiere ADMIN (por ahora permitido a cualquier autenticado)
        /// </summary>
        [HttpGet("users/{userId}/history")]
        public async Task<ActionResult> GetUserAuditHistory(int userId, [FromQuery] int top = 50)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            // TODO: Verificar rol ADMIN
            // if (!IsAdmin()) return Forbid();

            try
            {
                var history = await _auditService.GetUserAuditHistoryAsync(userId, top);
                return Ok(ApiResponseDTO.SuccessResponse("Historial de auditoría obtenido.", history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit history for user {UserID}", userId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener historial."));
            }
        }

        /// <summary>
        /// Obtiene estadísticas de auditoría
        /// GET /api/audit/stats
        /// Requiere ADMIN (por ahora permitido a cualquier autenticado)
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetAuditStats([FromQuery] AuditStatsRequestDTO request)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var stats = await _auditService.GetAuditStatsAsync(request.Days);
                return Ok(ApiResponseDTO.SuccessResponse("Estadísticas de auditoría obtenidas.", stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit stats");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener estadísticas."));
            }
        }

        /// <summary>
        /// Ejecuta limpieza de sesiones y tokens expirados
        /// POST /api/audit/cleanup
        /// Requiere ADMIN (por ahora permitido a cualquier autenticado)
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupSessions([FromBody] CleanupRequestDTO request)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                _logger.LogInformation("User {UserID} initiated cleanup with retention {Days} days",
                    userId, request.RetentionDays);

                var result = await _auditService.CleanupExpiredSessionsAsync(request.RetentionDays);

                return Ok(ApiResponseDTO.SuccessResponse("Limpieza ejecutada.", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al ejecutar limpieza."));
            }
        }
    }
}