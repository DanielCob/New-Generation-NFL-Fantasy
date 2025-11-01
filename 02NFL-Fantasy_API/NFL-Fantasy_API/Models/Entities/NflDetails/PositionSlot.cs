using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFL_Fantasy_API.Models.Entities.NflDetails
{
    /// <summary>
    /// Entidad que refleja la tabla ref.PositionSlot
    /// Define cuántos slots tiene cada posición en un formato
    /// Ejemplo: Default tiene 1 QB, 2 RB, 2 WR, etc.
    /// </summary>
    [Table("PositionSlot", Schema = "ref")]
    public class PositionSlot
    {
        [Required]
        public int PositionFormatID { get; set; }

        [Required]
        [MaxLength(20)]
        public string PositionCode { get; set; } = string.Empty;

        [Required]
        public byte SlotCount { get; set; }

        // Navigation
        [ForeignKey("PositionFormatID")]
        public virtual PositionFormat? PositionFormat { get; set; }
    }
}