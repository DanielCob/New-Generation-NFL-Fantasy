using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de vistas y reportes administrativos
    /// Endpoints: GetLeagueSummaryView, GetAllLeagues, GetActiveUsers, GetSystemStats
    /// Todos los endpoints requieren autenticación
    /// En futuras versiones, podrían requerir rol ADMIN
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ViewsController : ControllerBase
    {
        private readonly IViewsService _viewsService;
        private readonly ILogger<ViewsController> _logger;

        public ViewsController(IViewsService viewsService, ILogger<ViewsController> logger)
        {
            _viewsService = viewsService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (sin equipos)
        /// GET /api/views/leagues/{id}/summary
        /// Alternativa ligera a /api/league/{id}/summary
        /// </summary>
        [HttpGet("leagues/{id}/summary")]
        public async Task<ActionResult> GetLeagueSummaryView(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var summary = await _viewsService.GetLeagueSummaryViewAsync(id);

                if (summary == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Liga no encontrada."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Resumen de liga obtenido.", summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league summary view for {LeagueID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener resumen."));
            }
        }

        /// <summary>
        /// Obtiene el directorio completo de todas las ligas
        /// GET /api/views/leagues/directory
        /// Para dashboards administrativos
        /// </summary>
        [HttpGet("leagues/directory")]
        public async Task<ActionResult> GetAllLeagues()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var leagues = await _viewsService.GetAllLeaguesAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Directorio de ligas obtenido.", leagues));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all leagues");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener ligas."));
            }
        }

        /// <summary>
        /// Obtiene todos los usuarios activos del sistema
        /// GET /api/views/users/active
        /// Para reportes administrativos
        /// </summary>
        [HttpGet("users/active")]
        public async Task<ActionResult> GetActiveUsers()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var users = await _viewsService.GetActiveUsersAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Usuarios activos obtenidos.", users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener usuarios."));
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales del sistema
        /// GET /api/views/system/stats
        /// Dashboard con métricas de usuarios, ligas, equipos y sesiones
        /// </summary>
        [HttpGet("system/stats")]
        public async Task<ActionResult> GetSystemStats()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var stats = await _viewsService.GetSystemStatsAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Estadísticas del sistema obtenidas.", stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system stats");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener estadísticas."));
            }
        }
    }
}