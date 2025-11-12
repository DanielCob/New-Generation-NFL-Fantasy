using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails
{
    /// <summary>
    /// Servicio de gestión de temporadas de la NFL
    /// Mapea a VIEWs: vw_Seasons, vw_SeasonWeeks
    /// Y SPs: app.sp_CreateSeason, app.sp_UpdateSeason, app.sp_DeactivateSeason, app.sp_DeleteSeason
    /// 
    /// Notas de seguridad:
    /// - GET /api/seasons/current es público (no requiere autenticación)
    /// - Resto de operaciones requieren rol ADMIN (enforced por AuthenticationMiddleware/Policies)
    /// </summary>
    public interface ISeasonService
    {
        /// <summary>
        /// Obtiene la temporada actual del sistema
        /// VIEW sugerida: vw_Seasons (WHERE IsCurrent = 1 AND IsActive = 1)
        /// Endpoint: GET /api/seasons/current (público)
        /// </summary>
        /// <returns>Temporada actual o null si no hay definida</returns>
        Task<SeasonVM?> GetCurrentSeasonAsync();

        /// <summary>
        /// Crea una nueva temporada
        /// SP: app.sp_CreateSeason
        /// Endpoint: POST /api/seasons (requiere ADMIN)
        /// Auditoría: incluye ActorUserID, IP y User-Agent
        /// Reglas de negocio típicas: no traslapar rangos de fechas con temporadas activas
        /// </summary>
        /// <param name="dto">Datos de creación (años, nombre, rango, flags)</param>
        /// <param name="actorUserId">Usuario actor (ADMIN)</param>
        /// <param name="ip">IP de origen</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Temporada creada o null si falla</returns>
        Task<SeasonVM?> CreateSeasonAsync(CreateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent);

        /// <summary>
        /// Desactiva una temporada existente (soft off)
        /// SP: app.sp_DeactivateSeason
        /// Endpoint: POST /api/seasons/{id}/deactivate (requiere ADMIN)
        /// Confirmación: parámetro 'confirm' debe ser true para proceder
        /// Efectos esperados: marcar IsActive = 0, validar impactos en ligas/jornadas
        /// </summary>
        /// <param name="seasonId">ID de la temporada</param>
        /// <param name="confirm">Debe ser true para confirmar acción</param>
        /// <param name="actorUserId">Usuario actor (ADMIN)</param>
        /// <param name="ip">IP de origen</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Mensaje de resultado (éxito o detalle del rechazo)</returns>
        Task<string> DeactivateSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent);

        /// <summary>
        /// Elimina una temporada (hard delete) 
        /// SP: app.sp_DeleteSeason
        /// Endpoint: DELETE /api/seasons/{id} (requiere ADMIN)
        /// Confirmación: parámetro 'confirm' debe ser true
        /// Reglas de protección: impedir borrado si hay referencias (ligas, weeks, etc.)
        /// </summary>
        /// <param name="seasonId">ID de la temporada</param>
        /// <param name="confirm">Debe ser true para confirmar acción irreversible</param>
        /// <param name="actorUserId">Usuario actor (ADMIN)</param>
        /// <param name="ip">IP de origen</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Mensaje de resultado</returns>
        Task<string> DeleteSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent);

        /// <summary>
        /// Actualiza metadatos de una temporada
        /// SP: app.sp_UpdateSeason
        /// Endpoint: PUT /api/seasons/{id} (requiere ADMIN)
        /// Validaciones típicas: no romper consistencia con weeks definidas; no traslapar rangos
        /// </summary>
        /// <param name="seasonId">ID de la temporada</param>
        /// <param name="dto">Datos de actualización</param>
        /// <param name="actorUserId">Usuario actor (ADMIN)</param>
        /// <param name="ip">IP de origen</param>
        /// <param name="userAgent">User-Agent del cliente</param>
        /// <returns>Temporada actualizada o null si no existe</returns>
        Task<SeasonVM?> UpdateSeasonAsync(int seasonId, UpdateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent);

        /// <summary>
        /// Lista las semanas (weeks) pertenecientes a una temporada
        /// VIEW: vw_SeasonWeeks (filtrada por SeasonID)
        /// Endpoint: GET /api/seasons/{id}/weeks
        /// </summary>
        /// <param name="seasonId">ID de la temporada</param>
        /// <returns>Listado de weeks</returns>
        Task<List<SeasonWeekVM>> GetSeasonWeeksAsync(int seasonId);

        /// <summary>
        /// Obtiene una temporada por ID
        /// VIEW: vw_Seasons (WHERE SeasonID = @id)
        /// Endpoint: GET /api/seasons/{id}
        /// </summary>
        /// <param name="seasonId">ID de la temporada</param>
        /// <returns>Temporada o null si no existe</returns>
        Task<SeasonVM?> GetSeasonByIdAsync(int seasonId);
    }
}
