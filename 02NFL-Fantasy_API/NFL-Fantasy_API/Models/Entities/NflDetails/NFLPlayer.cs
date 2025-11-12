using NFL_Fantasy_API.Models.Entities.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.NFLPlayer
    /// Jugadores oficiales de la NFL
    /// </summary>
    [Table("NFLPlayer", Schema = "ref")]
    public class NFLPlayer
    {
        [Key]
        public int NFLPlayerID { get; set; }

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

        [Required]
        public int NFLTeamID { get; set; }

        [MaxLength(50)]
        public string? InjuryStatus { get; set; }

        [MaxLength(300)]
        public string? InjuryDescription { get; set; }

        // Foto principal del jugador
        [MaxLength(400)]
        public string? PhotoUrl { get; set; }

        public short? PhotoWidth { get; set; } // 300-1024

        public short? PhotoHeight { get; set; } // 300-1024

        public int? PhotoBytes { get; set; } // <= 5MB

        // Thumbnail
        [MaxLength(400)]
        public string? PhotoThumbnailUrl { get; set; }

        public short? ThumbnailWidth { get; set; }

        public short? ThumbnailHeight { get; set; }

        public int? ThumbnailBytes { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedByUserID { get; set; }

        public int? UpdatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("NFLTeamID")]
        public virtual NFLTeam NFLTeam { get; set; } = null!;

        [ForeignKey("CreatedByUserID")]
        public virtual UserAccount? CreatedBy { get; set; }

        [ForeignKey("UpdatedByUserID")]
        public virtual UserAccount? UpdatedBy { get; set; }
    }
}