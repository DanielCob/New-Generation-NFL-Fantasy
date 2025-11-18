using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.NflDetails
{
    /// <summary>
    /// DTO para agregar noticia de jugador NFL
    /// Feature 10.3 - US 1: Agregar noticia de jugador
    /// </summary>
    public class AddNFLPlayerNewsDTO
    {
        [Required(ErrorMessage = "El ID del jugador es obligatorio.")]
        public int NFLPlayerID { get; set; }

        [Required(ErrorMessage = "El texto de la noticia es obligatorio.")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "El texto debe tener entre 10 y 300 caracteres.")]
        public string NewsText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe indicar si es noticia de lesión.")]
        public bool IsInjury { get; set; }

        [StringLength(30, ErrorMessage = "El resumen de lesión no puede exceder 30 caracteres.")]
        public string? InjurySummary { get; set; }

        [StringLength(10, ErrorMessage = "La designación no puede exceder 10 caracteres.")]
        public string? Designation { get; set; }
    }

    /// <summary>
    /// Respuesta al agregar noticia
    /// </summary>
    public class AddNFLPlayerNewsResponseDTO
    {
        public long NewsID { get; set; }
        public string Message { get; set; } = "Noticia agregada exitosamente.";
    }

    /// <summary>
    /// DTO para eliminar noticia de jugador
    /// Feature 10.3 - US 2: Eliminar noticia de jugador y revertir designación
    /// </summary>
    public class DeleteNFLPlayerNewsRequestDTO
    {
        [Required(ErrorMessage = "El ID de la noticia es obligatorio.")]
        public long NewsID { get; set; }
    }

    /// <summary>
    /// Respuesta al eliminar noticia
    /// </summary>
    public class DeleteNFLPlayerNewsResponseDTO
    {
        public string Message { get; set; } = "Noticia eliminada exitosamente.";
        public string? RevertedDesignation { get; set; }
    }

    /// <summary>
    /// DTO para obtener feed de noticias de un jugador
    /// Feature 10.3: Listar noticias de jugador
    /// </summary>
    public class GetNFLPlayerNewsFeedRequestDTO
    {
        [Required(ErrorMessage = "El ID del jugador es obligatorio.")]
        public int NFLPlayerID { get; set; }

        [Range(1, 100, ErrorMessage = "PageNumber debe estar entre 1 y 100.")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "PageSize debe estar entre 1 y 50.")]
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Respuesta del feed de noticias
    /// </summary>
    public class GetNFLPlayerNewsFeedResponseDTO
    {
        public List<NFLPlayerNewsItemDTO> News { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Item individual de noticia en el feed
    /// </summary>
    public class NFLPlayerNewsItemDTO
    {
        public long NewsID { get; set; }
        public int NFLPlayerID { get; set; }
        public string NewsText { get; set; } = string.Empty;
        public bool IsInjury { get; set; }
        public string? InjurySummary { get; set; }
        public string? Designation { get; set; }
        public int CreatedByUserID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Detalles completos de una noticia específica
    /// Feature 10.3: Ver detalles de noticia
    /// </summary>
    public class NFLPlayerNewsDetailsDTO
    {
        public long NewsID { get; set; }
        public int NFLPlayerID { get; set; }
        public string PlayerFirstName { get; set; } = string.Empty;
        public string PlayerLastName { get; set; } = string.Empty;
        public string PlayerFullName { get; set; } = string.Empty;
        public string NewsText { get; set; } = string.Empty;
        public bool IsInjury { get; set; }
        public string? InjurySummary { get; set; }
        public string? Designation { get; set; }
        public int CreatedByUserID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int? DeletedByUserID { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    /// <summary>
    /// DTO para listar jugadores por designación
    /// Feature 10.3: Listar jugadores por estado (IR, OUT, etc.)
    /// </summary>
    public class GetPlayersByDesignationRequestDTO
    {
        [Required(ErrorMessage = "La designación es obligatoria.")]
        [StringLength(10, ErrorMessage = "La designación no puede exceder 10 caracteres.")]
        public string Designation { get; set; } = string.Empty;

        public int? NFLTeamID { get; set; }

        [StringLength(20, ErrorMessage = "La posición no puede exceder 20 caracteres.")]
        public string? Position { get; set; }
    }

    /// <summary>
    /// Respuesta de jugadores por designación
    /// </summary>
    public class PlayerWithDesignationDTO
    {
        public int NFLPlayerID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int NFLTeamID { get; set; }
        public string NFLTeamName { get; set; } = string.Empty;
        public string NFLTeamCity { get; set; } = string.Empty;
        public string CurrentDesignation { get; set; } = string.Empty;
        public string? PhotoThumbnailUrl { get; set; }
        public DateTime DesignationUpdatedAt { get; set; }
    }
}