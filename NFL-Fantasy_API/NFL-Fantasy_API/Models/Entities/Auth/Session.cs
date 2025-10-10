using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    /// <summary>
    /// Entidad que refleja la tabla auth.Session
    /// Almacena sesiones activas de usuarios (Bearer token = SessionID)
    /// </summary>
    [Table("Session", Schema = "auth")]
    public class Session
    {
        [Key]
        public Guid SessionID { get; set; }

        [Required]
        public int UserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } // CreatedAt + 12h (sliding)

        public bool IsValid { get; set; } = true;

        [MaxLength(45)]
        public string? Ip { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        // Navigation
        [ForeignKey("UserID")]
        public virtual UserAccount? User { get; set; }
    }
}