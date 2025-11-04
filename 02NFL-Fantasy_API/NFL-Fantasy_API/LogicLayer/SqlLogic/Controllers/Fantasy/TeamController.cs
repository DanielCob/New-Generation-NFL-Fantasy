using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Fantasy
{
    /// <summary>
    /// Controller de gestión de equipos fantasy.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para equipos
    /// - Actualizar branding (nombre e imagen)
    /// - Consultar información del equipo
    /// - Gestionar roster (agregar/remover jugadores)
    /// - Consultar distribución de adquisiciones
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Solo el dueño del equipo puede modificarlo
    /// 
    /// ENDPOINTS:
    /// - PUT /api/team/{id}/branding - Actualizar nombre e imagen
    /// - GET /api/team/{id}/my-team - Información completa del equipo
    /// - GET /api/team/{id}/roster/distribution - Distribución de adquisiciones
    /// - POST /api/team/{id}/roster/add - Agregar jugador al roster
    /// - POST /api/team/roster/{rosterId}/remove - Remover jugador del roster
    /// 
    /// Feature 3.1: Creación y administración de equipos fantasy
    /// </summary>
    [ApiController]
    [Route("api/team")]
    [Authorize] // Todos los endpoints requieren autenticación
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
        /// Actualiza el branding de un equipo fantasy (nombre e imagen).
        /// PUT /api/team/{id}/branding
        /// </summary>
        /// <param name="id">ID del equipo</param>
        /// <param name="dto">Nuevos nombre e imagen del equipo</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Branding actualizado exitosamente</response>
        /// <response code="400">Nombre duplicado en la liga o datos inválidos</response>
        /// <response code="403">No eres el dueño del equipo</response>
        /// <remarks>
        /// VALIDACIONES:
        /// - Solo el dueño del equipo puede editarlo
        /// - El nombre debe ser único dentro de la liga
        /// - TeamName: 3-50 caracteres
        /// - TeamImageURL: URL válida (opcional)
        /// </remarks>
        [HttpPut("{id}/branding")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateTeamBranding(
            int id,
            [FromBody] UpdateTeamBrandingDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _teamService.UpdateTeamBrandingAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo actualizar el branding del equipo."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} updated branding for team {TeamID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene información completa del equipo con roster.
        /// GET /api/team/{id}/my-team
        /// </summary>
        /// <param name="id">ID del equipo</param>
        /// <param name="filterPosition">Filtrar jugadores por posición (opcional)</param>
        /// <param name="searchPlayer">Buscar jugador por nombre (opcional)</param>
        /// <returns>Información del equipo, roster y distribución</returns>
        /// <response code="200">Equipo obtenido exitosamente</response>
        /// <response code="404">Equipo no encontrado o sin permisos</response>
        /// <remarks>
        /// RETORNA 3 CONJUNTOS DE DATOS:
        /// 1. Información del equipo (nombre, liga, record, etc.)
        /// 2. Jugadores en roster (con filtros aplicados)
        /// 3. Distribución porcentual por tipo de adquisición
        /// 
        /// FILTROS OPCIONALES:
        /// - filterPosition: QB, RB, WR, TE, K, DEF, etc.
        /// - searchPlayer: Búsqueda por nombre del jugador
        /// </remarks>
        [HttpGet("{id}/my-team")]
        public async Task<ActionResult<ApiResponseDTO>> GetMyTeam(
            int id,
            [FromQuery] string? filterPosition = null,
            [FromQuery] string? searchPlayer = null)
        {
            var actorUserId = this.UserId();

            var myTeam = await _teamService.GetMyTeamAsync(
                id,
                actorUserId,
                filterPosition,
                searchPlayer
            );

            if (myTeam == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Equipo no encontrado o sin permisos."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Información del equipo obtenida exitosamente.",
                myTeam
            ));
        }

        /// <summary>
        /// Obtiene distribución porcentual del roster por tipo de adquisición.
        /// GET /api/team/{id}/roster/distribution
        /// </summary>
        /// <param name="id">ID del equipo</param>
        /// <returns>Distribución porcentual de jugadores</returns>
        /// <response code="200">Distribución obtenida exitosamente</response>
        /// <remarks>
        /// TIPOS DE ADQUISICIÓN:
        /// - Draft: Jugadores seleccionados en el draft
        /// - Trade: Jugadores adquiridos por intercambio
        /// - Free Agent: Jugadores tomados de agentes libres
        /// - Waiver: Jugadores adquiridos del waiver wire
        /// 
        /// Retorna porcentaje para cada tipo.
        /// </remarks>
        [HttpGet("{id}/roster/distribution")]
        public async Task<ActionResult<ApiResponseDTO>> GetRosterDistribution(int id)
        {
            var distribution = await _teamService.GetTeamRosterDistributionAsync(id);
            if (distribution is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo obtener la distribución del roster."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Distribución de roster obtenida exitosamente.",
                distribution
            ));
        }

        /// <summary>
        /// Agrega un jugador al roster del equipo.
        /// POST /api/team/{id}/roster/add
        /// </summary>
        /// <param name="id">ID del equipo</param>
        /// <param name="dto">ID del jugador y tipo de adquisición</param>
        /// <returns>Confirmación de agregación</returns>
        /// <response code="200">Jugador agregado exitosamente</response>
        /// <response code="400">Jugador ya existe, liga llena o inactivo</response>
        /// <response code="403">No eres el dueño del equipo</response>
        /// <remarks>
        /// VALIDACIONES:
        /// - Jugador debe existir y estar activo
        /// - No puede estar en dos equipos activos de la misma liga
        /// - No permite duplicados en el mismo equipo
        /// - El equipo no debe exceder el límite de jugadores
        /// 
        /// Feature 3.1: Gestión de roster
        /// </remarks>
        [HttpPost("{id}/roster/add")]
        public async Task<ActionResult<ApiResponseDTO>> AddPlayerToRoster(
            int id,
            [FromBody] AddPlayerToRosterDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _teamService.AddPlayerToRosterAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo agregar el jugador al roster."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} added player {PlayerID} to team {TeamID} from {IP}",
                    actorUserId,
                    dto.PlayerID,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Remueve un jugador del roster del equipo.
        /// POST /api/team/roster/{rosterId}/remove
        /// </summary>
        /// <param name="rosterId">ID del registro en el roster</param>
        /// <returns>Confirmación de remoción</returns>
        /// <response code="200">Jugador removido exitosamente</response>
        /// <response code="400">Registro de roster no encontrado</response>
        /// <response code="403">No eres el dueño del equipo</response>
        /// <remarks>
        /// SOFT DELETE:
        /// - Marca IsActive=0 en lugar de eliminar físicamente
        /// - Registra DroppedDate para auditoría
        /// - El jugador queda disponible para otros equipos
        /// 
        /// Feature 3.1: Gestión de roster
        /// </remarks>
        [HttpPost("roster/{rosterId}/remove")]
        public async Task<ActionResult<ApiResponseDTO>> RemovePlayerFromRoster(int rosterId)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _teamService.RemovePlayerFromRosterAsync(
                rosterId,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo remover el jugador del roster."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} removed roster entry {RosterID} from {IP}",
                    actorUserId,
                    rosterId,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}