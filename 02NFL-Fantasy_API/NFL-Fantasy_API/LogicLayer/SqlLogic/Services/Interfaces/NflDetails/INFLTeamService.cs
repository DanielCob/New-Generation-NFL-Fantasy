using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails
{
    /// <summary>
    /// Servicio de gestión de equipos NFL
    /// Feature 10.1: Gestión de Equipos NFL (CRUD)
    /// Mapea a SPs: sp_CreateNFLTeam, sp_UpdateNFLTeam, sp_DeactivateNFLTeam, 
    /// sp_ReactivateNFLTeam, sp_ListNFLTeams, sp_GetNFLTeamDetails
    /// </summary>
    public interface INFLTeamService
    {
        /// <summary>
        /// Crea un nuevo equipo NFL
        /// SP: app.sp_CreateNFLTeam
        /// Feature 10.1 - Crear equipo NFL
        /// </summary>
        Task<ApiResponseDTO> CreateNFLTeamAsync(CreateNFLTeamDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Lista equipos NFL con paginación y filtros
        /// SP: app.sp_ListNFLTeams
        /// Feature 10.1 - Listar equipos NFL
        /// Paginación: 50 por página (máx 100)
        /// Filtros: búsqueda por nombre/ciudad, estado activo/inactivo
        /// </summary>
        Task<ListNFLTeamsResponseDTO> ListNFLTeamsAsync(ListNFLTeamsRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de un equipo NFL
        /// SP: app.sp_GetNFLTeamDetails (retorna 3 result sets)
        /// Feature 10.1 - Ver detalles
        /// RS1: Información del equipo
        /// RS2: Historial de cambios (últimos 20)
        /// RS3: Jugadores activos del equipo
        /// </summary>
        Task<NFLTeamDetailsDTO?> GetNFLTeamDetailsAsync(int nflTeamId);

        /// <summary>
        /// Actualiza un equipo NFL existente
        /// SP: app.sp_UpdateNFLTeam
        /// Feature 10.1 - Modificar equipo NFL
        /// </summary>
        Task<ApiResponseDTO> UpdateNFLTeamAsync(int nflTeamId, UpdateNFLTeamDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Desactiva un equipo NFL
        /// SP: app.sp_DeactivateNFLTeam
        /// Feature 10.1 - Desactivar equipo NFL
        /// Valida que no tenga partidos programados en temporada actual
        /// </summary>
        Task<ApiResponseDTO> DeactivateNFLTeamAsync(int nflTeamId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Reactiva un equipo NFL desactivado
        /// SP: app.sp_ReactivateNFLTeam
        /// Feature 10.1 - Reactivar equipo NFL
        /// </summary>
        Task<ApiResponseDTO> ReactivateNFLTeamAsync(int nflTeamId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Obtiene equipos NFL activos (para dropdowns)
        /// VIEW: vw_ActiveNFLTeams
        /// </summary>
        Task<List<NFLTeamBasicVM>> GetActiveNFLTeamsAsync();
    }
}