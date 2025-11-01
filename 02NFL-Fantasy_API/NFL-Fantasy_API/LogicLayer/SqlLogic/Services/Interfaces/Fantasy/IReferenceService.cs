using NFL_Fantasy_API.Models.ViewModels.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy
{
    /// <summary>
    /// Servicio de datos de referencia del sistema.
    /// 
    /// VIEWS:
    /// - vw_PositionFormats - Formatos disponibles
    /// - vw_PositionFormatSlots - Slots por formato
    /// 
    /// STORED PROCEDURES (alternativos):
    /// - app.sp_ListPositionFormats
    /// - app.sp_GetCurrentSeason
    /// 
    /// Feature 1.2: Editar configuración de liga
    /// </summary>
    public interface IReferenceService
    {
        /// <summary>
        /// Lista todos los formatos de posiciones disponibles.
        /// </summary>
        /// <returns>Lista de formatos disponibles</returns>
        /// <remarks>
        /// VIEW: vw_PositionFormats
        /// SP alternativo: app.sp_ListPositionFormats
        /// 
        /// Formatos típicos: Default, Extremo, Detallado, Ofensivo
        /// </remarks>
        Task<List<PositionFormatVM>> ListPositionFormatsAsync();

        /// <summary>
        /// Obtiene los slots/posiciones de un formato específico.
        /// </summary>
        /// <param name="positionFormatId">ID del formato</param>
        /// <returns>Lista de posiciones con cantidad de slots</returns>
        /// <remarks>
        /// VIEW: vw_PositionFormatSlots
        /// 
        /// Ejemplo (Default): 1 QB, 2 RB, 2 WR, 1 RB/WR, 1 TE, 1 K, 1 DEF, 6 BENCH, 3 IR
        /// </remarks>
        Task<List<PositionFormatSlotVM>> GetPositionFormatSlotsAsync(int positionFormatId);

        /// <summary>
        /// Obtiene un formato de posiciones específico por ID.
        /// </summary>
        /// <param name="positionFormatId">ID del formato</param>
        /// <returns>Formato o null si no existe</returns>
        /// <remarks>
        /// VIEW: vw_PositionFormats con WHERE PositionFormatID = @ID
        /// </remarks>
        Task<PositionFormatVM?> GetPositionFormatByIdAsync(int positionFormatId);
    }
}