using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy
{
    /// <summary>
    /// Servicio de gestión de equipos fantasy
    /// Feature 3.1: Creación y administración de equipos fantasy
    /// Mapea a SPs: sp_UpdateTeamBranding, sp_GetMyTeam, sp_GetTeamRosterDistribution,
    /// sp_AddPlayerToRoster, sp_RemovePlayerFromRoster
    /// </summary>
    public interface ITeamService
    {
        /// <summary>
        /// Actualiza el branding de un equipo fantasy
        /// SP: app.sp_UpdateTeamBranding
        /// Feature 3.1 - Editar branding de equipo
        /// Solo el dueño del equipo puede editarlo
        /// Valida unicidad de nombre dentro de la liga
        /// </summary>
        Task<ApiResponseDTO> UpdateTeamBrandingAsync(int teamId, UpdateTeamBrandingDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Obtiene información completa del equipo con roster
        /// SP: app.sp_GetMyTeam (retorna 3 result sets)
        /// Feature 3.1 - Ver mi equipo
        /// RS1: Información del equipo
        /// RS2: Jugadores en roster (con filtros aplicados)
        /// RS3: Distribución porcentual por tipo de adquisición
        /// </summary>
        Task<MyTeamResponseDTO?> GetMyTeamAsync(int teamId, int actorUserId, string? filterPosition = null, string? searchPlayer = null);

        /// <summary>
        /// Obtiene distribución porcentual del roster
        /// SP: app.sp_GetTeamRosterDistribution
        /// Feature 3.1 - Distribución de adquisición
        /// </summary>
        Task<List<RosterDistributionItemDTO>> GetTeamRosterDistributionAsync(int teamId);

        /// <summary>
        /// Agrega un jugador al roster del equipo
        /// SP: app.sp_AddPlayerToRoster
        /// Feature 3.1 - Gestión de roster
        /// Validaciones:
        /// - Jugador debe existir y estar activo
        /// - No puede estar en dos equipos activos de la misma liga
        /// - No permite duplicados en el mismo equipo
        /// </summary>
        Task<ApiResponseDTO> AddPlayerToRosterAsync(int teamId, AddPlayerToRosterDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Remueve un jugador del roster
        /// SP: app.sp_RemovePlayerFromRoster
        /// Feature 3.1 - Gestión de roster
        /// Soft delete: marca IsActive=0, registra DroppedDate
        /// </summary>
        Task<ApiResponseDTO> RemovePlayerFromRosterAsync(int rosterId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Obtiene detalles de un equipo fantasy desde VIEW
        /// VIEW: vw_FantasyTeamDetails
        /// </summary>
        Task<FantasyTeamDetailsVM?> GetFantasyTeamDetailsAsync(int teamId);
    }
}