namespace NFL_Fantasy_API.Models.ViewModels.NflDetails
{
    /// <summary>
    /// Mapea vw_NFLPlayers
    /// Vista: Jugadores NFL con información de equipo
    /// </summary>
    public class NFLPlayerVM
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? NFLTeamLogo { get; set; }
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsOnFantasyRoster { get; set; }
        public int RosterCount { get; set; }
    }

    /// <summary>
    /// Mapea vw_ActiveNFLPlayers
    /// Vista: Jugadores NFL activos (para selección en formularios)
    /// </summary>
    public class ActiveNFLPlayerVM
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? PhotoThumbnailUrl { get; set; }
        public string? InjuryStatus { get; set; }
    }

    /// <summary>
    /// Mapea vw_AvailablePlayers
    /// Vista: Jugadores disponibles (no en roster)
    /// </summary>
    public class AvailablePlayerVM
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
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
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public bool PlayerIsActive { get; set; }
    }

    /// <summary>
    /// Mapea vw_NFLPlayerDetails
    /// Vista: Detalles completos de jugador NFL
    /// </summary>
    public class NFLPlayerDetailsVM
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public short? PhotoWidth { get; set; }
        public short? PhotoHeight { get; set; }
        public int? PhotoBytes { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public short? ThumbnailWidth { get; set; }
        public short? ThumbnailHeight { get; set; }
        public int? ThumbnailBytes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByEmail { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
        public string? UpdatedByEmail { get; set; }
    }

    /// <summary>
    /// Mapea vw_NFLPlayersByPosition
    /// Vista: Jugadores agrupados por posición con orden lógico
    /// </summary>
    public class NFLPlayersByPositionVM
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public int PositionOrder { get; set; }
        public bool IsOnFantasyRoster { get; set; }
    }

    /// <summary>
    /// Mapea vw_NFLPlayerAvailability
    /// Vista: Disponibilidad de jugadores por liga/temporada
    /// </summary>
    public class NFLPlayerAvailabilityVM
    {
        public int NFLPlayerID { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public int SeasonID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? CurrentTeamName { get; set; }
    }
}