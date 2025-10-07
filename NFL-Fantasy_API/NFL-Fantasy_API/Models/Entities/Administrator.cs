// Models/Entities/Administrator.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class Administrator
    {
        [Key]
        public int AdminID { get; set; }

        [Required]
        public int UserID { get; set; }

        [StringLength(500)]
        public string? Detail { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}