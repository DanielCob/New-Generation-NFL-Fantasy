using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Fantasy
{
    /// <summary>
    /// DTO para agregar jugador a roster
    /// Feature 3.1 - Gestión de roster
    /// </summary>
    public class AddPlayerToRosterDTO
    {
        [Required(ErrorMessage = "El ID del jugador es obligatorio.")]
        public int PlayerID { get; set; }

        [Required(ErrorMessage = "El tipo de adquisición es obligatorio.")]
        [StringLength(20)]
        [RegularExpression("^(Draft|Trade|FreeAgent|Waiver)$",
            ErrorMessage = "AcquisitionType debe ser: Draft, Trade, FreeAgent o Waiver.")]
        public string AcquisitionType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de agregar jugador a roster
    /// </summary>
    public class AddPlayerToRosterResponseDTO
    {
        public long RosterID { get; set; }
        public string Message { get; set; } = "Jugador agregado al roster exitosamente.";
    }
}
