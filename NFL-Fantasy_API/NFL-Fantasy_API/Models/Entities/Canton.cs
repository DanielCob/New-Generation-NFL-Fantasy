// Models/Entities/Canton.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class Canton
    {
        [Key]
        public int CantonID { get; set; }

        [Required]
        public int ProvinceID { get; set; }

        [Required]
        [StringLength(100)]
        public string CantonName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ProvinceID")]
        public virtual Province Province { get; set; } = null!;
        public virtual ICollection<District> Districts { get; set; } = new List<District>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}