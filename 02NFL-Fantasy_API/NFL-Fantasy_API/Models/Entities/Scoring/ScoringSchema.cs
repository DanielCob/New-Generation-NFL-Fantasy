using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Scoring
{
    /// <summary>
    /// Entidad que refleja la tabla scoring.ScoringSchema
    /// Esquemas de puntuación: Default, PrioridadCarrera, MaxPuntos, PrioridadDefensa
    /// </summary>
    [Table("ScoringSchema", Schema = "scoring")]
    public class ScoringSchema
    {
        [Key]
        public int ScoringSchemaID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Version { get; set; } = 1;

        public bool IsTemplate { get; set; } = true;

        [MaxLength(300)]
        public string? Description { get; set; }

        public int? CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CreatedByUserID")]
        public virtual Auth.UserAccount? Creator { get; set; }
    }
}