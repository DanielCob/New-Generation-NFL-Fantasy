using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de gestión de jugadores NFL (CRUD).
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para jugadores NFL
    /// - Crear, listar, actualizar jugadores NFL
    /// - Activar/desactivar jugadores
    /// - Consultar detalles completos
    /// - Listar jugadores disponibles para draft/FA
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Crear/Actualizar/Activar/Desactivar requieren rol ADMIN
    /// 
    /// ENDPOINTS:
    /// - POST /api/nflplayer - Crear jugador NFL (ADMIN)
    /// - GET /api/nflplayer - Listar jugadores con filtros y paginación
    /// - GET /api/nflplayer/{id} - Detalles completos
    /// - PUT /api/nflplayer/{id} - Actualizar jugador (ADMIN)
    /// - POST /api/nflplayer/{id}/deactivate - Desactivar (ADMIN)
    /// - POST /api/nflplayer/{id}/reactivate - Reactivar (ADMIN)
    /// - GET /api/nflplayer/available - Jugadores disponibles para draft/FA
    /// - GET /api/nflplayer/by-nfl-team/{nflTeamId} - Jugadores de un equipo NFL
    /// - GET /api/nflplayer/active - Jugadores activos para dropdowns
    /// 
    /// Feature: Gestión de Jugadores NFL (CRUD)
    /// </summary>
    [ApiController]
    [Route("api/nflplayer")]
    [Authorize]
    public class NFLPlayerController : ControllerBase
    {
        private readonly INFLPlayerService _nflPlayerService;
        private readonly ILogger<NFLPlayerController> _logger;

        public NFLPlayerController(INFLPlayerService nflPlayerService, ILogger<NFLPlayerController> logger)
        {
            _nflPlayerService = nflPlayerService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo jugador NFL manualmente.
        /// POST /api/nflplayer
        /// </summary>
        /// <param name="dto">Datos del jugador a crear</param>
        /// <returns>Datos del jugador creado con su ID</returns>
        /// <response code="201">Jugador creado exitosamente</response>
        /// <response code="400">Datos inválidos o jugador duplicado</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear jugadores NFL.
        /// Validación de unicidad: (FirstName, LastName, NFLTeamID) debe ser único.
        /// Feature: Crear jugador NFL
        /// </remarks>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateNFLPlayer([FromBody] CreateNFLPlayerDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.CreateNFLPlayerAsync(
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo crear el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created NFL player: {PlayerName} from {IP}",
                    actorUserId,
                    $"{dto.FirstName} {dto.LastName}",
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetNFLPlayerDetails),
                    new { id = ((CreateNFLPlayerResponseDTO?)result.Data)?.NFLPlayerID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros.
        /// GET /api/nflplayer
        /// </summary>
        /// <param name="request">Filtros de búsqueda y paginación</param>
        /// <returns>Lista paginada de jugadores NFL</returns>
        /// <response code="200">Jugadores obtenidos exitosamente</response>
        /// <remarks>
        /// FILTROS:
        /// - Búsqueda por nombre (FirstName, LastName, FullName)
        /// - Posición (QB, RB, WR, TE, K, DEF, etc.)
        /// - Equipo NFL
        /// - Estado activo/inactivo
        /// - Paginación: 50 por página (máx 100)
        /// 
        /// Feature: Listar jugadores NFL
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDTO>> ListNFLPlayers([FromQuery] ListNFLPlayersRequestDTO request)
        {
            var result = await _nflPlayerService.ListNFLPlayersAsync(request);
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores NFL."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores NFL obtenidos exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL.
        /// GET /api/nflplayer/{id}
        /// </summary>
        /// <param name="id">ID del jugador NFL</param>
        /// <returns>Información completa del jugador</returns>
        /// <response code="200">Detalles obtenidos exitosamente</response>
        /// <response code="404">Jugador no encontrado</response>
        /// <remarks>
        /// RETORNA:
        /// - Información del jugador
        /// - Historial de cambios (últimos 20)
        /// - Equipos fantasy actuales que tienen este jugador
        /// 
        /// Feature: Ver detalles de jugador NFL
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDTO>> GetNFLPlayerDetails(int id)
        {
            var details = await _nflPlayerService.GetNFLPlayerDetailsAsync(id);

            if (details == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Jugador NFL no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Detalles de jugador NFL obtenidos exitosamente.",
                details
            ));
        }

        /// <summary>
        /// Actualiza un jugador NFL existente.
        /// PUT /api/nflplayer/{id}
        /// </summary>
        /// <param name="id">ID del jugador a actualizar</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Jugador actualizado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Jugador no encontrado</response>
        /// <remarks>
        /// Solo ADMIN puede actualizar jugadores NFL.
        /// Validación de unicidad: (FirstName, LastName, NFLTeamID) debe ser único.
        /// Feature: Modificar jugador NFL
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateNFLPlayer(
            int id,
            [FromBody] UpdateNFLPlayerDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.UpdateNFLPlayerAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo actualizar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} updated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Desactiva un jugador NFL.
        /// POST /api/nflplayer/{id}/deactivate
        /// </summary>
        /// <param name="id">ID del jugador a desactivar</param>
        /// <returns>Confirmación de desactivación</returns>
        /// <response code="200">Jugador desactivado exitosamente</response>
        /// <response code="400">No se puede desactivar (está en roster activo)</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Valida que no esté en roster activo de equipos en temporada actual.
        /// Solo ADMIN puede desactivar jugadores NFL.
        /// Feature: Desactivar jugador NFL
        /// </remarks>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> DeactivateNFLPlayer(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.DeactivateNFLPlayerAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo desactivar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} deactivated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Reactiva un jugador NFL desactivado.
        /// POST /api/nflplayer/{id}/reactivate
        /// </summary>
        /// <param name="id">ID del jugador a reactivar</param>
        /// <returns>Confirmación de reactivación</returns>
        /// <response code="200">Jugador reactivado exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede reactivar jugadores NFL.
        /// Feature: Reactivar jugador NFL
        /// </remarks>
        [HttpPost("{id}/reactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> ReactivateNFLPlayer(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.ReactivateNFLPlayerAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo reactivar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} reactivated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lista jugadores disponibles (no están en ningún roster activo).
        /// GET /api/nflplayer/available
        /// </summary>
        /// <param name="position">Filtrar por posición (opcional)</param>
        /// <returns>Lista de jugadores disponibles</returns>
        /// <response code="200">Jugadores disponibles obtenidos exitosamente</response>
        /// <remarks>
        /// Usado para:
        /// - Draft de jugadores
        /// - Free agency
        /// - Waiver wire
        /// 
        /// Solo retorna jugadores que NO están en ningún roster activo.
        /// VIEW: vw_AvailablePlayers
        /// </remarks>
        [HttpGet("available")]
        public async Task<ActionResult<ApiResponseDTO>> GetAvailablePlayers(
            [FromQuery] string? position = null)
        {
            var players = await _nflPlayerService.GetAvailablePlayersAsync(position);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores disponibles."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores disponibles obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// GET /api/nflplayer/by-nfl-team/{nflTeamId}
        /// </summary>
        /// <param name="nflTeamId">ID del equipo NFL</param>
        /// <returns>Lista de jugadores del equipo</returns>
        /// <response code="200">Jugadores del equipo obtenidos exitosamente</response>
        /// <remarks>
        /// Retorna todos los jugadores activos del equipo NFL especificado.
        /// VIEW: vw_PlayersByNFLTeam
        /// </remarks>
        [HttpGet("by-nfl-team/{nflTeamId}")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayersByNFLTeam(int nflTeamId)
        {
            var players = await _nflPlayerService.GetPlayersByNFLTeamAsync(nflTeamId);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores del equipo NFL."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores del equipo obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Obtiene jugadores NFL activos (para dropdowns).
        /// GET /api/nflplayer/active
        /// </summary>
        /// <param name="position">Filtrar por posición (opcional)</param>
        /// <returns>Lista de jugadores activos</returns>
        /// <response code="200">Jugadores activos obtenidos exitosamente</response>
        /// <remarks>
        /// Usado para selectores/dropdowns en la UI.
        /// VIEW: vw_ActiveNFLPlayers
        /// </remarks>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponseDTO>> GetActiveNFLPlayers(
            [FromQuery] string? position = null)
        {
            var players = await _nflPlayerService.GetActiveNFLPlayersAsync(position);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores NFL activos."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores NFL activos obtenidos exitosamente.",
                players
            ));
        }
    }
}