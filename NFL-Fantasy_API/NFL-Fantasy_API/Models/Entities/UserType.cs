// Models/Entities/UserType.cs
using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.Entities
{
    public class UserType
    {
        [Key]
        public int UserTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; } = string.Empty; // 'CLIENT', 'ENGINEER', 'ADMIN'

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}