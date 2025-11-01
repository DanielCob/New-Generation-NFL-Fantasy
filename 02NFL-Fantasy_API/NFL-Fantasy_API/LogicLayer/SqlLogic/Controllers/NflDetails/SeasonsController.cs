using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de administración de temporadas NFL.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para temporadas
    /// - CRUD completo de temporadas
    /// - Activar/desactivar temporadas
    /// - Consultar semanas de temporada
    /// 
    /// SEGURIDAD:
    /// - La mayoría de endpoints requieren rol ADMIN
    /// - GetCurrent es público (no requiere autenticación)
    /// 
    /// ENDPOINTS:
    /// - GET /api/seasons/current - Temporada actual (PÚBLICO)
    /// - POST /api/seasons - Crear temporada (ADMIN)
    /// - PUT /api/seasons/{id} - Actualizar temporada (ADMIN)
    /// - POST /api/seasons/{id}/deactivate - Desactivar temporada (ADMIN)
    /// - DELETE /api/seasons/{id} - Eliminar temporada (ADMIN)
    /// - GET /api/seasons/{id} - Obtener temporada por ID (ADMIN)
    /// - GET /api/seasons/{id}/weeks - Listar semanas (ADMIN)
    /// </summary>
    [ApiController]
    [Route("api/seasons")]
    [Authorize(Policy = "AdminOnly")] // Por defecto todos requieren ADMIN
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
        /// Obtiene la temporada actual (IsCurrent=1).
        /// GET /api/seasons/current
        /// </summary>
        /// <returns>Información de la temporada actual</returns>
        /// <response code="200">Temporada actual obtenida exitosamente</response>
        /// <response code="404">No hay temporada actual configurada</response>
        /// <remarks>
        /// Público - No requiere autenticación.
        /// Usado para saber qué temporada está activa en el sistema.
        /// </remarks>
        [HttpGet("current")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> GetCurrent()
        {
            var season = await _seasonService.GetCurrentSeasonAsync();

            if (season == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "No hay temporada actual configurada."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Temporada actual obtenida exitosamente.",
                season
            ));
        }

        /// <summary>
        /// Crea una nueva temporada.
        /// POST /api/seasons
        /// </summary>
        /// <param name="dto">Datos de la temporada a crear</param>
        /// <returns>Temporada creada</returns>
        /// <response code="200">Temporada creada exitosamente</response>
        /// <response code="400">Datos inválidos o temporada duplicada</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear temporadas.
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<ApiResponseDTO>> Create([FromBody] CreateSeasonRequestDTO dto)
        {
            var userId = this.UserId();
            var ip = this.ClientIp();
            var ua = this.UserAgent();

            var season = await _seasonService.CreateSeasonAsync(dto, userId, ip, ua);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Temporada creada exitosamente.",
                season
            ));
        }

        /// <summary>
        /// Actualiza una temporada existente.
        /// PUT /api/seasons/{id}
        /// </summary>
        /// <param name="id">ID de la temporada</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <returns>Temporada actualizada</returns>
        /// <response code="200">Temporada actualizada exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Temporada no encontrada</response>
        /// <remarks>
        /// Solo ADMIN puede actualizar temporadas.
        /// </remarks>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponseDTO>> Update(
            [FromRoute] int id,
            [FromBody] UpdateSeasonRequestDTO dto)
        {
            var userId = this.UserId();
            var ip = this.ClientIp();
            var ua = this.UserAgent();

            var season = await _seasonService.UpdateSeasonAsync(id, dto, userId, ip, ua);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Temporada actualizada exitosamente.",
                season
            ));
        }

        /// <summary>
        /// Desactiva la temporada actual.
        /// POST /api/seasons/{id}/deactivate
        /// </summary>
        /// <param name="id">ID de la temporada</param>
        /// <param name="payload">Confirmación de acción</param>
        /// <returns>Mensaje de confirmación</returns>
        /// <response code="200">Temporada desactivada exitosamente</response>
        /// <response code="400">Confirmación requerida o acción no permitida</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Requiere confirmación explícita (Confirm=true).
        /// Solo ADMIN puede desactivar temporadas.
        /// </remarks>
        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<ApiResponseDTO>> Deactivate(
            [FromRoute] int id,
            [FromBody] ConfirmActionDTO payload)
        {
            var userId = this.UserId();
            var ip = this.ClientIp();
            var ua = this.UserAgent();

            var message = await _seasonService.DeactivateSeasonAsync(
                id,
                payload?.Confirm == true,
                userId,
                ip,
                ua
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                message,
                new { SeasonID = id }
            ));
        }

        /// <summary>
        /// Elimina una temporada.
        /// DELETE /api/seasons/{id}
        /// </summary>
        /// <param name="id">ID de la temporada</param>
        /// <param name="payload">Confirmación de acción (body)</param>
        /// <param name="confirm">Confirmación de acción (query, fallback)</param>
        /// <returns>Mensaje de confirmación</returns>
        /// <response code="200">Temporada eliminada exitosamente</response>
        /// <response code="400">Confirmación requerida o acción no permitida</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Requiere confirmación explícita (Confirm=true en body o query).
        /// Solo ADMIN puede eliminar temporadas.
        /// 
        /// NOTA: Algunos clientes no envían body en DELETE, 
        /// por eso se acepta ?confirm=true como fallback.
        /// </remarks>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponseDTO>> Delete(
            [FromRoute] int id,
            [FromBody] ConfirmActionDTO? payload = null,
            [FromQuery] bool? confirm = null)
        {
            var userId = this.UserId();
            var ip = this.ClientIp();
            var ua = this.UserAgent();
            var doConfirm = payload?.Confirm == true || confirm == true;

            var message = await _seasonService.DeleteSeasonAsync(
                id,
                doConfirm,
                userId,
                ip,
                ua
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                message,
                new { SeasonID = id }
            ));
        }

        /// <summary>
        /// Obtiene una temporada por ID.
        /// GET /api/seasons/{id}
        /// </summary>
        /// <param name="id">ID de la temporada</param>
        /// <returns>Información de la temporada</returns>
        /// <response code="200">Temporada obtenida exitosamente</response>
        /// <response code="404">Temporada no encontrada</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede consultar temporadas por ID.
        /// </remarks>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponseDTO>> GetById([FromRoute] int id)
        {
            var season = await _seasonService.GetSeasonByIdAsync(id);

            if (season == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Temporada no encontrada."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Temporada obtenida exitosamente.",
                season
            ));
        }

        /// <summary>
        /// Obtiene las semanas de una temporada.
        /// GET /api/seasons/{id}/weeks
        /// </summary>
        /// <param name="id">ID de la temporada</param>
        /// <returns>Lista de semanas de la temporada</returns>
        /// <response code="200">Semanas obtenidas exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede consultar semanas de temporadas.
        /// Retorna todas las semanas (regulares y playoffs) de la temporada especificada.
        /// </remarks>
        [HttpGet("{id:int}/weeks")]
        public async Task<ActionResult<ApiResponseDTO>> GetWeeks([FromRoute] int id)
        {
            var weeks = await _seasonService.GetSeasonWeeksAsync(id);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Semanas obtenidas exitosamente.",
                weeks
            ));
        }
    }
}