using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Scoring
{
    /// <summary>
    /// Entidad que refleja la tabla scoring.ScoringRule
    /// Define cómo se puntúa cada métrica: PASS_YDS, PASS_TD, RUSH_YDS, etc.
    /// </summary>
    [Table("ScoringRule", Schema = "scoring")]
    public class ScoringRule
    {
        [Required]
        public int ScoringSchemaID { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetricCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(9,4)")]
        public decimal? PointsPerUnit { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public int? UnitValue { get; set; }

        [Column(TypeName = "decimal(9,4)")]
        public decimal? FlatPoints { get; set; }

        // Navigation
        [ForeignKey("ScoringSchemaID")]
        public virtual ScoringSchema? ScoringSchema { get; set; }
    }
}