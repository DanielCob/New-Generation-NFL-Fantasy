// Models/Entities/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastSurname { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SecondSurname { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        public int UserTypeID { get; set; }

        [Required]
        public int ProvinceID { get; set; }

        [Required]
        public int CantonID { get; set; }

        public int? DistrictID { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserTypeID")]
        public virtual UserType UserType { get; set; } = null!;

        [ForeignKey("ProvinceID")]
        public virtual Province Province { get; set; } = null!;

        [ForeignKey("CantonID")]
        public virtual Canton Canton { get; set; } = null!;

        [ForeignKey("DistrictID")]
        public virtual District? District { get; set; }

        public virtual Engineer? Engineer { get; set; }
        public virtual Administrator? Administrator { get; set; }
        public virtual ICollection<SessionToken> SessionTokens { get; set; } = new List<SessionToken>();
    }
}