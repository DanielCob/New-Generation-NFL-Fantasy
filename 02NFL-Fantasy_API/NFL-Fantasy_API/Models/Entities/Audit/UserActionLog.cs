using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Audit
{
    /// <summary>
    /// Entidad que refleja la tabla audit.UserActionLog
    /// Log auditable de todas las acciones de usuarios (CRUD, LOGIN, LOGOUT, etc.)
    /// Retención mínima: 12 meses
    /// </summary>
    [Table("UserActionLog", Schema = "audit")]
    public class UserActionLog
    {
        [Key]
        public long ActionLogID { get; set; }

        public int? ActorUserID { get; set; } // NULL si público/no autenticado

        public int? ImpersonatedByUserID { get; set; } // Si aplica suplantación

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // USER_PROFILE, LEAGUE, TEAM, etc.

        [Required]
        [MaxLength(50)]
        public string EntityID { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ActionCode { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, etc.

        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        public string? Details { get; set; } // NVARCHAR(MAX)

        // Navigation
        [ForeignKey("ActorUserID")]
        public virtual Auth.UserAccount? Actor { get; set; }

        [ForeignKey("ImpersonatedByUserID")]
        public virtual Auth.UserAccount? Impersonator { get; set; }
    }
}