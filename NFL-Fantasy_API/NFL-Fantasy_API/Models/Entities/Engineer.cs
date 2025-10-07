// Models/Entities/Engineer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class Engineer
    {
        [Key]
        public int EngineerID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(200)]
        public string Career { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Specialization { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}