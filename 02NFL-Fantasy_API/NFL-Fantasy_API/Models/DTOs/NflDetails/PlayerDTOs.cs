namespace NFL_Fantasy_API.Models.DTOs.NflDetails
{
    /// <summary>
    /// Jugador NFL básico (para listados)
    /// </summary>
    public class PlayerBasicDTO
    {
        public int NFLPlayerID { get; set; }  // CAMBIO: PlayerID → NFLPlayerID
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }  // CAMBIO: int? → int (required)
        public string? NFLTeamName { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Jugador disponible (no en roster)
    /// </summary>
    public class AvailablePlayerDTO
    {
        public int NFLPlayerID { get; set; }  // CAMBIO: PlayerID → NFLPlayerID
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? NFLTeamName { get; set; }
        public string? NFLTeamCity { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
    }
}