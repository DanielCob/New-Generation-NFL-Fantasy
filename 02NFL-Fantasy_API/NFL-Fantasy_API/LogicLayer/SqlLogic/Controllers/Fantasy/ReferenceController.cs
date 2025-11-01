using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Fantasy
{
    /// <summary>
    /// Controller de datos de referencia del sistema.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para datos de referencia
    /// - Formatos de posiciones disponibles
    /// - Slots por formato de posiciones
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints son públicos (no requieren autenticación)
    /// - Datos de solo lectura (catálogos del sistema)
    /// 
    /// ENDPOINTS:
    /// - GET /api/reference/position-formats - Lista todos los formatos disponibles
    /// - GET /api/reference/position-formats/{id}/slots - Slots de un formato específico
    /// 
    /// Feature 1.2: Creación y administración de ligas
    /// </summary>
    [ApiController]
    [Route("api/reference")]
    [AllowAnonymous] // Todos los endpoints son públicos
    public class ReferenceController : ControllerBase
    {
        private readonly IReferenceService _referenceService;
        private readonly ILogger<ReferenceController> _logger;

        public ReferenceController(
            IReferenceService referenceService,
            ILogger<ReferenceController> logger)
        {
            _referenceService = referenceService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los formatos de posiciones disponibles.
        /// GET /api/reference/position-formats
        /// </summary>
        /// <returns>Lista de formatos de posiciones con sus configuraciones</returns>
        /// <response code="200">Formatos obtenidos exitosamente</response>
        /// <remarks>
        /// FORMATOS DISPONIBLES:
        /// - Default: Formato estándar (1 QB, 2 RB, 2 WR, 1 RB/WR, 1 TE, 1 K, 1 DEF, 6 BENCH, 3 IR)
        /// - Extremo: Formato con más posiciones flexibles
        /// - Detallado: Formato con posiciones específicas adicionales
        /// - Ofensivo: Solo posiciones ofensivas (sin DEF/K)
        /// 
        /// Usado al crear una liga para seleccionar la configuración de roster.
        /// </remarks>
        [HttpGet("position-formats")]
        public async Task<ActionResult<ApiResponseDTO>> ListPositionFormats()
        {
            var formats = await _referenceService.ListPositionFormatsAsync();

            return Ok(ApiResponseDTO.SuccessResponse(
                "Formatos de posiciones obtenidos exitosamente.",
                formats
            ));
        }

        /// <summary>
        /// Obtiene los slots de un formato de posiciones específico.
        /// GET /api/reference/position-formats/{id}/slots
        /// </summary>
        /// <param name="id">ID del formato de posiciones</param>
        /// <returns>Lista de slots con cantidad por posición</returns>
        /// <response code="200">Slots obtenidos exitosamente</response>
        /// <response code="404">Formato de posiciones no encontrado</response>
        /// <remarks>
        /// ESTRUCTURA DE UN SLOT:
        /// - PositionFormatID: ID del formato al que pertenece
        /// - FormatName: Nombre del formato (ej: "Default")
        /// - PositionCode: Código de la posición (QB, RB, WR, TE, RB/WR, K, DEF, BENCH, IR)
        /// - SlotCount: Cantidad de slots para esa posición
        /// 
        /// EJEMPLO (Formato Default):
        /// - 1 QB (Quarterback)
        /// - 2 RB (Running Back)
        /// - 2 WR (Wide Receiver)
        /// - 1 RB/WR (Flex RB o WR)
        /// - 1 TE (Tight End)
        /// - 1 K (Kicker)
        /// - 1 DEF (Defense/Special Teams)
        /// - 6 BENCH (Banca)
        /// - 3 IR (Injured Reserve)
        /// 
        /// Usado para validar configuración de roster y mostrar en UI.
        /// </remarks>
        [HttpGet("position-formats/{id}/slots")]
        public async Task<ActionResult<ApiResponseDTO>> GetPositionFormatSlots(int id)
        {
            var slots = await _referenceService.GetPositionFormatSlotsAsync(id);

            if (slots == null || !slots.Any())
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Formato de posiciones no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Slots de formato obtenidos exitosamente.",
                slots
            ));
        }
    }
}