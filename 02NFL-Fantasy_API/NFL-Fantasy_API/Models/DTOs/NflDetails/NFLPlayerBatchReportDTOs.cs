using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.NflDetails
{
    /// <summary>
    /// DTO para creación de reporte de batch de jugadores NFL
    /// Feature: Reportes de importación batch
    /// </summary>
    public class CreateNFLPlayerBatchReportDTO
    {
        [Required(ErrorMessage = "La URL del reporte es obligatoria.")]
        [StringLength(400, MinimumLength = 1, ErrorMessage = "La URL del reporte debe tener entre 1 y 400 caracteres.")]
        public string ReportUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "El total procesado es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El total procesado debe ser mayor o igual a 0.")]
        public int TotalProcessed { get; set; }

        [Required(ErrorMessage = "La cantidad de éxitos es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de éxitos debe ser mayor o igual a 0.")]
        public int SuccessCount { get; set; }

        [Required(ErrorMessage = "La cantidad de errores es obligatoria.")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad de errores debe ser mayor o igual a 0.")]
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// Respuesta de creación de reporte de batch
    /// </summary>
    public class CreateNFLPlayerBatchReportResponseDTO
    {
        public int BatchReportID { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string Message { get; set; } = "Reporte de batch creado exitosamente.";
    }

    /// <summary>
    /// DTO para solicitud de listado de reportes con paginación
    /// Feature: Listar reportes de batch
    /// </summary>
    public class ListNFLPlayerBatchReportsRequestDTO
    {
        [Range(1, 100, ErrorMessage = "PageNumber debe estar entre 1 y 100.")]
        public int PageNumber { get; set; } = 1;

        [Range(10, 100, ErrorMessage = "PageSize debe estar entre 10 y 100.")]
        public int PageSize { get; set; } = 50;

        [StringLength(20)]
        public string OrderBy { get; set; } = "CreatedAt";

        [StringLength(4)]
        public string SortDirection { get; set; } = "DESC";
    }

    /// <summary>
    /// Respuesta de listado de reportes de batch
    /// </summary>
    public class ListNFLPlayerBatchReportsResponseDTO
    {
        public List<NFLPlayerBatchReportListItemDTO> Reports { get; set; } = new();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Item de reporte en listado
    /// </summary>
    public class NFLPlayerBatchReportListItemDTO
    {
        public int BatchReportID { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int ActorUserID { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public string? SourceIp { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Detalles completos de un reporte de batch
    /// Feature: Ver detalles de reporte
    /// </summary>
    public class NFLPlayerBatchReportDetailsDTO
    {
        public int BatchReportID { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int ActorUserID { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public string? SourceIp { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}