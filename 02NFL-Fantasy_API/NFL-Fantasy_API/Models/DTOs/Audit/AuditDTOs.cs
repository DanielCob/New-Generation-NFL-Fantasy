using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Audit
{
    /// <summary>
    /// DTO para filtrar logs de auditoría
    /// </summary>
    public class AuditLogFilterDTO
    {
        [StringLength(50)]
        public string? EntityType { get; set; }

        [StringLength(50)]
        public string? EntityID { get; set; }

        public int? ActorUserID { get; set; }

        [StringLength(50)]
        public string? ActionCode { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 1000, ErrorMessage = "Top debe estar entre 1 y 1000.")]
        public int Top { get; set; } = 100;
    }

    /// <summary>
    /// DTO para solicitar estadísticas de auditoría
    /// </summary>
    public class AuditStatsRequestDTO
    {
        [Range(1, 365, ErrorMessage = "Days debe estar entre 1 y 365.")]
        public int Days { get; set; } = 30;
    }

    /// <summary>
    /// DTO para parámetros de limpieza
    /// </summary>
    public class CleanupRequestDTO
    {
        [Range(1, 180, ErrorMessage = "RetentionDays debe estar entre 1 y 180.")]
        public int RetentionDays { get; set; } = 30;
    }
}