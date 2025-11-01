namespace NFL_Fantasy_API.Models.ViewModels.NflDetails
{
    /// <summary>
    /// Mapea vw_NFLGames
    /// Vista: Partidos NFL con información de equipos
    /// </summary>
    public class NFLGameVM
    {
        public int NFLGameID { get; set; }
        public int SeasonID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public byte Week { get; set; }
        public int HomeTeamID { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string HomeTeamCity { get; set; } = string.Empty;
        public string? HomeTeamLogo { get; set; }
        public int AwayTeamID { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string AwayTeamCity { get; set; } = string.Empty;
        public string? AwayTeamLogo { get; set; }
        public DateTime GameDate { get; set; }
        public TimeSpan? GameTime { get; set; }
        public string GameStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_NFLTeamSchedule
    /// Vista: Calendario de un equipo NFL
    /// </summary>
    public class NFLTeamScheduleVM
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int NFLGameID { get; set; }
        public int SeasonID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public byte Week { get; set; }
        public DateTime GameDate { get; set; }
        public TimeSpan? GameTime { get; set; }
        public string GameStatus { get; set; } = string.Empty;
        public string HomeAway { get; set; } = string.Empty;
        public string OpponentName { get; set; } = string.Empty;
        public string OpponentCity { get; set; } = string.Empty;
    }
}