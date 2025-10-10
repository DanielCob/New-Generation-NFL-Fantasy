using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    /// <summary>
    /// Entidad que refleja la tabla auth.LoginAttempt
    /// Auditoría de intentos de inicio de sesión (exitosos y fallidos)
    /// </summary>
    [Table("LoginAttempt", Schema = "auth")]
    public class LoginAttempt
    {
        [Key]
        public long LoginAttemptID { get; set; }

        public int? UserID { get; set; } // NULL si el email no existe

        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; }

        [MaxLength(45)]
        public string? Ip { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        // Navigation
        [ForeignKey("UserID")]
        public virtual UserAccount? User { get; set; }
    }
}