namespace NFL_Fantasy_API.Models.ViewModels
{
    /// <summary>
    /// Mapea el resultado de sp_GetAuditLogs
    /// Log de auditoría con información del actor
    /// </summary>
    public class AuditLogVM
    {
        public long ActionLogID { get; set; }
        public int? ActorUserID { get; set; }
        public string? ActorName { get; set; }
        public string? ActorEmail { get; set; }
        public int? ImpersonatedByUserID { get; set; }
        public string? ImpersonatedByName { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityID { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; }
        public string? SourceIp { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Historial de auditoría de un usuario específico
    /// </summary>
    public class UserAuditHistoryVM
    {
        public long ActionLogID { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityID { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; }
        public string? SourceIp { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Estadísticas de auditoría
    /// </summary>
    public class AuditStatsVM
    {
        public List<EntityTypeStatVM> ActionsByEntity { get; set; } = new();
        public List<ActionCodeStatVM> ActionsByCode { get; set; } = new();
        public List<TopUserStatVM> TopUsers { get; set; } = new();
    }

    public class EntityTypeStatVM
    {
        public string EntityType { get; set; } = string.Empty;
        public int ActionCount { get; set; }
    }

    public class ActionCodeStatVM
    {
        public string ActionCode { get; set; } = string.Empty;
        public int ActionCount { get; set; }
    }

    public class TopUserStatVM
    {
        public int ActorUserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ActionCount { get; set; }
    }

    /// <summary>
    /// Resultado de limpieza de sesiones
    /// </summary>
    public class CleanupResultVM
    {
        public int DeletedSessions { get; set; }
        public int DeletedResetTokens { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}