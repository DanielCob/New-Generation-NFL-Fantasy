namespace NFL_Fantasy_API.Models.ViewModels
{
    /// <summary>
    /// Mapea vw_Players
    /// Vista: Jugadores NFL con información de equipo
    /// </summary>
    public class PlayerVM
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? NFLTeamID { get; set; }
        public string? NFLTeamName { get; set; }
        public string? NFLTeamCity { get; set; }
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOnFantasyRoster { get; set; }
    }

    /// <summary>
    /// Mapea vw_AvailablePlayers
    /// Vista: Jugadores disponibles (no en roster)
    /// </summary>
    public class AvailablePlayerVM
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? NFLTeamID { get; set; }
        public string? NFLTeamName { get; set; }
        public string? NFLTeamCity { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Mapea vw_PlayersByNFLTeam
    /// Vista: Jugadores agrupados por equipo NFL
    /// </summary>
    public class PlayersByNFLTeamVM
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public bool PlayerIsActive { get; set; }
    }
}
