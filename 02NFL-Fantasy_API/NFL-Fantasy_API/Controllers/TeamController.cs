using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de equipos fantasy
    /// Endpoints: UpdateBranding, GetMyTeam, AddPlayer, RemovePlayer, GetDistribution
    /// Feature 3.1: Creación y administración de equipos fantasy
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly ILogger<TeamController> _logger;

        public TeamController(ITeamService teamService, ILogger<TeamController> logger)
        {
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Actualiza el branding de un equipo fantasy (nombre e imagen)
        /// PUT /api/team/{id}/branding
        /// Feature 3.1 - Editar branding de equipo
        /// Solo el dueño del equipo puede editarlo
        /// </summary>
        [HttpPut("{id}/branding")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateTeamBranding(int id, [FromBody] UpdateTeamBrandingDTO dto)
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

                var result = await _teamService.UpdateTeamBrandingAsync(id, dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} updated branding for team {TeamID} from {IP}",
                        actorUserId, id, sourceIp);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team branding for {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al actualizar branding."));
            }
        }

        /// <summary>
        /// Obtiene información completa del equipo con roster
        /// GET /api/team/{id}/my-team
        /// Feature 3.1 - Ver mi equipo
        /// Retorna: info del equipo + roster de jugadores + distribución porcentual
        /// </summary>
        [HttpGet("{id}/my-team")]
        public async Task<ActionResult> GetMyTeam(int id, [FromQuery] string? filterPosition, [FromQuery] string? searchPlayer)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var actorUserId = HttpContext.GetUserId();
                var myTeam = await _teamService.GetMyTeamAsync(id, actorUserId, filterPosition, searchPlayer);

                if (myTeam == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Equipo no encontrado o sin permisos."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Información del equipo obtenida.", myTeam));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my team for {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener equipo."));
            }
        }

        /// <summary>
        /// Obtiene distribución porcentual del roster por tipo de adquisición
        /// GET /api/team/{id}/roster/distribution
        /// Feature 3.1 - Distribución de adquisición
        /// </summary>
        [HttpGet("{id}/roster/distribution")]
        public async Task<ActionResult> GetRosterDistribution(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var distribution = await _teamService.GetTeamRosterDistributionAsync(id);
                return Ok(ApiResponseDTO.SuccessResponse("Distribución de roster obtenida.", distribution));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roster distribution for {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener distribución."));
            }
        }

        /// <summary>
        /// Agrega un jugador al roster del equipo
        /// POST /api/team/{id}/roster/add
        /// Feature 3.1 - Gestión de roster (futuro)
        /// </summary>
        [HttpPost("{id}/roster/add")]
        public async Task<ActionResult<ApiResponseDTO>> AddPlayerToRoster(int id, [FromBody] AddPlayerToRosterDTO dto)
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

                var result = await _teamService.AddPlayerToRosterAsync(id, dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} added player {PlayerID} to team {TeamID}",
                        actorUserId, dto.PlayerID, id);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding player to roster for team {TeamID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al agregar jugador."));
            }
        }

        /// <summary>
        /// Remueve un jugador del roster del equipo
        /// POST /api/team/roster/{rosterId}/remove
        /// Feature 3.1 - Gestión de roster (futuro)
        /// </summary>
        [HttpPost("roster/{rosterId}/remove")]
        public async Task<ActionResult<ApiResponseDTO>> RemovePlayerFromRoster(int rosterId)
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

                var result = await _teamService.RemovePlayerFromRosterAsync(rosterId, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} removed roster entry {RosterID}",
                        actorUserId, rosterId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing player from roster {RosterID}", rosterId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al remover jugador."));
            }
        }
    }
}
