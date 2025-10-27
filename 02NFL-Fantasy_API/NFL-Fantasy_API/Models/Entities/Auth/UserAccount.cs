using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    /// <summary>
    /// Entidad que refleja la tabla auth.UserAccount
    /// Almacena información de usuarios del sistema
    /// </summary>
    [Table("UserAccount", Schema = "auth")]
    public class UserAccount
    {
        [Key]
        public int UserID { get; set; }

        // Identidad / Credenciales
        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>(); // VARBINARY(64)

        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>(); // VARBINARY(16)

        // Perfil visible
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Alias { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = "en";

        [MaxLength(20)]
        public string SystemRoleCode { get; set; } = "USER";
        [ForeignKey("SystemRoleCode")]
        public virtual SystemRole? SystemRole { get; set; }

        // Imagen de perfil
        [MaxLength(400)]
        public string? ProfileImageUrl { get; set; }

        public short? ProfileImageWidth { get; set; } // 300-1024

        public short? ProfileImageHeight { get; set; } // 300-1024

        public int? ProfileImageBytes { get; set; } // <= 5MB

        // Estado de cuenta / Seguridad
        public byte AccountStatus { get; set; } = 1; // 1=Active, 2=Locked, 0=Disabled

        public short FailedLoginCount { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}