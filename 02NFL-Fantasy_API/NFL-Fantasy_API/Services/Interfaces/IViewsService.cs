using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de vistas/reportes administrativos
    /// Consolida acceso a vistas complejas para reportería
    /// Endpoints bajo /api/views/* (ADMIN-only por middleware)
    /// </summary>
    public interface IViewsService
    {
        /// <summary>
        /// Obtiene el resumen de una liga desde la VIEW
        /// VIEW: vw_LeagueSummary
        /// Alternativa a sp_GetLeagueSummary (sin equipos)
        /// </summary>
        /// <param name="leagueId">ID de la liga</param>
        /// <returns>Resumen de liga</returns>
        Task<LeagueSummaryVM?> GetLeagueSummaryViewAsync(int leagueId);

        /// <summary>
        /// Obtiene todas las ligas del sistema
        /// VIEW: vw_LeagueDirectory
        /// Para reportes administrativos
        /// </summary>
        /// <returns>Lista completa de ligas</returns>
        Task<List<LeagueDirectoryVM>> GetAllLeaguesAsync();

        /// <summary>
        /// Obtiene todos los usuarios activos
        /// VIEW: vw_UserProfileBasic con WHERE AccountStatus=1
        /// </summary>
        /// <returns>Lista de usuarios activos</returns>
        Task<List<UserProfileBasicVM>> GetActiveUsersAsync();

        /// <summary>
        /// Obtiene estadísticas generales del sistema
        /// Combina múltiples VIEWs para dashboard administrativo
        /// </summary>
        /// <returns>Objeto con estadísticas generales</returns>
        Task<object> GetSystemStatsAsync();
    }
}