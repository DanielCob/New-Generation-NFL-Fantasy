namespace NFL_Fantasy_API.Models.ViewModels.Fantasy
{
    /// <summary>
    /// Mapea vw_ScoringSchemas
    /// Vista: esquemas de puntuación disponibles (Default, PrioridadCarrera, MaxPuntos, PrioridadDefensa)
    /// </summary>
    public class ScoringSchemaVM
    {
        public int ScoringSchemaID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsTemplate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_ScoringSchemaRules
    /// Vista: reglas de puntuación de un esquema específico
    /// Ejemplo: PASS_YDS = 1 punto cada 25 yardas, PASS_TD = 4 puntos flat, etc.
    /// </summary>
    public class ScoringSchemaRuleVM
    {
        public int ScoringSchemaID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
        public string MetricCode { get; set; } = string.Empty;
        public decimal? PointsPerUnit { get; set; }
        public string? Unit { get; set; }
        public int? UnitValue { get; set; }
        public decimal? FlatPoints { get; set; }
    }
}