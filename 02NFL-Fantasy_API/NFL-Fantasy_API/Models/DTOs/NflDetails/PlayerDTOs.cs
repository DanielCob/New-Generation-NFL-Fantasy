namespace NFL_Fantasy_API.Models.DTOs.NflDetails
{
    /// <summary>
    /// Jugador NFL básico (para listados)
    /// </summary>
    public class PlayerBasicDTO
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? NFLTeamID { get; set; }
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
        public int PlayerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? NFLTeamName { get; set; }
        public string? NFLTeamCity { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
    }
}