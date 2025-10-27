using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Administración de Temporadas (ADMIN ONLY)
    /// Rutas:
    ///   POST   /api/seasons                     -> crear temporada
    ///   PUT    /api/seasons/{id}                -> actualizar temporada
    ///   POST   /api/seasons/{id}/deactivate     -> desactivar temporada actual
    ///   DELETE /api/seasons/{id}                -> eliminar temporada
    ///   GET    /api/seasons/{id}                -> obtener temporada por id
    ///   GET    /api/seasons/{id}/weeks          -> listar semanas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class SeasonsController : ControllerBase
    {
        private readonly ISeasonService _seasonService;
        private readonly ILogger<SeasonsController> _logger;

        public SeasonsController(ISeasonService seasonService, ILogger<SeasonsController> logger)
        {
            _seasonService = seasonService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la temporada actual (IsCurrent=1)
        /// GET /api/seasons/current
        /// Público - No requiere autenticación
        /// </summary>
        [HttpGet("current")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCurrent()
        {
            try
            {
                var season = await _seasonService.GetCurrentSeasonAsync();
                if (season == null)
                    return NotFound(ApiResponseDTO.ErrorResponse("No hay temporada actual configurada."));

                return Ok(ApiResponseDTO.SuccessResponse("Temporada actual obtenida.", season));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current season");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener temporada."));
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateSeasonRequestDTO dto)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                var ip = HttpContext.GetClientIpAddress();
                var ua = HttpContext.GetUserAgent();

                var season = await _seasonService.CreateSeasonAsync(dto, userId, ip, ua);
                return Ok(ApiResponseDTO.SuccessResponse("Temporada creada.", season));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating season");
                return StatusCode(500, ApiResponseDTO.ErrorResponse(ex.Message));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update([FromRoute] int id, [FromBody] UpdateSeasonRequestDTO dto)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                var ip = HttpContext.GetClientIpAddress();
                var ua = HttpContext.GetUserAgent();

                var season = await _seasonService.UpdateSeasonAsync(id, dto, userId, ip, ua);
                return Ok(ApiResponseDTO.SuccessResponse("Temporada actualizada.", season));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating season {SeasonID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult> Deactivate([FromRoute] int id, [FromBody] ConfirmActionDTO payload)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                var ip = HttpContext.GetClientIpAddress();
                var ua = HttpContext.GetUserAgent();

                var message = await _seasonService.DeactivateSeasonAsync(id, payload?.Confirm == true, userId, ip, ua);
                return Ok(ApiResponseDTO.SuccessResponse(message, new { SeasonID = id }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating season {SeasonID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse(ex.Message));
            }
        }

        // Nota: algunos clientes no envían body en DELETE, aceptamos query ?confirm=true como fallback
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete([FromRoute] int id, [FromBody] ConfirmActionDTO? payload, [FromQuery] bool? confirm = null)
        {
            try
            {
                var userId = HttpContext.GetUserId();
                var ip = HttpContext.GetClientIpAddress();
                var ua = HttpContext.GetUserAgent();
                var doConfirm = (payload?.Confirm == true) || (confirm == true);

                var message = await _seasonService.DeleteSeasonAsync(id, doConfirm, userId, ip, ua);
                return Ok(ApiResponseDTO.SuccessResponse(message, new { SeasonID = id }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting season {SeasonID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById([FromRoute] int id)
        {
            try
            {
                var season = await _seasonService.GetSeasonByIdAsync(id);
                if (season == null)
                    return NotFound(ApiResponseDTO.ErrorResponse("Temporada no encontrada."));
                return Ok(ApiResponseDTO.SuccessResponse("Temporada obtenida.", season));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting season {SeasonID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener temporada."));
            }
        }

        [HttpGet("{id:int}/weeks")]
        public async Task<ActionResult> GetWeeks([FromRoute] int id)
        {
            try
            {
                var weeks = await _seasonService.GetSeasonWeeksAsync(id);
                return Ok(ApiResponseDTO.SuccessResponse("Semanas obtenidas.", weeks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weeks for season {SeasonID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener semanas."));
            }
        }
    }
}
