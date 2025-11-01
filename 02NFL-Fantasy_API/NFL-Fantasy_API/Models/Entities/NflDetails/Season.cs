using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla league.Season
    /// Temporadas NFL (solo una puede ser IsCurrent=1 al mismo tiempo)
    /// </summary>
    [Table("Season", Schema = "league")]
    public class Season
    {
        [Key]
        public int SeasonID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Label { get; set; } = string.Empty; // "NFL 2025"

        [Required]
        public int Year { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCurrent { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}