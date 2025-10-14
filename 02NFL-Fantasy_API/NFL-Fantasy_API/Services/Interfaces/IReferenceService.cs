using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de datos de referencia del sistema
    /// Mapea a VIEWs: vw_CurrentSeason, vw_PositionFormats, vw_PositionFormatSlots
    /// Y SPs: sp_GetCurrentSeason, sp_ListPositionFormats
    /// </summary>
    public interface IReferenceService
    {
        /// <summary>
        /// Obtiene la temporada actual (IsCurrent=1)
        /// VIEW: vw_CurrentSeason
        /// SP alternativo: app.sp_GetCurrentSeason
        /// Feature 1.2 - Crear liga (necesita temporada actual)
        /// </summary>
        /// <returns>Temporada actual o null si no hay ninguna marcada</returns>
        Task<CurrentSeasonVM?> GetCurrentSeasonAsync();

        /// <summary>
        /// Lista todos los formatos de posiciones disponibles
        /// VIEW: vw_PositionFormats
        /// SP alternativo: app.sp_ListPositionFormats
        /// Feature 1.2 - Editar configuración (seleccionar formato)
        /// Formatos: Default, Extremo, Detallado, Ofensivo
        /// </summary>
        /// <returns>Lista de formatos disponibles</returns>
        Task<List<PositionFormatVM>> ListPositionFormatsAsync();

        /// <summary>
        /// Obtiene los slots/posiciones de un formato específico
        /// VIEW: vw_PositionFormatSlots
        /// Ejemplo: Default tiene 1 QB, 2 RB, 2 WR, 1 RB/WR, 1 TE, 1 K, 1 DEF, 6 BENCH, 3 IR
        /// </summary>
        /// <param name="positionFormatId">ID del formato</param>
        /// <returns>Lista de posiciones con cantidad de slots</returns>
        Task<List<PositionFormatSlotVM>> GetPositionFormatSlotsAsync(int positionFormatId);

        /// <summary>
        /// Obtiene un formato de posiciones específico por ID
        /// VIEW: vw_PositionFormats con WHERE
        /// </summary>
        /// <param name="positionFormatId">ID del formato</param>
        /// <returns>Formato o null si no existe</returns>
        Task<PositionFormatVM?> GetPositionFormatByIdAsync(int positionFormatId);
    }
}