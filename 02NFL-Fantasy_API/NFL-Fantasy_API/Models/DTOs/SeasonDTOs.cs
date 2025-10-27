using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    public class CreateSeasonRequestDTO
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required, Range(1, 22)]
        public byte WeekCount { get; set; }

        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }

        public bool MarkAsCurrent { get; set; } = false;
    }

    public class UpdateSeasonRequestDTO
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required, Range(1, 22)]
        public byte WeekCount { get; set; }

        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }

        /// <summary>
        /// NULL = no cambiar; true = marcar actual; false = desmarcar
        /// </summary>
        public bool? SetAsCurrent { get; set; } = null;

        /// <summary>
        /// Requerido si SetAsCurrent = true
        /// </summary>
        public bool ConfirmMakeCurrent { get; set; } = false;
    }

    public class ConfirmActionDTO
    {
        public bool Confirm { get; set; } = false;
    }
}
