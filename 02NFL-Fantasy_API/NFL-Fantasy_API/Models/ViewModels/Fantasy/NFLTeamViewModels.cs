namespace NFL_Fantasy_API.Models.ViewModels.Fantasy
{
    /// <summary>
    /// Mapea vw_NFLTeams
    /// Vista: Listado general de equipos NFL
    /// </summary>
    public class NFLTeamVM
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
        public int ActivePlayersCount { get; set; }
    }

    /// <summary>
    /// Mapea vw_NFLTeamDetails
    /// Vista: Detalles completos de equipo NFL
    /// </summary>
    public class NFLTeamDetailsVM
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
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
        public string? CreatedByName { get; set; }
        public string? CreatedByEmail { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }
        public string? UpdatedByEmail { get; set; }
    }

    /// <summary>
    /// Mapea vw_ActiveNFLTeams
    /// Vista: Solo equipos NFL activos (para dropdowns)
    /// </summary>
    public class NFLTeamBasicVM
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}