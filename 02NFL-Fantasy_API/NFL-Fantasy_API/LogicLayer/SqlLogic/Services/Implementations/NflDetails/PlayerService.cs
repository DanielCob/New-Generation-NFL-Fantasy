using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.NflDetails
{
    /// <summary>
    /// Implementación del servicio de gestión de jugadores NFL.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye queries (delegado a PlayerDataAccess).
    /// Feature 3.1 / 10.1: Gestión de jugadores para rosters.
    /// </summary>
    public class PlayerService : IPlayerService
    {
        private readonly PlayerDataAccess _dataAccess;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(
            PlayerDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<PlayerService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region List Players

        /// <summary>
        /// Lista todos los jugadores NFL con filtros opcionales.
        /// VIEW: vw_Players
        /// </summary>
        public async Task<List<PlayerBasicDTO>> ListPlayersAsync(
            string? position = null,
            int? nflTeamId = null,
            string? injuryStatus = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.ListPlayersAsync(position, nflTeamId, injuryStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar jugadores: Position={Position}, NFLTeamId={NFLTeamId}, InjuryStatus={InjuryStatus}",
                    position,
                    nflTeamId,
                    injuryStatus
                );
                throw;
            }
        }

        #endregion

        #region Available Players

        /// <summary>
        /// Lista jugadores disponibles (no en ningún roster activo).
        /// VIEW: vw_AvailablePlayers
        /// </summary>
        public async Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetAvailablePlayersAsync(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores disponibles: Position={Position}",
                    position
                );
                throw;
            }
        }

        #endregion

        #region Players by NFL Team

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// VIEW: vw_PlayersByNFLTeam
        /// </summary>
        public async Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayersByNFLTeamAsync(nflTeamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores de equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                throw;
            }
        }

        #endregion

        #region Get Player by ID

        /// <summary>
        /// Obtiene un jugador específico por ID.
        /// VIEW: vw_Players con WHERE
        /// </summary>
        public async Task<PlayerBasicDTO?> GetPlayerByIdAsync(int playerId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayerByIdAsync(playerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugador {PlayerId}",
                    playerId
                );
                throw;
            }
        }

        #endregion
    }
}