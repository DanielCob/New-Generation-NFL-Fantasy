using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de esquemas de puntuación
    /// Endpoints: ListSchemas, GetSchemaRules
    /// Todos los endpoints GET son públicos (no requieren autenticación)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ScoringController : ControllerBase
    {
        private readonly IScoringService _scoringService;
        private readonly ILogger<ScoringController> _logger;

        public ScoringController(IScoringService scoringService, ILogger<ScoringController> logger)
        {
            _scoringService = scoringService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los esquemas de puntuación disponibles
        /// GET /api/scoring/schemas
        /// Público - No requiere autenticación
        /// Esquemas: Default, PrioridadCarrera, MaxPuntos, PrioridadDefensa
        /// </summary>
        [HttpGet("schemas")]
        public async Task<ActionResult> ListSchemas()
        {
            try
            {
                var schemas = await _scoringService.ListSchemasAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Esquemas de puntuación obtenidos.", schemas));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing scoring schemas");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener esquemas."));
            }
        }

        /// <summary>
        /// Obtiene las reglas de puntuación de un esquema específico
        /// GET /api/scoring/schemas/{id}/rules
        /// Público - No requiere autenticación
        /// Muestra cómo se puntúa cada métrica: PASS_YDS, PASS_TD, RUSH_YDS, etc.
        /// </summary>
        [HttpGet("schemas/{id}/rules")]
        public async Task<ActionResult> GetSchemaRules(int id)
        {
            try
            {
                var rules = await _scoringService.GetSchemaRulesAsync(id);

                if (rules == null || !rules.Any())
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Esquema de puntuación no encontrado."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Reglas de puntuación obtenidas.", rules));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rules for scoring schema {SchemaID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener reglas."));
            }
        }
    }
}