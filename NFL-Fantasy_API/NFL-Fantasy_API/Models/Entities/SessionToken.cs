// Models/Entities/SessionToken.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class SessionToken
    {
        [Key]
        public int TokenID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public Guid Token { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}