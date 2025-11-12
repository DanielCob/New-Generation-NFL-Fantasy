using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de gestión de equipos NFL (CRUD).
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para equipos NFL
    /// - Crear, listar, actualizar equipos NFL
    /// - Activar/desactivar equipos
    /// - Consultar detalles completos
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Crear/Actualizar/Activar/Desactivar requieren rol ADMIN
    /// 
    /// ENDPOINTS:
    /// - POST /api/nflteam - Crear equipo NFL (ADMIN)
    /// - GET /api/nflteam - Listar equipos con filtros
    /// - GET /api/nflteam/{id} - Detalles completos
    /// - PUT /api/nflteam/{id} - Actualizar equipo (ADMIN)
    /// - POST /api/nflteam/{id}/deactivate - Desactivar (ADMIN)
    /// - POST /api/nflteam/{id}/reactivate - Reactivar (ADMIN)
    /// - GET /api/nflteam/active - Equipos activos para dropdowns
    /// 
    /// Feature 10.1: Gestión de Equipos NFL
    /// </summary>
    [ApiController]
    [Route("api/nflteam")]
    [Authorize]
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
        /// Crea un nuevo equipo NFL manualmente.
        /// POST /api/nflteam
        /// </summary>
        /// <param name="dto">Datos del equipo a crear</param>
        /// <returns>Datos del equipo creado con su ID</returns>
        /// <response code="201">Equipo creado exitosamente</response>
        /// <response code="400">Datos inválidos o equipo duplicado</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear equipos NFL.
        /// Feature 10.1: Crear equipo NFL
        /// </remarks>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateNFLTeam([FromBody] CreateNFLTeamDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflTeamService.CreateNFLTeamAsync(
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo crear el equipo NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created NFL team: {TeamName} from {IP}",
                    actorUserId,
                    dto.TeamName,
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetNFLTeamDetails),
                    new { id = ((CreateNFLTeamResponseDTO?)result.Data)?.NFLTeamID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lista equipos NFL con paginación y filtros.
        /// GET /api/nflteam
        /// </summary>
        /// <param name="request">Filtros de búsqueda y paginación</param>
        /// <returns>Lista paginada de equipos NFL</returns>
        /// <response code="200">Equipos obtenidos exitosamente</response>
        /// <remarks>
        /// FILTROS:
        /// - Búsqueda por nombre/ciudad
        /// - Estado activo/inactivo
        /// - Paginación: 50 por página (máx 100)
        /// 
        /// Feature 10.1: Listar equipos NFL
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDTO>> ListNFLTeams([FromQuery] ListNFLTeamsRequestDTO request)
        {
            var result = await _nflTeamService.ListNFLTeamsAsync(request);
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los equipos NFL."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Equipos NFL obtenidos exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene detalles completos de un equipo NFL.
        /// GET /api/nflteam/{id}
        /// </summary>
        /// <param name="id">ID del equipo NFL</param>
        /// <returns>Información completa del equipo</returns>
        /// <response code="200">Detalles obtenidos exitosamente</response>
        /// <response code="404">Equipo no encontrado</response>
        /// <remarks>
        /// RETORNA:
        /// - Información del equipo
        /// - Historial de cambios (últimos 20)
        /// - Jugadores activos del equipo
        /// 
        /// Feature 10.1: Ver detalles de equipo NFL
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDTO>> GetNFLTeamDetails(int id)
        {
            var details = await _nflTeamService.GetNFLTeamDetailsAsync(id);

            if (details == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Equipo NFL no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Detalles de equipo NFL obtenidos exitosamente.",
                details
            ));
        }

        /// <summary>
        /// Actualiza un equipo NFL existente.
        /// PUT /api/nflteam/{id}
        /// </summary>
        /// <param name="id">ID del equipo a actualizar</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Equipo actualizado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Equipo no encontrado</response>
        /// <remarks>
        /// Solo ADMIN puede actualizar equipos NFL.
        /// Feature 10.1: Modificar equipo NFL
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateNFLTeam(
            int id,
            [FromBody] UpdateNFLTeamDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflTeamService.UpdateNFLTeamAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo actualizar el equipo NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} updated NFL team {TeamID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Desactiva un equipo NFL.
        /// POST /api/nflteam/{id}/deactivate
        /// </summary>
        /// <param name="id">ID del equipo a desactivar</param>
        /// <returns>Confirmación de desactivación</returns>
        /// <response code="200">Equipo desactivado exitosamente</response>
        /// <response code="400">No se puede desactivar (tiene partidos programados)</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Valida que no tenga partidos programados en temporada actual.
        /// Solo ADMIN puede desactivar equipos NFL.
        /// Feature 10.1: Desactivar equipo NFL
        /// </remarks>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> DeactivateNFLTeam(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflTeamService.DeactivateNFLTeamAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo desactivar el equipo NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} deactivated NFL team {TeamID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Reactiva un equipo NFL desactivado.
        /// POST /api/nflteam/{id}/reactivate
        /// </summary>
        /// <param name="id">ID del equipo a reactivar</param>
        /// <returns>Confirmación de reactivación</returns>
        /// <response code="200">Equipo reactivado exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede reactivar equipos NFL.
        /// Feature 10.1: Reactivar equipo NFL
        /// </remarks>
        [HttpPost("{id}/reactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> ReactivateNFLTeam(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflTeamService.ReactivateNFLTeamAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo reactivar el equipo NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} reactivated NFL team {TeamID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene equipos NFL activos (para dropdowns).
        /// GET /api/nflteam/active
        /// </summary>
        /// <returns>Lista de equipos activos</returns>
        /// <response code="200">Equipos activos obtenidos exitosamente</response>
        /// <remarks>
        /// Usado para selectores/dropdowns en la UI.
        /// Feature 10.1: Selector de equipos NFL
        /// </remarks>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponseDTO>> GetActiveNFLTeams()
        {
            var teams = await _nflTeamService.GetActiveNFLTeamsAsync();
            if (teams is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los equipos NFL activos."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Equipos NFL activos obtenidos exitosamente.",
                teams
            ));
        }
    }
}