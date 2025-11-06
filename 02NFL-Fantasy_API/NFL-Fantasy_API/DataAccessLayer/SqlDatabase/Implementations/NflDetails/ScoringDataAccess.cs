using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de esquemas de puntuación.
    /// Responsabilidad: Ejecución de queries a VIEWs de scoring.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class ScoringDataAccess
    {
        private readonly IDatabaseHelper _db;

        public ScoringDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Scoring Schemas

        /// <summary>
        /// Lista todos los esquemas de puntuación.
        /// VIEW: vw_ScoringSchemas
        /// </summary>
        public async Task<List<ScoringSchemaVM>> ListSchemasAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_ScoringSchemas",
                reader => new ScoringSchemaVM
                {
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    Name = reader.GetSafeString("Name"),
                    Version = reader.GetSafeInt32("Version"),
                    IsTemplate = reader.GetSafeBool("IsTemplate"),
                    Description = reader.GetSafeNullableString("Description"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                orderBy: "Name, Version"
            );
        }

        /// <summary>
        /// Obtiene reglas de puntuación de un esquema.
        /// VIEW: vw_ScoringSchemaRules
        /// </summary>
        public async Task<List<ScoringSchemaRuleVM>> GetSchemaRulesAsync(int scoringSchemaId)
        {
            return await _db.ExecuteViewAsync(
                "vw_ScoringSchemaRules",
                reader => new ScoringSchemaRuleVM
                {
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    Name = reader.GetSafeString("Name"),
                    Version = reader.GetSafeInt32("Version"),
                    MetricCode = reader.GetSafeString("MetricCode"),
                    PointsPerUnit = reader.GetSafeDecimal("PointsPerUnit") == 0
                        ? null : reader.GetSafeDecimal("PointsPerUnit"),
                    Unit = reader.GetSafeNullableString("Unit"),
                    UnitValue = reader.GetSafeInt32("UnitValue") == 0
                        ? null : reader.GetSafeInt32("UnitValue"),
                    FlatPoints = reader.GetSafeDecimal("FlatPoints") == 0
                        ? null : reader.GetSafeDecimal("FlatPoints")
                },
                whereClause: $"ScoringSchemaID = {scoringSchemaId}"
            );
        }

        /// <summary>
        /// Obtiene un esquema específico por ID.
        /// VIEW: vw_ScoringSchemas con WHERE
        /// </summary>
        public async Task<ScoringSchemaVM?> GetSchemaByIdAsync(int scoringSchemaId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_ScoringSchemas",
                reader => new ScoringSchemaVM
                {
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    Name = reader.GetSafeString("Name"),
                    Version = reader.GetSafeInt32("Version"),
                    IsTemplate = reader.GetSafeBool("IsTemplate"),
                    Description = reader.GetSafeNullableString("Description"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                whereClause: $"ScoringSchemaID = {scoringSchemaId}"
            );

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene el esquema por defecto (Name='Default', Version=1).
        /// VIEW: vw_ScoringSchemas con WHERE
        /// </summary>
        public async Task<ScoringSchemaVM?> GetDefaultSchemaAsync()
        {
            var results = await _db.ExecuteViewAsync(
                "vw_ScoringSchemas",
                reader => new ScoringSchemaVM
                {
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    Name = reader.GetSafeString("Name"),
                    Version = reader.GetSafeInt32("Version"),
                    IsTemplate = reader.GetSafeBool("IsTemplate"),
                    Description = reader.GetSafeNullableString("Description"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                whereClause: "Name = 'Default' AND Version = 1"
            );

            return results.FirstOrDefault();
        }

        #endregion
    }
}