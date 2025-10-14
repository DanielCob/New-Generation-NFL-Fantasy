using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.TeamRoster
    /// Jugadores en el roster de equipos fantasy
    /// </summary>
    [Table("TeamRoster", Schema = "league")]
    public class TeamRoster
    {
        [Key]
        public int RosterID { get; set; }

        [Required]
        public int TeamID { get; set; }

        [Required]
        public int PlayerID { get; set; }

        [Required]
        [MaxLength(20)]
        public string AcquisitionType { get; set; } = string.Empty; // Draft, Trade, FreeAgent, Waiver

        public DateTime AcquisitionDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime? DroppedDate { get; set; }

        public int? AddedByUserID { get; set; }

        // Navigation
        [ForeignKey("TeamID")]
        public virtual Team? Team { get; set; }

        [ForeignKey("PlayerID")]
        public virtual Player? Player { get; set; }

        [ForeignKey("AddedByUserID")]
        public virtual Auth.UserAccount? AddedBy { get; set; }
    }
}