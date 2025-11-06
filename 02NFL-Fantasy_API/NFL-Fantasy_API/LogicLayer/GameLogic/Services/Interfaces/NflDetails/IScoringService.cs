using NFL_Fantasy_API.Models.ViewModels.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails
{
    /// <summary>
    /// Servicio de esquemas de puntuación
    /// Mapea a VIEWs: vw_ScoringSchemas, vw_ScoringSchemaRules
    /// Y SPs: sp_ListScoringSchemas
    /// </summary>
    public interface IScoringService
    {
        /// <summary>
        /// Lista todos los esquemas de puntuación disponibles
        /// VIEW: vw_ScoringSchemas
        /// SP alternativo: app.sp_ListScoringSchemas
        /// Feature 1.2 - Editar configuración (seleccionar esquema)
        /// Esquemas: Default, PrioridadCarrera, MaxPuntos, PrioridadDefensa
        /// </summary>
        /// <returns>Lista de esquemas con versiones</returns>
        Task<List<ScoringSchemaVM>> ListSchemasAsync();

        /// <summary>
        /// Obtiene las reglas de puntuación de un esquema específico
        /// VIEW: vw_ScoringSchemaRules
        /// Muestra cómo se puntúa cada métrica: PASS_YDS, PASS_TD, RUSH_YDS, etc.
        /// </summary>
        /// <param name="scoringSchemaId">ID del esquema</param>
        /// <returns>Lista de reglas de puntuación</returns>
        Task<List<ScoringSchemaRuleVM>> GetSchemaRulesAsync(int scoringSchemaId);

        /// <summary>
        /// Obtiene un esquema de puntuación específico por ID
        /// VIEW: vw_ScoringSchemas con WHERE
        /// </summary>
        /// <param name="scoringSchemaId">ID del esquema</param>
        /// <returns>Esquema o null si no existe</returns>
        Task<ScoringSchemaVM?> GetSchemaByIdAsync(int scoringSchemaId);

        /// <summary>
        /// Obtiene el esquema por defecto (Name='Default', Version=1)
        /// </summary>
        /// <returns>Esquema por defecto</returns>
        Task<ScoringSchemaVM?> GetDefaultSchemaAsync();
    }
}