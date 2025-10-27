using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.Team
    /// Equipos de fantasy dentro de una liga
    /// ACTUALIZADA: Incluye campos de imagen y branding
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
        [MaxLength(100)] // Expandido de 50 a 100
        public string TeamName { get; set; } = string.Empty;

        // NUEVOS CAMPOS: Imagen del equipo
        [MaxLength(400)]
        public string? TeamImageUrl { get; set; }

        public short? TeamImageWidth { get; set; } // 300-1024

        public short? TeamImageHeight { get; set; } // 300-1024

        public int? TeamImageBytes { get; set; } // <= 5MB

        // NUEVOS CAMPOS: Thumbnail
        [MaxLength(400)]
        public string? ThumbnailUrl { get; set; }

        public short? ThumbnailWidth { get; set; }

        public short? ThumbnailHeight { get; set; }

        public int? ThumbnailBytes { get; set; }

        // NUEVO CAMPO: Estado
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NUEVO CAMPO: UpdatedAt
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("LeagueID")]
        public virtual League? League { get; set; }

        [ForeignKey("OwnerUserID")]
        public virtual Auth.UserAccount? Owner { get; set; }
    }
}
