using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de jugadores NFL
    /// Endpoints: ListPlayers, GetAvailablePlayers, GetPlayersByPosition, GetPlayersByNFLTeam
    /// Feature 3.1 / 10.1: Gestión de jugadores NFL para rosters
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        /// Lista todos los jugadores NFL
        /// GET /api/player
        /// Incluye filtros opcionales por posición, equipo NFL, estado de lesión
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> ListPlayers([FromQuery] string? position, [FromQuery] int? nflTeamId, [FromQuery] string? injuryStatus)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var players = await _playerService.ListPlayersAsync(position, nflTeamId, injuryStatus);
                return Ok(ApiResponseDTO.SuccessResponse("Jugadores obtenidos.", players));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing players");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener jugadores."));
            }
        }

        /// <summary>
        /// Lista jugadores disponibles (no están en ningún roster activo)
        /// GET /api/player/available
        /// Para draft y free agency
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult> GetAvailablePlayers([FromQuery] string? position)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var players = await _playerService.GetAvailablePlayersAsync(position);
                return Ok(ApiResponseDTO.SuccessResponse("Jugadores disponibles obtenidos.", players));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available players");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener jugadores disponibles."));
            }
        }

        /// <summary>
        /// Obtiene jugadores agrupados por equipo NFL
        /// GET /api/player/by-nfl-team/{nflTeamId}
        /// </summary>
        [HttpGet("by-nfl-team/{nflTeamId}")]
        public async Task<ActionResult> GetPlayersByNFLTeam(int nflTeamId)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var players = await _playerService.GetPlayersByNFLTeamAsync(nflTeamId);
                return Ok(ApiResponseDTO.SuccessResponse("Jugadores del equipo obtenidos.", players));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting players for NFL team {TeamID}", nflTeamId);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener jugadores del equipo."));
            }
        }
    }
}