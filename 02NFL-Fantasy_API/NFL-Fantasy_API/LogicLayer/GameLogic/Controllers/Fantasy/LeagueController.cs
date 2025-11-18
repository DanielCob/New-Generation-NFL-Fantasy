using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.Fantasy
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
    /// ⭐ ACTUALIZADO: Todos los endpoints usan LeaguePublicID en lugar de LeagueID
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
        /// <returns>Datos de la liga creada con su LeaguePublicID</returns>
        /// <response code="201">Liga creada exitosamente</response>
        /// <response code="400">Datos inválidos o nombre duplicado</response>
        /// <remarks>
        /// El usuario autenticado se convierte automáticamente en comisionado principal.
        /// ⭐ RETORNA: LeaguePublicID (no el LeagueID privado)
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
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo crear la liga."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created league: {LeagueName} (PublicID: {LeaguePublicID}) from {IP}",
                    creatorUserId,
                    dto.Name,
                    ((CreateLeagueResponseDTO?)result.Data)?.LeaguePublicID ?? 0,
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetLeagueSummary),
                    new { leaguePublicId = ((CreateLeagueResponseDTO?)result.Data)?.LeaguePublicID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Edita la configuración de una liga.
        /// PUT /api/league/{leaguePublicId}/config
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
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
        [HttpPut("{leaguePublicId}/config")]
        public async Task<ActionResult<ApiResponseDTO>> EditLeagueConfig(
            int leaguePublicId,
            [FromBody] EditLeagueConfigDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.EditLeagueConfigAsync(
                leaguePublicId,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo actualizar la configuración de la liga."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} edited config for league PublicID {LeaguePublicID} from {IP}",
                    actorUserId,
                    leaguePublicId,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cambia el estado de una liga.
        /// PUT /api/league/{leaguePublicId}/status
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
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
        [HttpPut("{leaguePublicId}/status")]
        public async Task<ActionResult<ApiResponseDTO>> SetLeagueStatus(
            int leaguePublicId,
            [FromBody] SetLeagueStatusDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.SetLeagueStatusAsync(
                leaguePublicId,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo cambiar el estado de la liga."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} changed status of league PublicID {LeaguePublicID} to {NewStatus} from {IP}",
                    actorUserId,
                    leaguePublicId,
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
        /// GET /api/league/{leaguePublicId}/summary
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <returns>Resumen con información completa y equipos</returns>
        /// <response code="200">Resumen obtenido exitosamente</response>
        /// <response code="404">Liga no encontrada</response>
        [HttpGet("{leaguePublicId}/summary")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueSummary(int leaguePublicId)
        {
            var summary = await _leagueService.GetLeagueSummaryAsync(leaguePublicId);

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
            if (directory is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo obtener el directorio de ligas."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Directorio de ligas obtenido exitosamente.",
                directory
            ));
        }

        /// <summary>
        /// Obtiene los miembros de una liga.
        /// GET /api/league/{leaguePublicId}/members
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <returns>Lista de usuarios con sus roles en la liga</returns>
        /// <response code="200">Miembros obtenidos exitosamente</response>
        [HttpGet("{leaguePublicId}/members")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueMembers(int leaguePublicId)
        {
            var members = await _leagueService.GetLeagueMembersAsync(leaguePublicId);
            if (members is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los miembros de la liga."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Miembros de liga obtenidos exitosamente.",
                members
            ));
        }

        /// <summary>
        /// Obtiene los equipos de una liga.
        /// GET /api/league/{leaguePublicId}/teams
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <returns>Lista de equipos con sus propietarios</returns>
        /// <response code="200">Equipos obtenidos exitosamente</response>
        [HttpGet("{leaguePublicId}/teams")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueTeams(int leaguePublicId)
        {
            var teams = await _leagueService.GetLeagueTeamsAsync(leaguePublicId);
            if (teams is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los equipos de la liga."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Equipos de liga obtenidos exitosamente.",
                teams
            ));
        }

        /// <summary>
        /// Obtiene todos los roles de un usuario en una liga específica.
        /// GET /api/league/{leaguePublicId}/users/{userId}/roles
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Roles explícitos, derivados y resumen</returns>
        /// <response code="200">Roles obtenidos exitosamente</response>
        /// <response code="404">Usuario no encontrado en esta liga o sin roles</response>
        [HttpGet("{leaguePublicId}/users/{userId}/roles")]
        public async Task<ActionResult<ApiResponseDTO>> GetUserRolesInLeague(
            int leaguePublicId,
            int userId)
        {
            var roles = await _leagueService.GetUserRolesInLeagueAsync(userId, leaguePublicId);

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
        /// ⭐ RETORNA: Solo LeaguePublicID (no el LeagueID privado)
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> SearchLeagues(
            [FromQuery] SearchLeaguesRequestDTO request)
        {
            var results = await _leagueService.SearchLeaguesAsync(request);
            if (results is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo realizar la búsqueda de ligas."));

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Se encontraron {results.FirstOrDefault()?.TotalRecords ?? 0} ligas.",
                results
            ));
        }

        /// <summary>
        /// Valida la contraseña de una liga.
        /// POST /api/league/validate-password
        /// </summary>
        /// <param name="request">LeaguePublicID y contraseña a validar</param>
        /// <returns>Resultado de validación (válida o inválida)</returns>
        /// <response code="200">Validación completada</response>
        /// <remarks>
        /// Acceso público - no requiere autenticación.
        /// Siempre retorna 200 OK con IsValid=true/false para no revelar si la liga existe.
        /// ⭐ USA: LeaguePublicID en lugar de LeagueID
        /// </remarks>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> ValidateLeaguePassword(
            [FromBody] ValidateLeaguePasswordRequestDTO request)
        {
            var result = await _leagueService.ValidateLeaguePasswordAsync(request);
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo validar la contraseña de la liga."));

            return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
        }

        /// <summary>
        /// Une al usuario autenticado a una liga.
        /// POST /api/league/join
        /// </summary>
        /// <param name="request">LeaguePublicID y contraseña (si requiere)</param>
        /// <returns>Confirmación de unión exitosa</returns>
        /// <response code="200">Usuario unido exitosamente</response>
        /// <response code="400">Liga llena, contraseña incorrecta o ya es miembro</response>
        /// <remarks>
        /// ⭐ USA: LeaguePublicID en lugar de LeagueID
        /// </remarks>
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
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo unirse a la liga."));

            return Ok(ApiResponseDTO.SuccessResponse(result.Message, result));
        }

        // ============================================================================
        // ENDPOINTS - Gestión de Miembros
        // ============================================================================

        /// <summary>
        /// Remueve un equipo de la liga.
        /// DELETE /api/league/{leaguePublicId}/teams
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <param name="request">ID del equipo a remover</param>
        /// <returns>Confirmación de remoción</returns>
        /// <response code="200">Equipo removido exitosamente</response>
        /// <response code="400">No se puede remover (draft en progreso, etc.)</response>
        /// <response code="403">No eres el comisionado</response>
        /// <remarks>
        /// Solo el comisionado puede remover equipos.
        /// </remarks>
        [HttpDelete("{leaguePublicId}/teams")]
        public async Task<ActionResult<ApiResponseDTO>> RemoveTeam(
            int leaguePublicId,
            [FromBody] RemoveTeamRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.RemoveTeamFromLeagueAsync(
                userId,
                leaguePublicId,
                request,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo remover el equipo de la liga."));

            return Ok(result);
        }

        /// <summary>
        /// Permite al usuario salir de una liga.
        /// POST /api/league/{leaguePublicId}/leave
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <returns>Confirmación de salida</returns>
        /// <response code="200">Usuario salió exitosamente</response>
        /// <response code="400">No se puede salir (eres comisionado, draft iniciado, etc.)</response>
        [HttpPost("{leaguePublicId}/leave")]
        public async Task<ActionResult<ApiResponseDTO>> LeaveLeague(int leaguePublicId)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.LeaveLeagueAsync(
                userId,
                leaguePublicId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo salir de la liga."));

            return Ok(result);
        }

        // ============================================================================
        // ENDPOINTS - Gestión de Comisionados
        // ============================================================================

        /// <summary>
        /// Transfiere el rol de comisionado principal a otro miembro.
        /// POST /api/league/{leaguePublicId}/transfer-commissioner
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <param name="request">ID del nuevo comisionado principal</param>
        /// <returns>Confirmación de transferencia</returns>
        /// <response code="200">Comisionado transferido exitosamente</response>
        /// <response code="400">Usuario no es miembro de la liga</response>
        /// <response code="403">No eres el comisionado principal actual</response>
        /// <remarks>
        /// El comisionado anterior pierde completamente su rol administrativo.
        /// </remarks>
        [HttpPost("{leaguePublicId}/transfer-commissioner")]
        public async Task<ActionResult<ApiResponseDTO>> TransferCommissioner(
            int leaguePublicId,
            [FromBody] TransferCommissionerRequestDTO request)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _leagueService.TransferCommissionerAsync(
                userId,
                leaguePublicId,
                request,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo transferir el comisionado principal."));

            return Ok(result);
        }

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (versión ligera sin equipos).
        /// GET /api/league/{leaguePublicId}/summary-view
        /// </summary>
        /// <param name="leaguePublicId">ID público de la liga</param>
        /// <returns>Resumen ligero de la liga</returns>
        /// <response code="200">Resumen obtenido exitosamente</response>
        /// <response code="404">Liga no encontrada</response>
        /// <remarks>
        /// DIFERENCIA CON /api/league/{leaguePublicId}/summary:
        /// - Este endpoint usa una VIEW directa (más rápido)
        /// - No incluye lista de equipos (solo cuenta)
        /// - Ideal para listados y dashboards
        /// 
        /// El endpoint /summary usa un SP y retorna equipos completos.
        /// </remarks>
        [HttpGet("{leaguePublicId}/summary-view")]
        public async Task<ActionResult<ApiResponseDTO>> GetLeagueSummaryView(int leaguePublicId)
        {
            var summary = await _leagueService.GetLeagueSummaryViewAsync(leaguePublicId);

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