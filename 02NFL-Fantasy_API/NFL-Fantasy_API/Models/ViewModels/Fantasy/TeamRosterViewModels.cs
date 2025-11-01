namespace NFL_Fantasy_API.Models.ViewModels.Fantasy
{
    /// <summary>
    /// Mapea vw_FantasyTeamDetails
    /// Vista: Detalles completos de equipo fantasy con información del manager
    /// </summary>
    public class FantasyTeamDetailsVM
    {
        public int TeamID { get; set; }
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public byte LeagueStatus { get; set; }
        public int OwnerUserID { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
        public string? ManagerAlias { get; set; }
        public string ManagerSystemRoleCode { get; set; } = "USER";
        public string? ManagerProfileImage { get; set; } // NUEVO
        public string TeamName { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public short? TeamImageWidth { get; set; }
        public short? TeamImageHeight { get; set; }
        public int? TeamImageBytes { get; set; }
        public string? ThumbnailUrl { get; set; }
        public short? ThumbnailWidth { get; set; }
        public short? ThumbnailHeight { get; set; }
        public int? ThumbnailBytes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int RosterCount { get; set; }
        public int DraftedCount { get; set; }
        public int TradedCount { get; set; }
        public int FreeAgentCount { get; set; }
        public int WaiverCount { get; set; }
    }

    /// <summary>
    /// Mapea vw_TeamRoster
    /// Vista: Roster completo de equipos con información de jugadores
    /// </summary>
    public class TeamRosterVM
    {
        public int RosterID { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? NFLTeamID { get; set; }
        public string? NFLTeamName { get; set; }
        public string? NFLTeamCity { get; set; }
        public string? NFLTeamLogo { get; set; }
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public string AcquisitionType { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; }
        public bool IsOnRoster { get; set; }
        public DateTime? DroppedDate { get; set; }
        public int? AddedByUserID { get; set; }
        public string? AddedByName { get; set; }
    }

    /// <summary>
    /// Mapea vw_TeamRosterActive
    /// Vista: Solo jugadores activos en roster
    /// </summary>
    public class TeamRosterActiveVM
    {
        public int RosterID { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int LeagueID { get; set; }
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? NFLTeamName { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public string AcquisitionType { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; }
    }

    /// <summary>
    /// Mapea vw_TeamRosterByPosition
    /// Vista: Roster organizado por posición
    /// </summary>
    public class TeamRosterByPositionVM
    {
        public int RosterID { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int PlayerID { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? NFLTeamName { get; set; }
        public string? InjuryStatus { get; set; }
        public string AcquisitionType { get; set; } = string.Empty;
        public int PositionOrder { get; set; }
    }

    /// <summary>
    /// Mapea vw_TeamRosterDistribution
    /// Vista: Distribución porcentual de adquisición
    /// </summary>
    public class TeamRosterDistributionVM
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int LeagueID { get; set; }
        public string AcquisitionType { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public decimal Percentage { get; set; }
    }
}
