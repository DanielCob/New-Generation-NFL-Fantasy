using NFL_Fantasy_API.Models.Entities.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.NFLPlayerNews
    /// Noticias y designaciones de jugadores NFL
    /// Feature 10.3: Estado de Jugador
    /// </summary>
    [Table("NFLPlayerNews", Schema = "ref")]
    public class NFLPlayerNews
    {
        [Key]
        public long NewsID { get; set; }

        [Required]
        public int NFLPlayerID { get; set; }

        [Required]
        [MaxLength(300)]
        public string NewsText { get; set; } = string.Empty;

        [Required]
        public bool IsInjury { get; set; }

        [MaxLength(30)]
        public string? InjurySummary { get; set; }

        [MaxLength(10)]
        public string? Designation { get; set; }

        [Required]
        public int CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public int? DeletedByUserID { get; set; }

        public DateTime? DeletedAt { get; set; }

        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        // Navigation Properties
        [ForeignKey("NFLPlayerID")]
        public virtual NFLPlayer NFLPlayer { get; set; } = null!;

        [ForeignKey("CreatedByUserID")]
        public virtual UserAccount CreatedBy { get; set; } = null!;

        [ForeignKey("DeletedByUserID")]
        public virtual UserAccount? DeletedBy { get; set; }
    }
}