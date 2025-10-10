using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.Team
    /// Equipos de fantasy dentro de una liga
    /// </summary>
    [Table("Team", Schema = "league")]
    public class Team
    {
        [Key]
        public int TeamID { get; set; }

        [Required]
        public int LeagueID { get; set; }

        [Required]
        public int OwnerUserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("LeagueID")]
        public virtual League? League { get; set; }

        [ForeignKey("OwnerUserID")]
        public virtual Auth.UserAccount? Owner { get; set; }
    }
}