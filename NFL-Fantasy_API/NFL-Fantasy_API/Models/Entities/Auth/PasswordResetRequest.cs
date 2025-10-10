using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    /// <summary>
    /// Entidad que refleja la tabla auth.PasswordResetRequest
    /// Tokens de restablecimiento de contraseña con expiración
    /// </summary>
    [Table("PasswordResetRequest", Schema = "auth")]
    public class PasswordResetRequest
    {
        [Key]
        public Guid ResetID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } // RequestedAt + 60 minutos

        public DateTime? UsedAt { get; set; }

        [MaxLength(45)]
        public string? FromIp { get; set; }

        // Navigation
        [ForeignKey("UserID")]
        public virtual UserAccount? User { get; set; }
    }
}