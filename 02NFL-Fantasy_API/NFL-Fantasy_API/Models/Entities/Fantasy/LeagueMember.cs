using NFL_Fantasy_API.Models.Entities.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Fantasy
{
    /// <summary>
    /// Entidad que refleja la tabla league.LeagueMember
    /// Relación usuario-liga con roles (COMMISSIONER, CO_COMMISSIONER, MANAGER, SPECTATOR)
    /// </summary>
    [Table("LeagueMember", Schema = "league")]
    public class LeagueMember
    {
        [Required]
        public int LeagueID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoleCode { get; set; } = string.Empty;

        public bool IsPrimaryCommissioner { get; set; } = false;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }

        // Navigation
        [ForeignKey("LeagueID")]
        public virtual League? League { get; set; }

        [ForeignKey("UserID")]
        public virtual UserAccount? User { get; set; }
    }
}