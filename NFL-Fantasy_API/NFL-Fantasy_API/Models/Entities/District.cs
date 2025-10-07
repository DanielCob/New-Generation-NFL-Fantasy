// Models/Entities/District.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class District
    {
        [Key]
        public int DistrictID { get; set; }

        [Required]
        public int CantonID { get; set; }

        [Required]
        [StringLength(100)]
        public string DistrictName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CantonID")]
        public virtual Canton Canton { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}