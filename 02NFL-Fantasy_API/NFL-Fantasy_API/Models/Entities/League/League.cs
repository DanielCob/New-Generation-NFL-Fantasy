using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.League
    /// Ligas de fantasy creadas por usuarios
    /// </summary>
    [Table("League", Schema = "league")]
    public class League
    {
        [Key]
        public int LeagueID { get; set; }

        [Required]
        public int SeasonID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Capacidad
        [Required]
        public byte TeamSlots { get; set; } // 4,6,8,10,12,14,16,18,20

        // Seguridad de liga
        [Required]
        public byte[] LeaguePasswordHash { get; set; } = Array.Empty<byte>(); // VARBINARY(64)

        [Required]
        public byte[] LeaguePasswordSalt { get; set; } = Array.Empty<byte>(); // VARBINARY(16)

        // Estados y reglas
        public byte Status { get; set; } = 0; // 0=PreDraft, 1=Active, 2=Inactive, 3=Closed

        public bool AllowDecimals { get; set; } = true;

        public byte PlayoffTeams { get; set; } = 4; // 4 o 6

        // Trade deadline
        public bool TradeDeadlineEnabled { get; set; } = false;

        public DateTime? TradeDeadlineDate { get; set; }

        // Límites de movimientos (NULL = sin límite)
        public int? MaxRosterChangesPerTeam { get; set; }

        public int? MaxFreeAgentAddsPerTeam { get; set; }

        // Defaults asignados
        [Required]
        public int PositionFormatID { get; set; }

        [Required]
        public int ScoringSchemaID { get; set; }

        // Metadata
        [Required]
        public int CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation (no usadas en este proyecto sin EF, solo documentación)
        [ForeignKey("SeasonID")]
        public virtual Season? Season { get; set; }

        [ForeignKey("CreatedByUserID")]
        public virtual Auth.UserAccount? Creator { get; set; }
    }
}