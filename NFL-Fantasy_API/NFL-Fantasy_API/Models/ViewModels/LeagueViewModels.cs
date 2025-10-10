namespace NFL_Fantasy_API.Models.ViewModels
{
    /// <summary>
    /// Mapea vw_LeagueSummary
    /// Vista: resumen completo de una liga con todos sus datos configurados
    /// </summary>
    public class LeagueSummaryVM
    {
        public int LeagueID { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_LeagueDirectory
    /// Vista: directorio público/listado de ligas disponibles
    /// </summary>
    public class LeagueDirectoryVM
    {
        public int LeagueID { get; set; }
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
    /// Vista: miembros de una liga con sus roles
    /// </summary>
    public class LeagueMemberVM
    {
        public int LeagueID { get; set; }
        public int UserID { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public bool IsPrimaryCommissioner { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mapea vw_LeagueTeams
    /// Vista: equipos dentro de una liga
    /// </summary>
    public class LeagueTeamVM
    {
        public int TeamID { get; set; }
        public int LeagueID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int OwnerUserID { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserCommissionedLeagues
    /// Vista: ligas donde el usuario es comisionado (principal o co-comisionado)
    /// </summary>
    public class UserCommissionedLeagueVM
    {
        public int UserID { get; set; }
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int AvailableSlots { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public bool IsPrimaryCommissioner { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime LeagueCreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserTeams
    /// Vista: equipos del usuario en todas sus ligas
    /// </summary>
    public class UserTeamVM
    {
        public int UserID { get; set; }
        public int TeamID { get; set; }
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public DateTime TeamCreatedAt { get; set; }
        public byte LeagueStatus { get; set; }
    }
}