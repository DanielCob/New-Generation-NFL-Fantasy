using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.Auth
{
    [Table("SystemRoleChangeLog", Schema = "auth")]
    public class SystemRoleChangeLog
    {
        [Key] public long ChangeID { get; set; }
        public int UserID { get; set; }
        public int ChangedByUserID { get; set; }
        [MaxLength(20)] public string OldRoleCode { get; set; } = string.Empty;
        [MaxLength(20)] public string NewRoleCode { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(300)] public string? Reason { get; set; }
        [MaxLength(45)] public string? SourceIp { get; set; }
    }
}
