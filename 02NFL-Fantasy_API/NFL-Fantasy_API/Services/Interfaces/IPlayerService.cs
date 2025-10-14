using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de gestión de jugadores NFL
    /// Feature 3.1 / 10.1: Gestión de jugadores para rosters
    /// Mapea a VIEWs: vw_Players, vw_AvailablePlayers, vw_PlayersByNFLTeam
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>
        /// Lista todos los jugadores NFL
        /// VIEW: vw_Players con filtros opcionales
        /// </summary>
        Task<List<PlayerBasicDTO>> ListPlayersAsync(string? position = null, int? nflTeamId = null, string? injuryStatus = null);

        /// <summary>
        /// Lista jugadores disponibles (no en ningún roster activo)
        /// VIEW: vw_AvailablePlayers
        /// Para draft y free agency
        /// </summary>
        Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position = null);

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico
        /// VIEW: vw_PlayersByNFLTeam
        /// </summary>
        Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId);

        /// <summary>
        /// Obtiene un jugador específico por ID
        /// VIEW: vw_Players con WHERE
        /// </summary>
        Task<PlayerBasicDTO?> GetPlayerByIdAsync(int playerId);
    }
}
