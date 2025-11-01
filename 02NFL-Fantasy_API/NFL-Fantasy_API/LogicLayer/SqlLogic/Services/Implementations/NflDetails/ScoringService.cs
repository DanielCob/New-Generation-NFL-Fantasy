using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.NflDetails
{
    /// <summary>
    /// Implementación del servicio de esquemas de puntuación.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye queries (delegado a ScoringDataAccess).
    /// Maneja consultas de esquemas y reglas de puntuación.
    /// </summary>
    public class ScoringService : IScoringService
    {
        private readonly ScoringDataAccess _dataAccess;
        private readonly ILogger<ScoringService> _logger;

        public ScoringService(
            ScoringDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<ScoringService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Scoring Schemas

        /// <summary>
        /// Lista todos los esquemas de puntuación.
        /// VIEW: vw_ScoringSchemas
        /// Feature 1.2 - Editar configuración (seleccionar esquema)
        /// </summary>
        public async Task<List<ScoringSchemaVM>> ListSchemasAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.ListSchemasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar esquemas de puntuación");
                return new List<ScoringSchemaVM>();
            }
        }

        /// <summary>
        /// Obtiene reglas de puntuación de un esquema.
        /// VIEW: vw_ScoringSchemaRules
        /// </summary>
        public async Task<List<ScoringSchemaRuleVM>> GetSchemaRulesAsync(int scoringSchemaId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetSchemaRulesAsync(scoringSchemaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener reglas del esquema {ScoringSchemaId}",
                    scoringSchemaId
                );
                return new List<ScoringSchemaRuleVM>();
            }
        }

        /// <summary>
        /// Obtiene un esquema específico por ID.
        /// VIEW: vw_ScoringSchemas con WHERE
        /// </summary>
        public async Task<ScoringSchemaVM?> GetSchemaByIdAsync(int scoringSchemaId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetSchemaByIdAsync(scoringSchemaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener esquema {ScoringSchemaId}",
                    scoringSchemaId
                );
                return null;
            }
        }

        /// <summary>
        /// Obtiene el esquema por defecto (Name='Default', Version=1).
        /// </summary>
        public async Task<ScoringSchemaVM?> GetDefaultSchemaAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetDefaultSchemaAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener esquema por defecto");
                return null;
            }
        }

        #endregion
    }
}