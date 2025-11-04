namespace NFL_Fantasy_API.Models.ViewModels.NflDetails
{
    /// <summary>
    /// Mapea vw_PositionFormats
    /// Vista: formatos de posiciones disponibles (Default, Extremo, Detallado, Ofensivo)
    /// </summary>
    public class PositionFormatVM
    {
        /// <summary>
        /// ID único del formato de posiciones
        /// </summary>
        public int PositionFormatID { get; set; }

        /// <summary>
        /// Nombre del formato (ej: "Default", "Extremo")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del formato (opcional)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Fecha de creación del formato
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_PositionFormatSlots
    /// Vista: slots/plazas de cada posición dentro de un formato
    /// Ejemplo: Default tiene 1 QB, 2 RB, 2 WR, etc.
    /// </summary>
    public class PositionFormatSlotVM
    {
        /// <summary>
        /// ID del formato al que pertenece este slot
        /// </summary>
        public int PositionFormatID { get; set; }

        /// <summary>
        /// Nombre del formato (ej: "Default")
        /// </summary>
        public string FormatName { get; set; } = string.Empty;

        /// <summary>
        /// Código de la posición (QB, RB, WR, TE, RB/WR, K, DEF, BENCH, IR)
        /// </summary>
        public string PositionCode { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad de slots para esta posición
        /// </summary>
        public byte SlotCount { get; set; }

        /// <summary>
        /// Indica si esta posición permite acumular puntos.
        /// true = Posición activa (cuenta para scoring)
        /// false = Posición inactiva (BENCH, IR)
        /// </summary>
        public bool PointsAllowed { get; set; }
    }
}