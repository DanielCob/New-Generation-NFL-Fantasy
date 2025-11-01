using NFL_Fantasy_API.Models.Entities.Auth;
using NFL_Fantasy_API.Models.Entities.Fantasy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Audit
{
    /// <summary>
    /// Entidad que refleja la tabla league.LeagueConfigHistory
    /// Auditoría de cambios de configuración de ligas
    /// </summary>
    [Table("LeagueConfigHistory", Schema = "league")]
    public class LeagueConfigHistory
    {
        [Key]
        public long ConfigHistoryID { get; set; }

        [Required]
        public int LeagueID { get; set; }

        [Required]
        public int ChangedByUserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? OldValue { get; set; }

        [MaxLength(1000)]
        public string? NewValue { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("LeagueID")]
        public virtual League? League { get; set; }

        [ForeignKey("ChangedByUserID")]
        public virtual UserAccount? ChangedBy { get; set; }
    }
}