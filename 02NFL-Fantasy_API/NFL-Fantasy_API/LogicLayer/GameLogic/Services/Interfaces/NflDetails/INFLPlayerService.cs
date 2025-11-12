using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails
{
    /// <summary>
    /// Servicio de gestión de jugadores NFL
    /// Feature: Gestión de Jugadores NFL (CRUD)
    /// Mapea a SPs: sp_CreateNFLPlayer, sp_UpdateNFLPlayer, sp_DeactivateNFLPlayer, 
    /// sp_ReactivateNFLPlayer, sp_ListNFLPlayers, sp_GetNFLPlayerDetails
    /// Mapea a VIEWs: vw_AvailablePlayers, vw_PlayersByNFLTeam, vw_ActiveNFLPlayers
    /// </summary>
    public interface INFLPlayerService
    {
        /// <summary>
        /// Crea un nuevo jugador NFL
        /// SP: app.sp_CreateNFLPlayer
        /// Feature: Crear jugador NFL
        /// </summary>
        Task<ApiResponseDTO> CreateNFLPlayerAsync(CreateNFLPlayerDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros
        /// SP: app.sp_ListNFLPlayers
        /// Feature: Listar jugadores NFL
        /// Paginación: 50 por página (máx 100)
        /// Filtros: búsqueda por nombre, posición, equipo NFL, estado activo/inactivo
        /// </summary>
        Task<ListNFLPlayersResponseDTO> ListNFLPlayersAsync(ListNFLPlayersRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL
        /// SP: app.sp_GetNFLPlayerDetails (retorna 3 result sets)
        /// Feature: Ver detalles de jugador
        /// RS1: Información del jugador
        /// RS2: Historial de cambios (últimos 20)
        /// RS3: Equipos fantasy actuales que tienen este jugador
        /// </summary>
        Task<NFLPlayerDetailsDTO?> GetNFLPlayerDetailsAsync(int nflPlayerId);

        /// <summary>
        /// Actualiza un jugador NFL existente
        /// SP: app.sp_UpdateNFLPlayer
        /// Feature: Modificar jugador NFL
        /// </summary>
        Task<ApiResponseDTO> UpdateNFLPlayerAsync(int nflPlayerId, UpdateNFLPlayerDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Desactiva un jugador NFL
        /// SP: app.sp_DeactivateNFLPlayer
        /// Feature: Desactivar jugador NFL
        /// Valida que no esté en roster activo de equipos en temporada actual
        /// </summary>
        Task<ApiResponseDTO> DeactivateNFLPlayerAsync(int nflPlayerId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Reactiva un jugador NFL desactivado
        /// SP: app.sp_ReactivateNFLPlayer
        /// Feature: Reactivar jugador NFL
        /// </summary>
        Task<ApiResponseDTO> ReactivateNFLPlayerAsync(int nflPlayerId, int actorUserId, string? sourceIp = null, string? userAgent = null);

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
        /// Obtiene jugadores NFL activos (para dropdowns)
        /// VIEW: vw_ActiveNFLPlayers
        /// </summary>
        Task<List<PlayerBasicDTO>> GetActiveNFLPlayersAsync(string? position = null);

        /// <summary>
        /// Obtiene un jugador específico por ID
        /// VIEW: vw_Players con WHERE
        /// </summary>
        Task<PlayerBasicDTO?> GetPlayerByIdAsync(int nflPlayerId);
    }
}