using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.League
{
    /// <summary>
    /// Entidad que refleja la tabla league.NFLGame
    /// Partidos de la NFL
    /// </summary>
    [Table("NFLGame", Schema = "league")]
    public class NFLGame
    {
        [Key]
        public int NFLGameID { get; set; }

        [Required]
        public int SeasonID { get; set; }

        [Required]
        public byte Week { get; set; }

        [Required]
        public int HomeTeamID { get; set; }

        [Required]
        public int AwayTeamID { get; set; }

        [Required]
        public DateTime GameDate { get; set; }

        public TimeSpan? GameTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string GameStatus { get; set; } = "Scheduled"; // Scheduled, InProgress, Final, Postponed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("SeasonID")]
        public virtual Season? Season { get; set; }

        [ForeignKey("HomeTeamID")]
        public virtual Ref.NFLTeam? HomeTeam { get; set; }

        [ForeignKey("AwayTeamID")]
        public virtual Ref.NFLTeam? AwayTeam { get; set; }
    }
}