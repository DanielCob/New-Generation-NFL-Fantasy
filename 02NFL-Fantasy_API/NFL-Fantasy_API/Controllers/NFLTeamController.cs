using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de equipos NFL (CRUD)
    /// Endpoints: Create, List, GetDetails, Update, Deactivate, Reactivate
    /// Feature 10.1: Gestión de Equipos NFL
    /// Requiere autenticación para todas las operaciones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NFLTeamController : ControllerBase
    {
        private readonly INFLTeamService _nflTeamService;
        private readonly ILogger<NFLTeamController> _logger;

        public NFLTeamController(INFLTeamService nflTeamService, ILogger<NFLTeamController> logger)
        {
            _nflTeamService = nflTeamService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo equipo NFL manualmente
        /// POST /api/nflteam
        /// Feature 10.1 - Crear equipo NFL
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateNFLTeam([FromBody] CreateNFLTeamDTO dto)
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
                var actorUserId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _nflTeamService.CreateNFLTeamAsync(dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} created NFL team: {TeamName} from {IP}",
                        actorUserId, dto.TeamName, sourceIp);
                    return CreatedAtAction(nameof(GetNFLTeamDetails),
                        new { id = ((CreateNFLTeamResponseDTO?)result.Data)?.NFLTeamID ?? 0 },
                        result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating NFL team");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al crear equipo NFL."));
            }
        }

        /// <summary>
        /// Lista equipos NFL con paginación y filtros
        /// GET /api/nflteam
        /// Feature 10.1 - Listar equipos NFL
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> ListNFLTeams([FromQuery] ListNFLTeamsRequestDTO request)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var result = await _nflTeamService.ListNFLTeamsAsync(request);
                return Ok(ApiResponseDTO.SuccessResponse("Equipos NFL obtenidos.", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing NFL teams");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener equipos NFL."));
            }
        }

        /// <summary>
        /// Obtiene detalles completos de un equipo NFL
        /// GET /api/nflteam/{id}
        /// Feature 10.1 - Ver detalles de equipo NFL
        /// Retorna: información del equipo + historial de cambios + jugadores activos
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult> GetNFLTeamDetails(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var details = await _nflTeamService.GetNFLTeamDetailsAsync(id);

                if (details == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Equipo NFL no encontrado."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Detalles de equipo NFL obtenidos.", details));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting NFL team details for {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener detalles."));
            }
        }

        /// <summary>
        /// Actualiza un equipo NFL existente
        /// PUT /api/nflteam/{id}
        /// Feature 10.1 - Modificar equipo NFL
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateNFLTeam(int id, [FromBody] UpdateNFLTeamDTO dto)
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
                var actorUserId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _nflTeamService.UpdateNFLTeamAsync(id, dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} updated NFL team {TeamID} from {IP}",
                        actorUserId, id, sourceIp);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating NFL team {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al actualizar equipo NFL."));
            }
        }

        /// <summary>
        /// Desactiva un equipo NFL
        /// POST /api/nflteam/{id}/deactivate
        /// Feature 10.1 - Desactivar equipo NFL
        /// Valida que no tenga partidos programados en temporada actual
        /// Solo ADMIN puede desactivar equipos NFL
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> DeactivateNFLTeam(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var actorUserId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _nflTeamService.DeactivateNFLTeamAsync(id, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} deactivated NFL team {TeamID} from {IP}",
                        actorUserId, id, sourceIp);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating NFL team {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al desactivar equipo NFL."));
            }
        }

        /// <summary>
        /// Reactiva un equipo NFL desactivado
        /// POST /api/nflteam/{id}/reactivate
        /// Feature 10.1 - Reactivar equipo NFL
        /// Solo ADMIN puede reactivar equipos NFL
        /// </summary>
        [HttpPost("{id}/reactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> ReactivateNFLTeam(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var actorUserId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _nflTeamService.ReactivateNFLTeamAsync(id, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} reactivated NFL team {TeamID} from {IP}",
                        actorUserId, id, sourceIp);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating NFL team {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al reactivar equipo NFL."));
            }
        }

        /// <summary>
        /// Obtiene equipos NFL activos (para dropdowns)
        /// GET /api/nflteam/active
        /// Feature 10.1 - Selector de equipos NFL
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult> GetActiveNFLTeams()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var teams = await _nflTeamService.GetActiveNFLTeamsAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Equipos NFL activos obtenidos.", teams));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active NFL teams");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener equipos activos."));
            }
        }
    }
}