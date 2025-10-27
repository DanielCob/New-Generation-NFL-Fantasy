namespace NFL_Fantasy_API.Models.ViewModels
{
    /// <summary>
    /// Mapea vw_PositionFormats
    /// Vista: formatos de posiciones disponibles (Default, Extremo, Detallado, Ofensivo)
    /// </summary>
    public class PositionFormatVM
    {
        public int PositionFormatID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_PositionFormatSlots
    /// Vista: slots/plazas de cada posición dentro de un formato
    /// Ejemplo: Default tiene 1 QB, 2 RB, 2 WR, etc.
    /// </summary>
    public class PositionFormatSlotVM
    {
        public int PositionFormatID { get; set; }
        public string FormatName { get; set; } = string.Empty;
        public string PositionCode { get; set; } = string.Empty;
        public byte SlotCount { get; set; }
    }
}