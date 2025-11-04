using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy
{
    /// <summary>
    /// Servicio de gestión de ligas de fantasy
    /// Mapea a: sp_CreateLeague, sp_EditLeagueConfig, sp_SetLeagueStatus, sp_GetLeagueSummary,
    /// vw_LeagueDirectory, vw_LeagueMembers, vw_LeagueTeams
    /// </summary>
    public interface ILeagueService
    {
        /// <summary>
        /// Crea una nueva liga de fantasy
        /// SP: app.sp_CreateLeague
        /// Feature 1.2 - Crear liga
        /// - Asigna al creador como comisionado principal
        /// - Crea el equipo inicial del comisionado
        /// - Asigna formatos y esquemas por defecto si no se especifican
        /// - Liga queda en estado Pre-Draft (0)
        /// </summary>
        /// <param name="dto">Datos de la nueva liga</param>
        /// <param name="creatorUserId">ID del usuario creador (del contexto auth)</param>
        /// <returns>Datos de la liga creada con cupos disponibles</returns>
        Task<ApiResponseDTO> CreateLeagueAsync(CreateLeagueDTO dto, int creatorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Edita la configuración de una liga
        /// SP: app.sp_EditLeagueConfig
        /// Feature 1.2 - Editar configuración de liga
        /// IMPORTANTE: Algunos campos solo editables en Pre-Draft
        /// - Pre-Draft only: TeamSlots, PositionFormatID, ScoringSchemaID, PlayoffTeams, 
        ///   AllowDecimals, TradeDeadlineEnabled, TradeDeadlineDate
        /// - Siempre editables: Name, Description, MaxRosterChangesPerTeam, MaxFreeAgentAddsPerTeam
        /// Solo el comisionado principal puede editar
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="dto">Campos a actualizar (todos opcionales)</param>
        /// <param name="actorUserId">ID del usuario que realiza el cambio</param>
        /// <returns>Mensaje de confirmación o error</returns>
        Task<ApiResponseDTO> EditLeagueConfigAsync(int leagueId, EditLeagueConfigDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Cambia el estado de una liga
        /// SP: app.sp_SetLeagueStatus
        /// Feature 1.2 - Administrar estado de liga
        /// Estados: 0=PreDraft, 1=Active, 2=Inactive, 3=Closed
        /// Solo el comisionado principal puede cambiar el estado
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <param name="dto">Nuevo estado y razón opcional</param>
        /// <param name="actorUserId">ID del usuario que realiza el cambio</param>
        /// <returns>Mensaje de confirmación</returns>
        Task<ApiResponseDTO> SetLeagueStatusAsync(int leagueId, SetLeagueStatusDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Obtiene el resumen completo de una liga
        /// SP: app.sp_GetLeagueSummary (retorna 2 result sets: liga + equipos)
        /// Feature 1.2 - Ver liga
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Resumen completo con equipos</returns>
        Task<LeagueSummaryDTO?> GetLeagueSummaryAsync(int leagueId);

        /// <summary>
        /// Obtiene el directorio/listado de ligas
        /// VIEW: vw_LeagueDirectory
        /// Para búsqueda y navegación de ligas disponibles
        /// </summary>
        /// <returns>Lista de ligas con información básica</returns>
        Task<List<LeagueDirectoryVM>> GetLeagueDirectoryAsync();

        /// <summary>
        /// Obtiene los miembros de una liga específica
        /// VIEW: vw_LeagueMembers
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Lista de miembros con sus roles</returns>
        Task<List<LeagueMemberVM>> GetLeagueMembersAsync(int leagueId);

        /// <summary>
        /// Obtiene los equipos de una liga específica
        /// VIEW: vw_LeagueTeams
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Lista de equipos con sus owners</returns>
        Task<List<LeagueTeamVM>> GetLeagueTeamsAsync(int leagueId);

        /// <summary>
        /// Obtiene todos los roles efectivos de un usuario en una liga
        /// SP: app.sp_GetUserRolesInLeague
        /// Retorna roles explícitos, derivados y resumen
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Roles y resumen del usuario en la liga</returns>
        Task<GetUserRolesInLeagueResponseDTO?> GetUserRolesInLeagueAsync(int userId, int leagueId);

        /// <summary>
        /// Obtiene las ligas donde un usuario es comisionado
        /// VIEW: vw_UserCommissionedLeagues
        /// Feature 1.1 - Ver perfil (sección de ligas como comisionado)
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de ligas donde es comisionado</returns>
        Task<List<UserCommissionedLeagueVM>> GetUserCommissionedLeaguesAsync(int userId);

        /// <summary>
        /// Obtiene los equipos de un usuario en todas sus ligas
        /// VIEW: vw_UserTeams
        /// Feature 1.1 - Ver perfil (sección de equipos)
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de equipos del usuario</returns>
        Task<List<UserTeamVM>> GetUserTeamsAsync(int userId);

        /// <summary>
        /// Busca ligas disponibles para unirse
        /// </summary>
        Task<List<SearchLeaguesResultDTO>> SearchLeaguesAsync(SearchLeaguesRequestDTO request);

        /// <summary>
        /// Une a un usuario a una liga existente
        /// </summary>
        Task<JoinLeagueResultDTO> JoinLeagueAsync(int userId, JoinLeagueRequestDTO request, string? sourceIp, string? userAgent);

        /// <summary>
        /// Valida si una contraseña de liga es correcta
        /// </summary>
        Task<ValidateLeaguePasswordResultDTO> ValidateLeaguePasswordAsync(ValidateLeaguePasswordRequestDTO request);

        // ============================================================================
        // NUEVOS MÉTODOS - Gestión de Miembros
        // ============================================================================

        /// <summary>
        /// Remueve un equipo de la liga (solo comisionado)
        /// </summary>
        Task<ApiResponseDTO> RemoveTeamFromLeagueAsync(int actorUserId, int leagueId, RemoveTeamRequestDTO request, string? sourceIp, string? userAgent);

        /// <summary>
        /// Permite a un usuario salir voluntariamente de una liga
        /// </summary>
        Task<ApiResponseDTO> LeaveLeagueAsync(int userId, int leagueId, string? sourceIp, string? userAgent);

        /// <summary>
        /// Transfiere el rol de comisionado principal a otro miembro
        /// </summary>
        Task<ApiResponseDTO> TransferCommissionerAsync(int actorUserId, int leagueId, TransferCommissionerRequestDTO request, string? sourceIp, string? userAgent);

        /// <summary>
        /// Obtiene el resumen de una liga desde la VIEW (versión ligera).
        /// VIEW: vw_LeagueSummary
        /// Alternativa a sp_GetLeagueSummary (sin equipos detallados)
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Resumen de liga o null si no existe</returns>
        /// <remarks>
        /// Más rápido que GetLeagueSummaryAsync porque no trae equipos.
        /// Ideal para listados y dashboards.
        /// </remarks>
        Task<LeagueSummaryVM?> GetLeagueSummaryViewAsync(int leagueId);
    }
}