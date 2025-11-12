using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.NflDetails
{
    /// <summary>
    /// DTO para creación de jugador NFL
    /// Feature: Gestión de jugadores NFL (CRUD)
    /// </summary>
    public class CreateNFLPlayerDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El apellido debe tener entre 1 y 50 caracteres.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La posición es obligatoria.")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "La posición debe tener entre 1 y 20 caracteres.")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "El equipo NFL es obligatorio.")]
        public int NFLTeamID { get; set; }

        [StringLength(50, ErrorMessage = "El estado de lesión no puede superar 50 caracteres.")]
        public string? InjuryStatus { get; set; }

        [StringLength(300, ErrorMessage = "La descripción de lesión no puede superar 300 caracteres.")]
        public string? InjuryDescription { get; set; }

        // Foto principal
        [StringLength(400, ErrorMessage = "La URL de foto no puede superar 400 caracteres.")]
        public string? PhotoUrl { get; set; }

        public short? PhotoWidth { get; set; }

        public short? PhotoHeight { get; set; }

        public int? PhotoBytes { get; set; }

        // Thumbnail
        [StringLength(400)]
        public string? PhotoThumbnailUrl { get; set; }

        public short? ThumbnailWidth { get; set; }

        public short? ThumbnailHeight { get; set; }

        public int? ThumbnailBytes { get; set; }
    }

    /// <summary>
    /// Respuesta de creación de jugador NFL
    /// </summary>
    public class CreateNFLPlayerResponseDTO
    {
        public int NFLPlayerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Message { get; set; } = "Jugador NFL creado exitosamente.";
    }

    /// <summary>
    /// DTO para actualización de jugador NFL
    /// Feature: Modificar jugador NFL
    /// </summary>
    public class UpdateNFLPlayerDTO
    {
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string? FirstName { get; set; }

        [StringLength(50, MinimumLength = 1, ErrorMessage = "El apellido debe tener entre 1 y 50 caracteres.")]
        public string? LastName { get; set; }

        [StringLength(20, MinimumLength = 1, ErrorMessage = "La posición debe tener entre 1 y 20 caracteres.")]
        public string? Position { get; set; }

        public int? NFLTeamID { get; set; }

        [StringLength(50)]
        public string? InjuryStatus { get; set; }

        [StringLength(300)]
        public string? InjuryDescription { get; set; }

        [StringLength(400)]
        public string? PhotoUrl { get; set; }

        public short? PhotoWidth { get; set; }

        public short? PhotoHeight { get; set; }

        public int? PhotoBytes { get; set; }

        [StringLength(400)]
        public string? PhotoThumbnailUrl { get; set; }

        public short? ThumbnailWidth { get; set; }

        public short? ThumbnailHeight { get; set; }

        public int? ThumbnailBytes { get; set; }
    }

    /// <summary>
    /// DTO para solicitud de listado de jugadores NFL
    /// Feature: Listar jugadores NFL con paginación
    /// </summary>
    public class ListNFLPlayersRequestDTO
    {
        [Range(1, 100, ErrorMessage = "PageNumber debe estar entre 1 y 100.")]
        public int PageNumber { get; set; } = 1;

        [Range(10, 100, ErrorMessage = "PageSize debe estar entre 10 y 100.")]
        public int PageSize { get; set; } = 50;

        [StringLength(100)]
        public string? SearchTerm { get; set; }

        [StringLength(20)]
        public string? FilterPosition { get; set; }

        public int? FilterNFLTeamID { get; set; }

        public bool? FilterIsActive { get; set; }
    }

    /// <summary>
    /// Respuesta de listado de jugadores NFL
    /// </summary>
    public class ListNFLPlayersResponseDTO
    {
        public List<NFLPlayerListItemDTO> Players { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Item de jugador NFL en listado
    /// </summary>
    public class NFLPlayerListItemDTO
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Detalles completos de jugador NFL
    /// Feature: Ver detalles de jugador
    /// </summary>
    public class NFLPlayerDetailsDTO
    {
        // Información del jugador
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string? InjuryStatus { get; set; }
        public string? InjuryDescription { get; set; }
        public string? PhotoUrl { get; set; }
        public short? PhotoWidth { get; set; }
        public short? PhotoHeight { get; set; }
        public int? PhotoBytes { get; set; }
        public string? PhotoThumbnailUrl { get; set; }
        public short? ThumbnailWidth { get; set; }
        public short? ThumbnailHeight { get; set; }
        public int? ThumbnailBytes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }

        // Historial de cambios (últimos 20)
        public List<NFLPlayerChangeDTO> ChangeHistory { get; set; } = new();

        // Equipos fantasy actuales que tienen este jugador
        public List<FantasyTeamWithPlayerDTO> CurrentFantasyTeams { get; set; } = new();
    }

    /// <summary>
    /// Cambio en historial de jugador NFL
    /// </summary>
    public class NFLPlayerChangeDTO
    {
        public long ChangeID { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Equipo fantasy que tiene al jugador
    /// </summary>
    public class FantasyTeamWithPlayerDTO
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string LeagueName { get; set; } = string.Empty;
        public int LeagueID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public string AcquisitionType { get; set; } = string.Empty;
        public DateTime AcquisitionDate { get; set; }
        public string ManagerName { get; set; } = string.Empty;
    }
}