using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.Player
    /// Jugadores NFL disponibles para rosters
    /// </summary>
    [Table("Player", Schema = "league")]
    public class Player
    {
        [Key]
        public int PlayerID { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        // Computed column en DB
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [MaxLength(101)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Position { get; set; } = string.Empty;

        public int? NFLTeamID { get; set; }

        [MaxLength(50)]
        public string? InjuryStatus { get; set; }

        [MaxLength(300)]
        public string? InjuryDescription { get; set; }

        [MaxLength(400)]
        public string? PhotoUrl { get; set; }

        [MaxLength(400)]
        public string? PhotoThumbnailUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("NFLTeamID")]
        public virtual Ref.NFLTeam? NFLTeam { get; set; }
    }
}
