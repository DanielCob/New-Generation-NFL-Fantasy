using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    [Table("SystemRole", Schema = "auth")]
    public class SystemRole
    {
        [Key, MaxLength(20)] public string RoleCode { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Display { get; set; } = string.Empty;
        [MaxLength(200)] public string? Description { get; set; }
    }
}
