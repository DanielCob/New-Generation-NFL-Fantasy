using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de esquemas de puntuación
    /// Maneja consultas de esquemas y reglas de puntuación
    /// </summary>
    public class ScoringService : IScoringService
    {
        private readonly DatabaseHelper _db;

        public ScoringService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Scoring Schemas

        /// <summary>
        /// Lista todos los esquemas de puntuación
        /// VIEW: vw_ScoringSchemas
        /// Feature 1.2 - Editar configuración (seleccionar esquema)
        /// </summary>
        public async Task<List<ScoringSchemaVM>> ListSchemasAsync()
        {
            try
            {
                return await _db.ExecuteViewAsync<ScoringSchemaVM>(
                    "vw_ScoringSchemas",
                    reader => new ScoringSchemaVM
                    {
                        ScoringSchemaID = DatabaseHelper.GetSafeInt32(reader, "ScoringSchemaID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Version = DatabaseHelper.GetSafeInt32(reader, "Version"),
                        IsTemplate = DatabaseHelper.GetSafeBool(reader, "IsTemplate"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    orderBy: "Name, Version"
                );
            }
            catch
            {
                return new List<ScoringSchemaVM>();
            }
        }

        /// <summary>
        /// Obtiene reglas de puntuación de un esquema
        /// VIEW: vw_ScoringSchemaRules
        /// </summary>
        public async Task<List<ScoringSchemaRuleVM>> GetSchemaRulesAsync(int scoringSchemaId)
        {
            try
            {
                return await _db.ExecuteViewAsync<ScoringSchemaRuleVM>(
                    "vw_ScoringSchemaRules",
                    reader => new ScoringSchemaRuleVM
                    {
                        ScoringSchemaID = DatabaseHelper.GetSafeInt32(reader, "ScoringSchemaID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Version = DatabaseHelper.GetSafeInt32(reader, "Version"),
                        MetricCode = DatabaseHelper.GetSafeString(reader, "MetricCode"),
                        PointsPerUnit = DatabaseHelper.GetSafeDecimal(reader, "PointsPerUnit") == 0
                            ? null : DatabaseHelper.GetSafeDecimal(reader, "PointsPerUnit"),
                        Unit = DatabaseHelper.GetSafeNullableString(reader, "Unit"),
                        UnitValue = DatabaseHelper.GetSafeInt32(reader, "UnitValue") == 0
                            ? null : DatabaseHelper.GetSafeInt32(reader, "UnitValue"),
                        FlatPoints = DatabaseHelper.GetSafeDecimal(reader, "FlatPoints") == 0
                            ? null : DatabaseHelper.GetSafeDecimal(reader, "FlatPoints")
                    },
                    whereClause: $"ScoringSchemaID = {scoringSchemaId}"
                );
            }
            catch
            {
                return new List<ScoringSchemaRuleVM>();
            }
        }

        /// <summary>
        /// Obtiene un esquema específico por ID
        /// VIEW: vw_ScoringSchemas con WHERE
        /// </summary>
        public async Task<ScoringSchemaVM?> GetSchemaByIdAsync(int scoringSchemaId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<ScoringSchemaVM>(
                    "vw_ScoringSchemas",
                    reader => new ScoringSchemaVM
                    {
                        ScoringSchemaID = DatabaseHelper.GetSafeInt32(reader, "ScoringSchemaID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Version = DatabaseHelper.GetSafeInt32(reader, "Version"),
                        IsTemplate = DatabaseHelper.GetSafeBool(reader, "IsTemplate"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    whereClause: $"ScoringSchemaID = {scoringSchemaId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene el esquema por defecto (Name='Default', Version=1)
        /// </summary>
        public async Task<ScoringSchemaVM?> GetDefaultSchemaAsync()
        {
            try
            {
                var results = await _db.ExecuteViewAsync<ScoringSchemaVM>(
                    "vw_ScoringSchemas",
                    reader => new ScoringSchemaVM
                    {
                        ScoringSchemaID = DatabaseHelper.GetSafeInt32(reader, "ScoringSchemaID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Version = DatabaseHelper.GetSafeInt32(reader, "Version"),
                        IsTemplate = DatabaseHelper.GetSafeBool(reader, "IsTemplate"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    whereClause: "Name = 'Default' AND Version = 1"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}