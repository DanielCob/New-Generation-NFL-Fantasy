using NFL_Fantasy_API.Models.Entities.Auth;
using NFL_Fantasy_API.Models.Entities.NflDetails;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Audit
{
    /// <summary>
    /// Entidad que refleja la tabla ref.NFLTeamChangeLog
    /// Auditoría de cambios en equipos NFL
    /// </summary>
    [Table("NFLTeamChangeLog", Schema = "ref")]
    public class NFLTeamChangeLog
    {
        [Key]
        public long ChangeID { get; set; }

        [Required]
        public int NFLTeamID { get; set; }

        [Required]
        public int ChangedByUserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? OldValue { get; set; }

        [MaxLength(1000)]
        public string? NewValue { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        // Navigation
        [ForeignKey("NFLTeamID")]
        public virtual NFLTeam? NFLTeam { get; set; }

        [ForeignKey("ChangedByUserID")]
        public virtual UserAccount? ChangedBy { get; set; }
    }
}
