using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// DTO para actualización de branding de equipo fantasy
    /// Feature 3.1 - Editar branding de equipo
    /// </summary>
    public class UpdateTeamBrandingDTO
    {
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
        public string? TeamName { get; set; }

        [StringLength(400)]
        public string? TeamImageUrl { get; set; }

        [Range(300, 1024)]
        public short? TeamImageWidth { get; set; }

        [Range(300, 1024)]
        public short? TeamImageHeight { get; set; }

        [Range(1, 5242880)]
        public int? TeamImageBytes { get; set; }

        [StringLength(400)]
        public string? ThumbnailUrl { get; set; }

        [Range(300, 1024)]
        public short? ThumbnailWidth { get; set; }

        [Range(300, 1024)]
        public short? ThumbnailHeight { get; set; }

        [Range(1, 5242880)]
        public int? ThumbnailBytes { get; set; }
    }

    /// <summary>
    /// Respuesta completa de "Ver mi equipo"
    /// Feature 3.1 - Ver mi equipo
    /// </summary>
    public class MyTeamResponseDTO
    {
        // Información del equipo
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public byte LeagueStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Roster de jugadores
        public List<RosterPlayerDTO> Roster { get; set; } = new();

        // Distribución porcentual
        public List<RosterDistributionItemDTO> Distribution { get; set; } = new();
    }

    /// <summary>
    /// Jugador en roster del equipo
    /// </summary>
    public class RosterPlayerDTO
    {
        public int RosterID { get; set; }
        public int PlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? NFLTeamName { get; set; }
        public string? InjuryStatus { get; set; }
        public string? PhotoUrl { get; set; }
        public string AcquisitionType { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; }
        public bool IsOnRoster { get; set; }
    }

    /// <summary>
    /// Item de distribución de roster
    /// Feature 3.1 - Distribución porcentual
    /// </summary>
    public class RosterDistributionItemDTO
    {
        public string AcquisitionType { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public int TotalPlayers { get; set; }
        public decimal Percentage { get; set; }
    }
}