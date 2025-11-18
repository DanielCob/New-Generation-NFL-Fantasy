using NFL_Fantasy_API.Models.Entities.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.NFLPlayerBatchReport
    /// Reportes de importaciones batch de jugadores NFL
    /// </summary>
    [Table("NFLPlayerBatchReport", Schema = "ref")]
    public class NFLPlayerBatchReport
    {
        [Key]
        public int BatchReportID { get; set; }

        [Required]
        [MaxLength(400)]
        public string ReportUrl { get; set; } = string.Empty;

        [Required]
        public int TotalProcessed { get; set; }

        [Required]
        public int SuccessCount { get; set; }

        [Required]
        public int ErrorCount { get; set; }

        [Required]
        public int ActorUserID { get; set; }

        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [MaxLength(300)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ActorUserID")]
        public virtual UserAccount Actor { get; set; } = null!;
    }
}