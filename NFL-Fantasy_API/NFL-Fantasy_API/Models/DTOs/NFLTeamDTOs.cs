using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// DTO para creación de equipo NFL
    /// Feature 10.1 - Crear equipo NFL
    /// </summary>
    public class CreateNFLTeamDTO
    {
        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
        public string TeamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "La ciudad debe tener entre 1 y 100 caracteres.")]
        public string City { get; set; } = string.Empty;

        // Campos opcionales de imagen
        [StringLength(400, ErrorMessage = "La URL de imagen no puede superar 400 caracteres.")]
        public string? TeamImageUrl { get; set; }

        [Range(300, 1024, ErrorMessage = "El ancho de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? TeamImageWidth { get; set; }

        [Range(300, 1024, ErrorMessage = "El alto de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? TeamImageHeight { get; set; }

        [Range(1, 5242880, ErrorMessage = "El tamaño de imagen debe estar entre 1 byte y 5MB.")]
        public int? TeamImageBytes { get; set; }

        // Thumbnail
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
    /// Respuesta de creación de equipo NFL
    /// </summary>
    public class CreateNFLTeamResponseDTO
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Message { get; set; } = "Equipo NFL creado exitosamente.";
    }

    /// <summary>
    /// DTO para actualización de equipo NFL
    /// Feature 10.1 - Modificar equipo NFL
    /// </summary>
    public class UpdateNFLTeamDTO
    {
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
        public string? TeamName { get; set; }

        [StringLength(100, MinimumLength = 1, ErrorMessage = "La ciudad debe tener entre 1 y 100 caracteres.")]
        public string? City { get; set; }

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
    /// DTO para solicitud de listado de equipos NFL
    /// Feature 10.1 - Listar equipos NFL
    /// </summary>
    public class ListNFLTeamsRequestDTO
    {
        [Range(1, 100, ErrorMessage = "PageNumber debe estar entre 1 y 100.")]
        public int PageNumber { get; set; } = 1;

        [Range(10, 100, ErrorMessage = "PageSize debe estar entre 10 y 100.")]
        public int PageSize { get; set; } = 50;

        [StringLength(100)]
        public string? SearchTerm { get; set; }

        [StringLength(100)]
        public string? FilterCity { get; set; }

        public bool? FilterIsActive { get; set; }
    }

    /// <summary>
    /// Respuesta de listado de equipos NFL
    /// </summary>
    public class ListNFLTeamsResponseDTO
    {
        public List<NFLTeamListItemDTO> Teams { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Item de equipo NFL en listado
    /// </summary>
    public class NFLTeamListItemDTO
    {
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Detalles completos de equipo NFL
    /// Feature 10.1 - Ver detalles
    /// </summary>
    public class NFLTeamDetailsDTO
    {
        // Información del equipo
        public int NFLTeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? TeamImageUrl { get; set; }
        public short? TeamImageWidth { get; set; }
        public short? TeamImageHeight { get; set; }
        public int? TeamImageBytes { get; set; }
        public string? ThumbnailUrl { get; set; }
        public short? ThumbnailWidth { get; set; }
        public short? ThumbnailHeight { get; set; }
        public int? ThumbnailBytes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }

        // Historial de cambios (últimos 20)
        public List<NFLTeamChangeDTO> ChangeHistory { get; set; } = new();

        // Jugadores activos del equipo
        public List<PlayerBasicDTO> ActivePlayers { get; set; } = new();
    }

    /// <summary>
    /// Cambio en historial de equipo NFL
    /// </summary>
    public class NFLTeamChangeDTO
    {
        public long ChangeID { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
    }
}