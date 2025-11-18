namespace NFL_Fantasy_API.Models.ViewModels.Fantasy
{
    /// <summary>
    /// Mapea vw_LeagueSummary
    /// Vista: resumen completo de una liga con todos sus datos configurados
    /// ⭐ ACTUALIZADO: Solo retorna LeaguePublicID (no LeagueID privado)
    /// </summary>
    public class LeagueSummaryVM
    {
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public int LeaguePublicID { get; set; }  // ⭐ ÚNICO ID PÚBLICO
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int TeamsCount { get; set; }
        public int AvailableSlots { get; set; }
        public byte PlayoffTeams { get; set; }
        public bool AllowDecimals { get; set; }
        public bool TradeDeadlineEnabled { get; set; }
        public DateTime? TradeDeadlineDate { get; set; }
        public int? MaxRosterChangesPerTeam { get; set; }
        public int? MaxFreeAgentAddsPerTeam { get; set; }

        // Formato de posiciones
        public int PositionFormatID { get; set; }
        public string PositionFormatName { get; set; } = string.Empty;

        // Esquema de puntuación
        public int ScoringSchemaID { get; set; }
        public string ScoringSchemaName { get; set; } = string.Empty;
        public int ScoringVersion { get; set; }

        // Temporada
        public int SeasonID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Creador
        public int CreatedByUserID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string CreatedBySystemRoleCode { get; set; } = "USER";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_LeagueDirectory
    /// Vista: directorio público/listado de ligas disponibles
    /// ⭐ ACTUALIZADO: Solo retorna LeaguePublicID (no LeagueID privado)
    /// </summary>
    public class LeagueDirectoryVM
    {
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public int LeaguePublicID { get; set; }  // ⭐ ÚNICO ID PÚBLICO
        public string SeasonLabel { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int TeamsCount { get; set; }
        public int AvailableSlots { get; set; }
        public int CreatedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_LeagueMembers
    /// Vista: miembros de una liga con sus roles (tanto de liga como del sistema)
    /// ⭐ NOTA: No expone LeagueID (se infiere del contexto de la petición)
    /// </summary>
    public class LeagueMemberVM
    {
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public int UserID { get; set; }
        public string LeagueRoleCode { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserAlias { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
        public string SystemRoleDisplay { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// Mapea vw_LeagueTeams
    /// Vista: equipos dentro de una liga con información del owner
    /// ⭐ NOTA: No expone LeagueID (se infiere del contexto de la petición)
    /// </summary>
    public class LeagueTeamVM
    {
        public int TeamID { get; set; }
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int OwnerUserID { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerSystemRoleCode { get; set; } = "USER";
        public string? OwnerProfileImage { get; set; }
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int RosterCount { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserCommissionedLeagues
    /// Vista: ligas donde el usuario es comisionado (principal o co-comisionado)
    /// ⭐ ACTUALIZADO: Retorna LeaguePublicID en lugar de LeagueID
    /// </summary>
    public class UserCommissionedLeagueVM
    {
        public int UserID { get; set; }
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public int LeaguePublicID { get; set; }  // ⭐ NUEVO
        public string LeagueName { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int AvailableSlots { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public DateTime LeagueCreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserTeams
    /// Vista: equipos del usuario en todas sus ligas
    /// ⭐ ACTUALIZADO: Retorna LeaguePublicID en lugar de LeagueID
    /// </summary>
    public class UserTeamVM
    {
        public int UserID { get; set; }
        public int TeamID { get; set; }
        // ⭐ ELIMINADO: public int LeagueID { get; set; }
        public int LeaguePublicID { get; set; }  // ⭐ NUEVO
        public string LeagueName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public int RosterCount { get; set; }
        public DateTime TeamCreatedAt { get; set; }
        public byte LeagueStatus { get; set; }
    }
}