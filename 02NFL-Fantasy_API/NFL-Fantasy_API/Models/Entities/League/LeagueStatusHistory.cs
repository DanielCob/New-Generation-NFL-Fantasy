using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.LeagueStatusHistory
    /// Auditoría de cambios de estado de ligas
    /// </summary>
    [Table("LeagueStatusHistory", Schema = "league")]
    public class LeagueStatusHistory
    {
        [Key]
        public long StatusHistoryID { get; set; }

        [Required]
        public int LeagueID { get; set; }

        [Required]
        public byte OldStatus { get; set; }

        [Required]
        public byte NewStatus { get; set; }

        [Required]
        public int ChangedByUserID { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(300)]
        public string? Reason { get; set; }

        // Navigation
        [ForeignKey("LeagueID")]
        public virtual League? League { get; set; }

        [ForeignKey("ChangedByUserID")]
        public virtual Auth.UserAccount? ChangedBy { get; set; }
    }
}