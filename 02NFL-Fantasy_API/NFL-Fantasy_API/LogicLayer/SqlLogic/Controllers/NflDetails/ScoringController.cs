using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de esquemas de puntuación.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para esquemas de puntuación
    /// - Listar esquemas disponibles
    /// - Consultar reglas de puntuación por esquema
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints son públicos (no requieren autenticación)
    /// - Datos de solo lectura (catálogos del sistema)
    /// 
    /// ENDPOINTS:
    /// - GET /api/scoring/schemas - Lista todos los esquemas disponibles
    /// - GET /api/scoring/schemas/{id}/rules - Reglas de un esquema específico
    /// 
    /// Feature 1.2: Editar configuración de liga (seleccionar esquema)
    /// </summary>
    [ApiController]
    [Route("api/scoring")]
    [AllowAnonymous] // Todos los endpoints son públicos
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
        /// Lista todos los esquemas de puntuación disponibles.
        /// GET /api/scoring/schemas
        /// </summary>
        /// <returns>Lista de esquemas de puntuación</returns>
        /// <response code="200">Esquemas obtenidos exitosamente</response>
        /// <remarks>
        /// ESQUEMAS DISPONIBLES:
        /// - Default: Esquema estándar balanceado
        /// - PrioridadCarrera: Favorece RBs y carreras
        /// - MaxPuntos: Esquema con puntuaciones más altas
        /// - PrioridadDefensa: Favorece defensas y turnovers
        /// 
        /// Usado al crear una liga para seleccionar el sistema de puntuación.
        /// Público - No requiere autenticación.
        /// </remarks>
        [HttpGet("schemas")]
        public async Task<ActionResult<ApiResponseDTO>> ListSchemas()
        {
            var schemas = await _scoringService.ListSchemasAsync();
            if (schemas is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los esquemas de puntuación."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Esquemas de puntuación obtenidos exitosamente.",
                schemas
            ));
        }

        /// <summary>
        /// Obtiene las reglas de puntuación de un esquema específico.
        /// GET /api/scoring/schemas/{id}/rules
        /// </summary>
        /// <param name="id">ID del esquema de puntuación</param>
        /// <returns>Lista de reglas con métricas y puntos</returns>
        /// <response code="200">Reglas obtenidas exitosamente</response>
        /// <response code="404">Esquema de puntuación no encontrado</response>
        /// <remarks>
        /// MÉTRICAS TÍPICAS:
        /// - PASS_YDS: Yardas por pase
        /// - PASS_TD: Touchdowns por pase
        /// - RUSH_YDS: Yardas por carrera
        /// - RUSH_TD: Touchdowns por carrera
        /// - REC: Recepciones (PPR)
        /// - REC_YDS: Yardas por recepción
        /// - REC_TD: Touchdowns por recepción
        /// - INT: Intercepciones
        /// - FUM_LOST: Fumbles perdidos
        /// 
        /// Cada regla especifica cuántos puntos otorga cada métrica.
        /// Público - No requiere autenticación.
        /// </remarks>
        [HttpGet("schemas/{id}/rules")]
        public async Task<ActionResult<ApiResponseDTO>> GetSchemaRules(int id)
        {
            var rules = await _scoringService.GetSchemaRulesAsync(id);

            if (rules == null || !rules.Any())
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Esquema de puntuación no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Reglas de puntuación obtenidas exitosamente.",
                rules
            ));
        }
    }
}