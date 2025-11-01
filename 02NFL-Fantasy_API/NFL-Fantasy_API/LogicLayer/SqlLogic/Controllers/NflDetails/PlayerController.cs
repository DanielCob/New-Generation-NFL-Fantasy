using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de gestión de jugadores NFL.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para jugadores NFL
    /// - Listar jugadores con filtros
    /// - Consultar jugadores disponibles (free agents)
    /// - Consultar jugadores por equipo NFL
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// 
    /// ENDPOINTS:
    /// - GET /api/player - Listar todos los jugadores con filtros
    /// - GET /api/player/available - Jugadores disponibles (no en roster)
    /// - GET /api/player/by-nfl-team/{nflTeamId} - Jugadores de un equipo NFL
    /// 
    /// Feature 3.1 / 10.1: Gestión de jugadores NFL para rosters
    /// </summary>
    [ApiController]
    [Route("api/player")]
    [Authorize]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerService _playerService;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(IPlayerService playerService, ILogger<PlayerController> logger)
        {
            _playerService = playerService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los jugadores NFL.
        /// GET /api/player
        /// </summary>
        /// <param name="position">Filtrar por posición (QB, RB, WR, TE, K, DEF)</param>
        /// <param name="nflTeamId">Filtrar por equipo NFL</param>
        /// <param name="injuryStatus">Filtrar por estado de lesión</param>
        /// <returns>Lista de jugadores</returns>
        /// <response code="200">Jugadores obtenidos exitosamente</response>
        /// <remarks>
        /// FILTROS OPCIONALES:
        /// - position: QB, RB, WR, TE, K, DEF
        /// - nflTeamId: ID del equipo NFL
        /// - injuryStatus: Healthy, Questionable, Doubtful, Out, IR
        /// 
        /// Si no se especifican filtros, retorna todos los jugadores activos.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDTO>> ListPlayers(
            [FromQuery] string? position = null,
            [FromQuery] int? nflTeamId = null,
            [FromQuery] string? injuryStatus = null)
        {
            var players = await _playerService.ListPlayersAsync(
                position,
                nflTeamId,
                injuryStatus
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Lista jugadores disponibles (no están en ningún roster activo).
        /// GET /api/player/available
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
        /// </remarks>
        [HttpGet("available")]
        public async Task<ActionResult<ApiResponseDTO>> GetAvailablePlayers(
            [FromQuery] string? position = null)
        {
            var players = await _playerService.GetAvailablePlayersAsync(position);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores disponibles obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// GET /api/player/by-nfl-team/{nflTeamId}
        /// </summary>
        /// <param name="nflTeamId">ID del equipo NFL</param>
        /// <returns>Lista de jugadores del equipo</returns>
        /// <response code="200">Jugadores del equipo obtenidos exitosamente</response>
        /// <remarks>
        /// Retorna todos los jugadores activos del equipo NFL especificado.
        /// </remarks>
        [HttpGet("by-nfl-team/{nflTeamId}")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayersByNFLTeam(int nflTeamId)
        {
            var players = await _playerService.GetPlayersByNFLTeamAsync(nflTeamId);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores del equipo obtenidos exitosamente.",
                players
            ));
        }
    }
}