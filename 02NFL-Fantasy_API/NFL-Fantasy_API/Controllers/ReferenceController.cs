using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de datos de referencia del sistema
    /// Endpoints: GetCurrentSeason, ListPositionFormats, GetPositionFormatSlots
    /// Todos los endpoints GET son públicos (no requieren autenticación)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReferenceController : ControllerBase
    {
        private readonly IReferenceService _referenceService;
        private readonly ILogger<ReferenceController> _logger;

        public ReferenceController(IReferenceService referenceService, ILogger<ReferenceController> logger)
        {
            _referenceService = referenceService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los formatos de posiciones disponibles
        /// GET /api/reference/position-formats
        /// Público - No requiere autenticación
        /// Formatos: Default, Extremo, Detallado, Ofensivo
        /// </summary>
        [HttpGet("position-formats")]
        public async Task<ActionResult> ListPositionFormats()
        {
            try
            {
                var formats = await _referenceService.ListPositionFormatsAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Formatos de posiciones obtenidos.", formats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing position formats");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener formatos."));
            }
        }

        /// <summary>
        /// Obtiene los slots de un formato de posiciones específico
        /// GET /api/reference/position-formats/{id}/slots
        /// Público - No requiere autenticación
        /// Ejemplo: Default tiene 1 QB, 2 RB, 2 WR, etc.
        /// </summary>
        [HttpGet("position-formats/{id}/slots")]
        public async Task<ActionResult> GetPositionFormatSlots(int id)
        {
            try
            {
                var slots = await _referenceService.GetPositionFormatSlotsAsync(id);

                if (slots == null || !slots.Any())
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Formato de posiciones no encontrado."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Slots de formato obtenidos.", slots));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slots for position format {FormatID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener slots."));
            }
        }
    }
}