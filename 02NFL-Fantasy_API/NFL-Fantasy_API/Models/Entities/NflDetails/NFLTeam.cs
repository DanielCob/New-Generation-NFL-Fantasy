using NFL_Fantasy_API.Models.Entities.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.NFLTeam
    /// Equipos de la NFL
    /// </summary>
    [Table("NFLTeam", Schema = "ref")]
    public class NFLTeam
    {
        [Key]
        public int NFLTeamID { get; set; }

        [Required]
        [MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        // Imagen del equipo
        [MaxLength(400)]
        public string? TeamImageUrl { get; set; }

        public short? TeamImageWidth { get; set; } // 300-1024

        public short? TeamImageHeight { get; set; } // 300-1024

        public int? TeamImageBytes { get; set; } // <= 5MB

        // Thumbnail
        [MaxLength(400)]
        public string? ThumbnailUrl { get; set; }

        public short? ThumbnailWidth { get; set; }

        public short? ThumbnailHeight { get; set; }

        public int? ThumbnailBytes { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedByUserID { get; set; }

        public int? UpdatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CreatedByUserID")]
        public virtual UserAccount? CreatedBy { get; set; }

        [ForeignKey("UpdatedByUserID")]
        public virtual UserAccount? UpdatedBy { get; set; }
    }
}
