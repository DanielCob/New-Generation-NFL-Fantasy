using NFL_Fantasy_API.Models.Entities.Auth;
using NFL_Fantasy_API.Models.Entities.NflDetails;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Fantasy
{
    /// <summary>
    /// Entidad que refleja la tabla league.TeamRoster
    /// Jugadores en el roster de equipos fantasy
    /// </summary>
    [Table("TeamRoster", Schema = "league")]
    public class TeamRoster
    {
        [Key]
        public long RosterID { get; set; }

        [Required]
        public int TeamID { get; set; }

        [Required]
        public int NFLPlayerID { get; set; }  // CORREGIDO

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

        [ForeignKey("NFLPlayerID")]  // CORREGIDO
        public virtual NFLPlayer? NFLPlayer { get; set; }  // CORREGIDO

        [ForeignKey("AddedByUserID")]
        public virtual UserAccount? AddedBy { get; set; }
    }
}