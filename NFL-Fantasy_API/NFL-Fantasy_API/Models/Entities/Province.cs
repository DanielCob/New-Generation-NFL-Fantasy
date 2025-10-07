// Models/Entities/Province.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities
{
    public class Province
    {
        [Key]
        public int ProvinceID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProvinceName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Canton> Cantons { get; set; } = new List<Canton>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}