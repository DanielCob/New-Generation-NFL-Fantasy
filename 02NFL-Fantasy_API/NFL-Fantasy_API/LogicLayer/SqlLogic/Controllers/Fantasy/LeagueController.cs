using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Fantasy
{
    /// <summary>
    /// Controller de gestión de ligas de fantasy.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para ligas
    /// - Crear y configurar ligas
    /// - Gestionar estado y miembros
    /// - Buscar y unirse a ligas
    /// - Administrar roles (comisionados)
    /// 
    /// SEGURIDAD:
    /// - La mayoría de endpoints requieren autenticación
    /// - Búsqueda y validación de contraseña son públicas
    /// - Operaciones administrativas requieren rol de comisionado
    /// 
    /// Feature 1.2: Creación y administración de ligas
    /// </summary>
    [ApiController]
    [Route("api/league")]
    [Authorize] // Por defecto todos requieren autenticación
    public class LeagueController : ControllerBase
    {
        private readonly ILeagueService _leagueService;
        private readonly ILogger<LeagueController> _logger;

        public LeagueController(ILeagueService leagueService, ILogger<LeagueController> logger)
        {
            _leagueService = leagueService;
            _logger = logger;
        }

        // ============================================================================
        // ENDPOINTS - Creación y Configuración
        // ============================================================================

        /// <summary>
        /// Crea una nueva liga de fantasy.
        /// POST /api/league
        /// </summary>
        /// <param name="dto">Configuración de la liga</param>
        /// <returns>Datos de la liga creada con su LeagueID</returns>
        /// <response code="201">Liga creada exitosamente</response>
        /// <response code="400">Datos inválidos o nombre duplicado</response>
        /// <remarks>
        /// El usuario autenticado se convierte automáticamente en comisionado principal.
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<ApiResponseDTO>> CreateLeague([FromBody] CreateLeagueDTO dto)
        {
            var creatorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.CreateLeagueAsync(
                dto,
                creatorUserId,
                sourceIp,
                userAgent
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created league: {LeagueName} from {IP}",
                    creatorUserId,
                    dto.Name,
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetLeagueSummary),
                    new { id = ((CreateLeagueResponseDTO?)result.Data)?.LeagueID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Edita la configuración de una liga.
        /// PUT /api/league/{id}/config
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <param name="dto">Nueva configuración</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Configuración actualizada exitosamente</response>
        /// <response code="400">Datos inválidos o liga en estado no editable</response>
        /// <response code="403">No eres el comisionado principal</response>
        /// <remarks>
        /// RESTRICCIONES:
        /// - Solo el comisionado principal puede editar
        /// - Algunas configuraciones solo editables en estado Pre-Draft
        /// </remarks>
        [HttpPut("{id}/config")]
        public async Task<ActionResult<ApiResponseDTO>> EditLeagueConfig(
            int id,
            [FromBody] EditLeagueConfigDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.EditLeagueConfigAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} edited config for league {LeagueID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cambia el estado de una liga.
        /// PUT /api/league/{id}/status
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <param name="dto">Nuevo estado</param>
        /// <returns>Confirmación de cambio de estado</returns>
        /// <response code="200">Estado cambiado exitosamente</response>
        /// <response code="400">Transición de estado inválida</response>
        /// <response code="403">No eres el comisionado principal</response>
        /// <remarks>
        /// ESTADOS DISPONIBLES:
        /// - 0 = PreDraft (pre-borrador)
        /// - 1 = Active (activa)
        /// - 2 = Inactive (inactiva)
        /// - 3 = Closed (cerrada)
        /// 
        /// Solo el comisionado principal puede cambiar el estado.
        /// </remarks>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponseDTO>> SetLeagueStatus(
            int id,
            [FromBody] SetLeagueStatusDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.SetLeagueStatusAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} changed status of league {LeagueID} to {NewStatus} from {IP}",
                    actorUserId,
                    id,
                    dto.NewStatus,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ============================================================================
        // ENDPOINTS - Consulta de Información
        // ============================================================================

        /// <summary>
        /// Obtiene el resumen completo de una liga.
        /// GET /api/league/{id}/summary
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <returns>Resumen con información completa y equipos</returns>
        /// <response code="200">Resumen obtenido exitosamente</response>
        /// <response code="404">Liga no encontrada</response>
        [HttpGet("{id}/summary")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueSummary(int id)
        {
            var summary = await _leagueService.GetLeagueSummaryAsync(id);

            if (summary == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse("Liga no encontrada."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Resumen de liga obtenido exitosamente.",
                summary
            ));
        }

        /// <summary>
        /// Obtiene el directorio/listado de ligas disponibles.
        /// GET /api/league/directory
        /// </summary>
        /// <returns>Lista de ligas para navegación</returns>
        /// <response code="200">Directorio obtenido exitosamente</response>
        [HttpGet("directory")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueDirectory()
        {
            var directory = await _leagueService.GetLeagueDirectoryAsync();

            return Ok(ApiResponseDTO.SuccessResponse(
                "Directorio de ligas obtenido exitosamente.",
                directory
            ));
        }

        /// <summary>
        /// Obtiene los miembros de una liga.
        /// GET /api/league/{id}/members
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <returns>Lista de usuarios con sus roles en la liga</returns>
        /// <response code="200">Miembros obtenidos exitosamente</response>
        [HttpGet("{id}/members")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueMembers(int id)
        {
            var members = await _leagueService.GetLeagueMembersAsync(id);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Miembros de liga obtenidos exitosamente.",
                members
            ));
        }

        /// <summary>
        /// Obtiene los equipos de una liga.
        /// GET /api/league/{id}/teams
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <returns>Lista de equipos con sus propietarios</returns>
        /// <response code="200">Equipos obtenidos exitosamente</response>
        [HttpGet("{id}/teams")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueTeams(int id)
        {
            var teams = await _leagueService.GetLeagueTeamsAsync(id);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Equipos de liga obtenidos exitosamente.",
                teams
            ));
        }

        /// <summary>
        /// Obtiene todos los roles de un usuario en una liga específica.
        /// GET /api/league/{leagueId}/users/{userId}/roles
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Roles explícitos, derivados y resumen</returns>
        /// <response code="200">Roles obtenidos exitosamente</response>
        /// <response code="404">Usuario no encontrado en esta liga o sin roles</response>
        [HttpGet("{leagueId}/users/{userId}/roles")]
        public async Task<ActionResult<ApiResponseDTO>> GetUserRolesInLeague(
            int leagueId,
            int userId)
        {
            var roles = await _leagueService.GetUserRolesInLeagueAsync(userId, leagueId);

            if (roles == null || roles.Roles.Count == 0)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Usuario no encontrado en esta liga o sin roles asignados."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Roles de usuario en liga obtenidos exitosamente.",
                roles
            ));
        }

        /// <summary>
        /// Obtiene información sobre la contraseña de la liga.
        /// GET /api/league/{leagueId}/password-info
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Información sobre si tiene contraseña y hint</returns>
        /// <response code="200">Información obtenida exitosamente</response>
        /// <response code="403">No eres el comisionado principal</response>
        /// <remarks>
        /// Solo el comisionado principal puede ver esta información.
        /// </remarks>
        [HttpGet("{leagueId}/password-info")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeaguePasswordInfo(int leagueId)
        {
            var userId = this.UserId();
            var result = await _leagueService.GetLeaguePasswordInfoAsync(userId, leagueId);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Información obtenida exitosamente.",
                result
            ));
        }

        // ============================================================================
        // ENDPOINTS - Búsqueda y Unión a Ligas
        // ============================================================================

        /// <summary>
        /// Busca ligas disponibles para unirse.
        /// GET /api/league/search
        /// </summary>
        /// <param name="request">Filtros de búsqueda</param>
        /// <returns>Lista de ligas que coinciden con los criterios</returns>
        /// <response code="200">Búsqueda completada exitosamente</response>
        /// <remarks>
        /// Acceso público - no requiere autenticación.
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> SearchLeagues(
            [FromQuery] SearchLeaguesRequestDTO request)
        {
            var results = await _leagueService.SearchLeaguesAsync(request);

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Se encontraron {results.FirstOrDefault()?.TotalRecords ?? 0} ligas.",
                results
            ));
        }

        /// <summary>
        /// Valida la contraseña de una liga.
        /// POST /api/league/validate-password
        /// </summary>
        /// <param name="request">LeagueID y contraseña a validar</param>
        /// <returns>Resultado de validación (válida o inválida)</returns>
        /// <response code="200">Validación completada</response>
        /// <remarks>
        /// Acceso público - no requiere autenticación.
        /// Siempre retorna 200 OK con IsValid=true/false para no revelar si la liga existe.
        /// </remarks>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> ValidateLeaguePassword(
            [FromBody] ValidateLeaguePasswordRequestDTO request)
        {
            var result = await _leagueService.ValidateLeaguePasswordAsync(request);

            return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
        }

        /// <summary>
        /// Une al usuario autenticado a una liga.
        /// POST /api/league/join
        /// </summary>
        /// <param name="request">LeagueID y contraseña (si requiere)</param>
        /// <returns>Confirmación de unión exitosa</returns>
        /// <response code="200">Usuario unido exitosamente</response>
        /// <response code="400">Liga llena, contraseña incorrecta o ya es miembro</response>
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponseDTO>> JoinLeague(
            [FromBody] JoinLeagueRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.JoinLeagueAsync(
                userId,
                request,
                sourceIp,
                userAgent
            );

            return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
        }

        // ============================================================================
        // ENDPOINTS - Gestión de Miembros
        // ============================================================================

        /// <summary>
        /// Remueve un equipo de la liga.
        /// DELETE /api/league/{leagueId}/teams
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="request">ID del equipo a remover</param>
        /// <returns>Confirmación de remoción</returns>
        /// <response code="200">Equipo removido exitosamente</response>
        /// <response code="400">No se puede remover (draft en progreso, etc.)</response>
        /// <response code="403">No eres el comisionado</response>
        /// <remarks>
        /// Solo el comisionado puede remover equipos.
        /// </remarks>
        [HttpDelete("{leagueId}/teams")]
        public async Task<ActionResult<ApiResponseDTO>> RemoveTeam(
            int leagueId,
            [FromBody] RemoveTeamRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.RemoveTeamFromLeagueAsync(
                userId,
                leagueId,
                request,
                sourceIp,
                userAgent
            );

            return Ok(result);
        }

        /// <summary>
        /// Permite al usuario salir de una liga.
        /// POST /api/league/{leagueId}/leave
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Confirmación de salida</returns>
        /// <response code="200">Usuario salió exitosamente</response>
        /// <response code="400">No se puede salir (eres comisionado, draft iniciado, etc.)</response>
        [HttpPost("{leagueId}/leave")]
        public async Task<ActionResult<ApiResponseDTO>> LeaveLeague(int leagueId)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.LeaveLeagueAsync(
                userId,
                leagueId,
                sourceIp,
                userAgent
            );

            return Ok(result);
        }

        // ============================================================================
        // ENDPOINTS - Gestión de Comisionados
        // ============================================================================

        /// <summary>
        /// Asigna un co-comisionado a la liga.
        /// POST /api/league/{leagueId}/co-commissioner
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="request">ID del usuario a asignar como co-comisionado</param>
        /// <returns>Confirmación de asignación</returns>
        /// <response code="200">Co-comisionado asignado exitosamente</response>
        /// <response code="400">Usuario no es miembro o ya es co-comisionado</response>
        /// <response code="403">No eres el comisionado principal</response>
        [HttpPost("{leagueId}/co-commissioner")]
        public async Task<ActionResult<ApiResponseDTO>> AssignCoCommissioner(
            int leagueId,
            [FromBody] AssignCoCommissionerRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.AssignCoCommissionerAsync(
                userId,
                leagueId,
                request,
                sourceIp,
                userAgent
            );

            return Ok(result);
        }

        /// <summary>
        /// Remueve un co-comisionado de la liga.
        /// DELETE /api/league/{leagueId}/co-commissioner
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="request">ID del usuario a remover como co-comisionado</param>
        /// <returns>Confirmación de remoción</returns>
        /// <response code="200">Co-comisionado removido exitosamente</response>
        /// <response code="400">Usuario no es co-comisionado</response>
        /// <response code="403">No eres el comisionado principal</response>
        [HttpDelete("{leagueId}/co-commissioner")]
        public async Task<ActionResult<ApiResponseDTO>> RemoveCoCommissioner(
            int leagueId,
            [FromBody] RemoveCoCommissionerRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.RemoveCoCommissionerAsync(
                userId,
                leagueId,
                request,
                sourceIp,
                userAgent
            );

            return Ok(result);
        }

        /// <summary>
        /// Transfiere el rol de comisionado principal a otro miembro.
        /// POST /api/league/{leagueId}/transfer-commissioner
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="request">ID del nuevo comisionado principal</param>
        /// <returns>Confirmación de transferencia</returns>
        /// <response code="200">Comisionado transferido exitosamente</response>
        /// <response code="400">Usuario no es miembro de la liga</response>
        /// <response code="403">No eres el comisionado principal actual</response>
        /// <remarks>
        /// El comisionado anterior se convierte automáticamente en co-comisionado.
        /// </remarks>
        [HttpPost("{leagueId}/transfer-commissioner")]
        public async Task<ActionResult<ApiResponseDTO>> TransferCommissioner(
            int leagueId,
            [FromBody] TransferCommissionerRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.TransferCommissionerAsync(
                userId,
                leagueId,
                request,
                sourceIp,
                userAgent
            );

            return Ok(result);
        }

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (versión ligera sin equipos).
        /// GET /api/league/{id}/summary-view
        /// </summary>
        /// <param name="id">ID de la liga</param>
        /// <returns>Resumen ligero de la liga</returns>
        /// <response code="200">Resumen obtenido exitosamente</response>
        /// <response code="404">Liga no encontrada</response>
        /// <remarks>
        /// DIFERENCIA CON /api/league/{id}/summary:
        /// - Este endpoint usa una VIEW directa (más rápido)
        /// - No incluye lista de equipos (solo cuenta)
        /// - Ideal para listados y dashboards
        /// 
        /// El endpoint /summary usa un SP y retorna equipos completos.
        /// </remarks>
        [HttpGet("{id}/summary-view")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueSummaryView(int id)
        {
            var summary = await _leagueService.GetLeagueSummaryViewAsync(id);

            if (summary == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Liga no encontrada."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Resumen de liga obtenido exitosamente.",
                summary
            ));
        }
    }
}