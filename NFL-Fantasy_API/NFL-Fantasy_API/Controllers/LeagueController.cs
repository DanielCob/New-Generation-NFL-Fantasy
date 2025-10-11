using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de ligas de fantasy
    /// Endpoints: Create, EditConfig, SetStatus, GetSummary, GetMembers, GetTeams, GetDirectory
    /// Feature 1.2: Creación y administración de ligas
    /// La mayoría requiere autenticación; algunos requieren ser comisionado
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LeagueController : ControllerBase
    {
        private readonly ILeagueService _leagueService;
        private readonly ILogger<LeagueController> _logger;

        public LeagueController(ILeagueService leagueService, ILogger<LeagueController> logger)
        {
            _leagueService = leagueService;
            _logger = logger;
        }

        /// <summary>
        /// Crea una nueva liga de fantasy
        /// POST /api/league
        /// Feature 1.2 - Crear liga
        /// El usuario autenticado se convierte en comisionado principal
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponseDTO>> CreateLeague([FromBody] CreateLeagueDTO dto)
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
                var creatorUserId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.CreateLeagueAsync(dto, creatorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} created league: {LeagueName} from {IP}",
                        creatorUserId, dto.Name, sourceIp);
                    return CreatedAtAction(nameof(GetLeagueSummary),
                        new { id = ((CreateLeagueResponseDTO?)result.Data)?.LeagueID ?? 0 },
                        result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating league for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al crear liga."));
            }
        }

        /// <summary>
        /// Edita la configuración de una liga
        /// PUT /api/league/{id}/config
        /// Feature 1.2 - Editar configuración de liga
        /// Solo el comisionado principal puede editar
        /// Algunas configuraciones solo editables en Pre-Draft
        /// </summary>
        [HttpPut("{id}/config")]
        public async Task<ActionResult<ApiResponseDTO>> EditLeagueConfig(int id, [FromBody] EditLeagueConfigDTO dto)
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

                var result = await _leagueService.EditLeagueConfigAsync(id, dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} edited config for league {LeagueID} from {IP}",
                        actorUserId, id, sourceIp);
                    return Ok(result);
                }

                // El SP valida permisos y estado; errores específicos vienen en result.Message
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing league {LeagueID} config", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al editar configuración."));
            }
        }

        /// <summary>
        /// Cambia el estado de una liga
        /// PUT /api/league/{id}/status
        /// Feature 1.2 - Administrar estado de liga
        /// Solo el comisionado principal puede cambiar el estado
        /// Estados: 0=PreDraft, 1=Active, 2=Inactive, 3=Closed
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponseDTO>> SetLeagueStatus(int id, [FromBody] SetLeagueStatusDTO dto)
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

                var result = await _leagueService.SetLeagueStatusAsync(id, dto, actorUserId, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} changed status of league {LeagueID} to {NewStatus} from {IP}",
                        actorUserId, id, dto.NewStatus, sourceIp);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing status for league {LeagueID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al cambiar estado."));
            }
        }

        /// <summary>
        /// Obtiene el resumen completo de una liga
        /// GET /api/league/{id}/summary
        /// Feature 1.2 - Ver liga
        /// Retorna información completa + equipos
        /// </summary>
        [HttpGet("{id}/summary")]
        public async Task<ActionResult> GetLeagueSummary(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var summary = await _leagueService.GetLeagueSummaryAsync(id);

                if (summary == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Liga no encontrada."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Resumen de liga obtenido.", summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting summary for league {LeagueID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener resumen."));
            }
        }

        /// <summary>
        /// Obtiene el directorio/listado de ligas disponibles
        /// GET /api/league/directory
        /// Para búsqueda y navegación de ligas
        /// </summary>
        [HttpGet("directory")]
        public async Task<ActionResult> GetLeagueDirectory()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var directory = await _leagueService.GetLeagueDirectoryAsync();
                return Ok(ApiResponseDTO.SuccessResponse("Directorio de ligas obtenido.", directory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league directory");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener directorio."));
            }
        }

        /// <summary>
        /// Obtiene los miembros de una liga
        /// GET /api/league/{id}/members
        /// Lista usuarios con sus roles en la liga
        /// </summary>
        [HttpGet("{id}/members")]
        public async Task<ActionResult> GetLeagueMembers(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var members = await _leagueService.GetLeagueMembersAsync(id);
                return Ok(ApiResponseDTO.SuccessResponse("Miembros de liga obtenidos.", members));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members for league {LeagueID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener miembros."));
            }
        }

        /// <summary>
        /// Obtiene los equipos de una liga
        /// GET /api/league/{id}/teams
        /// Lista equipos con sus owners
        /// </summary>
        [HttpGet("{id}/teams")]
        public async Task<ActionResult> GetLeagueTeams(int id)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var teams = await _leagueService.GetLeagueTeamsAsync(id);
                return Ok(ApiResponseDTO.SuccessResponse("Equipos de liga obtenidos.", teams));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams for league {LeagueID}", id);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener equipos."));
            }
        }
    }
}