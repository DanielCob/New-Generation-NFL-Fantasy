using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        /// <summary>
        /// Obtiene todos los roles de un usuario en una liga específica
        /// GET /api/league/{leagueId}/users/{userId}/roles
        /// Retorna roles explícitos, derivados y resumen
        /// </summary>
        [HttpGet("{leagueId}/users/{userId}/roles")]
        public async Task<ActionResult> GetUserRolesInLeague(int leagueId, int userId)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var roles = await _leagueService.GetUserRolesInLeagueAsync(userId, leagueId);

                if (roles == null || roles.Roles.Count == 0)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Usuario no encontrado en esta liga o sin roles asignados."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Roles de usuario en liga obtenidos.", roles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserID} in league {LeagueID}", userId, leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener roles."));
            }
        }

        // ============================================================================
        // ENDPOINTS - Búsqueda y Unión a Ligas
        // ============================================================================

        /// <summary>
        /// Busca ligas disponibles para unirse
        /// GET /api/league/search
        /// Acceso público (no requiere autenticación)
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult> SearchLeagues([FromQuery] SearchLeaguesRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var results = await _leagueService.SearchLeaguesAsync(request);
                return Ok(ApiResponseDTO.SuccessResponse(
                    $"Se encontraron {results.FirstOrDefault()?.TotalRecords ?? 0} ligas.",
                    results
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching leagues");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al buscar ligas."));
            }
        }

        /// <summary>
        /// Valida la contraseña de una liga
        /// POST /api/league/validate-password
        /// Acceso público (no requiere autenticación)
        /// </summary>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ValidateLeaguePassword([FromBody] ValidateLeaguePasswordRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var result = await _leagueService.ValidateLeaguePasswordAsync(request);

                if (result.IsValid)
                {
                    return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
                }
                else
                {
                    return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating league password");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al validar contraseña."));
            }
        }

        /// <summary>
        /// Une al usuario autenticado a una liga
        /// POST /api/league/join
        /// Requiere autenticación
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult> JoinLeague([FromBody] JoinLeagueRequestDTO request)
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
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.JoinLeagueAsync(userId, request, sourceIp, userAgent);

                return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                // Errores de negocio lanzados por el SP
                _logger.LogWarning(ex, "Business error joining league for user {UserID}", HttpContext.GetUserId());
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining league for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al unirse a la liga."));
            }
        }

        // ============================================================================
        // ENDPOINTS - Gestión de Miembros
        // ============================================================================

        /// <summary>
        /// Remueve un equipo de la liga (solo comisionado)
        /// DELETE /api/league/{leagueId}/teams
        /// Requiere autenticación y rol de comisionado
        /// </summary>
        [HttpDelete("{leagueId}/teams")]
        public async Task<ActionResult> RemoveTeam(int leagueId, [FromBody] RemoveTeamRequestDTO request)
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
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.RemoveTeamFromLeagueAsync(
                    userId, leagueId, request, sourceIp, userAgent
                );

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogWarning(ex, "Business error removing team from league {LeagueID}", leagueId);
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing team from league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al remover equipo."));
            }
        }

        /// <summary>
        /// Permite al usuario salir de una liga
        /// POST /api/league/{leagueId}/leave
        /// Requiere autenticación
        /// </summary>
        [HttpPost("{leagueId}/leave")]
        public async Task<ActionResult> LeaveLeague(int leagueId)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.LeaveLeagueAsync(
                    userId, leagueId, sourceIp, userAgent
                );

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogWarning(ex, "Business error leaving league {LeagueID}", leagueId);
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al salir de la liga."));
            }
        }

        /// <summary>
        /// Asigna un co-comisionado
        /// POST /api/league/{leagueId}/co-commissioner
        /// Requiere autenticación y rol de comisionado principal
        /// </summary>
        [HttpPost("{leagueId}/co-commissioner")]
        public async Task<ActionResult> AssignCoCommissioner(
            int leagueId,
            [FromBody] AssignCoCommissionerRequestDTO request)
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
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.AssignCoCommissionerAsync(
                    userId, leagueId, request, sourceIp, userAgent
                );

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogWarning(ex, "Business error assigning co-commissioner in league {LeagueID}", leagueId);
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning co-commissioner in league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al asignar co-comisionado."));
            }
        }

        /// <summary>
        /// Remueve un co-comisionado
        /// DELETE /api/league/{leagueId}/co-commissioner
        /// Requiere autenticación y rol de comisionado principal
        /// </summary>
        [HttpDelete("{leagueId}/co-commissioner")]
        public async Task<ActionResult> RemoveCoCommissioner(
            int leagueId,
            [FromBody] RemoveCoCommissionerRequestDTO request)
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
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.RemoveCoCommissionerAsync(
                    userId, leagueId, request, sourceIp, userAgent
                );

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogWarning(ex, "Business error removing co-commissioner from league {LeagueID}", leagueId);
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing co-commissioner from league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al remover co-comisionado."));
            }
        }

        /// <summary>
        /// Transfiere el comisionado principal a otro miembro
        /// POST /api/league/{leagueId}/transfer-commissioner
        /// Requiere autenticación y rol de comisionado principal
        /// </summary>
        [HttpPost("{leagueId}/transfer-commissioner")]
        public async Task<ActionResult> TransferCommissioner(
            int leagueId,
            [FromBody] TransferCommissionerRequestDTO request)
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
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _leagueService.TransferCommissionerAsync(
                    userId, leagueId, request, sourceIp, userAgent
                );

                return Ok(result);
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                _logger.LogWarning(ex, "Business error transferring commissioner in league {LeagueID}", leagueId);
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring commissioner in league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al transferir comisionado."));
            }
        }

        /// <summary>
        /// Obtiene información sobre la contraseña de la liga
        /// GET /api/league/{leagueId}/password-info
        /// Requiere autenticación y rol de comisionado principal
        /// </summary>
        [HttpGet("{leagueId}/password-info")]
        public async Task<ActionResult> GetLeaguePasswordInfo(int leagueId)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var result = await _leagueService.GetLeaguePasswordInfoAsync(userId, leagueId);

                return Ok(ApiResponseDTO.SuccessResponse("Información obtenida.", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting league password info for league {LeagueID}", leagueId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener información."));
            }
        }
    }
}