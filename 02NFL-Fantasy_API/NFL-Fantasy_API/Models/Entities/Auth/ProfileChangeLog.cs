using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    /// <summary>
    /// Entidad que refleja la tabla auth.ProfileChangeLog
    /// Historial auditable de cambios en perfiles de usuario
    /// </summary>
    [Table("ProfileChangeLog", Schema = "auth")]
    public class ProfileChangeLog
    {
        [Key]
        public long ChangeID { get; set; }

        [Required]
        public int UserID { get; set; }

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

        // Navigation
        [ForeignKey("UserID")]
        public virtual UserAccount? User { get; set; }

        [ForeignKey("ChangedByUserID")]
        public virtual UserAccount? ChangedByUser { get; set; }
    }
}