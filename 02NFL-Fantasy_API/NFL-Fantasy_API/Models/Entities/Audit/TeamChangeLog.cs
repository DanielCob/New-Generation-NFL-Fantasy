using NFL_Fantasy_API.Models.Entities.Auth;
using NFL_Fantasy_API.Models.Entities.Fantasy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Audit
{
    /// <summary>
    /// Entidad que refleja la tabla league.TeamChangeLog
    /// Auditoría de cambios en equipos fantasy
    /// </summary>
    [Table("TeamChangeLog", Schema = "league")]
    public class TeamChangeLog
    {
        [Key]
        public long ChangeID { get; set; }

        [Required]
        public int TeamID { get; set; }

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
        [ForeignKey("TeamID")]
        public virtual Team? Team { get; set; }

        [ForeignKey("ChangedByUserID")]
        public virtual UserAccount? ChangedBy { get; set; }
    }
}