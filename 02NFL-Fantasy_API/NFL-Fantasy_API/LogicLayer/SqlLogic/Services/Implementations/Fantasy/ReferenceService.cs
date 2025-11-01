using NFL_Fantasy_API.Models.ViewModels.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Fantasy
{
    /// <summary>
    /// Implementación del servicio de datos de referencia.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye queries (delegado a ReferenceDataAccess).
    /// Maneja temporadas, formatos de posiciones y datos del sistema.
    /// </summary>
    public class ReferenceService : IReferenceService
    {
        private readonly ReferenceDataAccess _dataAccess;
        private readonly ILogger<ReferenceService> _logger;

        public ReferenceService(
            ReferenceDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<ReferenceService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Position Formats

        /// <summary>
        /// Lista todos los formatos de posiciones.
        /// VIEW: vw_PositionFormats
        /// </summary>
        public async Task<List<PositionFormatVM>> ListPositionFormatsAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.ListPositionFormatsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar formatos de posiciones");
                return new List<PositionFormatVM>();
            }
        }

        /// <summary>
        /// Obtiene los slots de un formato específico.
        /// VIEW: vw_PositionFormatSlots
        /// </summary>
        public async Task<List<PositionFormatSlotVM>> GetPositionFormatSlotsAsync(int positionFormatId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPositionFormatSlotsAsync(positionFormatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener slots de formato: FormatId={PositionFormatId}",
                    positionFormatId
                );
                return new List<PositionFormatSlotVM>();
            }
        }

        /// <summary>
        /// Obtiene un formato específico por ID.
        /// VIEW: vw_PositionFormats con WHERE
        /// </summary>
        public async Task<PositionFormatVM?> GetPositionFormatByIdAsync(int positionFormatId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPositionFormatByIdAsync(positionFormatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener formato por ID: FormatId={PositionFormatId}",
                    positionFormatId
                );
                return null;
            }
        }

        #endregion
    }
}