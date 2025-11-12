using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.PositionFormat
    /// Formatos de posiciones: Default, Extremo, Detallado, Ofensivo
    /// </summary>
    [Table("PositionFormat", Schema = "ref")]
    public class PositionFormat
    {
        [Key]
        public int PositionFormatID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}