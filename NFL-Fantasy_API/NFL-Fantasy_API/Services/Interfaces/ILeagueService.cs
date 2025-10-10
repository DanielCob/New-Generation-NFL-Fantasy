using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
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
        Task<ApiResponseDTO> CreateLeagueAsync(CreateLeagueDTO dto, int creatorUserId);

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
        Task<ApiResponseDTO> EditLeagueConfigAsync(int leagueId, EditLeagueConfigDTO dto, int actorUserId);

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
        Task<ApiResponseDTO> SetLeagueStatusAsync(int leagueId, SetLeagueStatusDTO dto, int actorUserId);

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
        /// Valida que TeamSlots sea uno de los valores permitidos
        /// Valores válidos: 4, 6, 8, 10, 12, 14, 16, 18, 20
        /// </summary>
        /// <param name="teamSlots">Valor a validar</param>
        /// <returns>True si es válido</returns>
        bool IsValidTeamSlots(byte teamSlots);

        /// <summary>
        /// Valida complejidad de contraseña de liga (misma política que usuarios)
        /// Reglas: 8-12 caracteres, alfanumérica, al menos 1 mayúscula, 1 minúscula, 1 dígito
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Lista de errores (vacía si es válida)</returns>
        List<string> ValidateLeaguePasswordComplexity(string password);
    }
}