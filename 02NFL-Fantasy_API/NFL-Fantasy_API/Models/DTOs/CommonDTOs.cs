namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// Respuesta estándar de la API para todas las operaciones
    /// </summary>
    public class ApiResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        // NUEVO: Código específico del error (cuando aplique; p.ej., códigos de SP 50xxx)
        public int? ErrorCode { get; set; }

        // NUEVO: Id de traza para correlacionar en logs
        public string? TraceId { get; set; }

        // Mantiene compatibilidad: firma original + opcionales nuevos
        public static ApiResponseDTO SuccessResponse(string message, object? data = null, string? traceId = null)
        {
            return new ApiResponseDTO
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = traceId
            };
        }

        // Mantiene compatibilidad: firma original + opcionales nuevos
        public static ApiResponseDTO ErrorResponse(string message, int? errorCode = null, string? traceId = null, object? data = null)
        {
            return new ApiResponseDTO
            {
                Success = false,
                Message = message,
                Data = data,          // normalmente null; deja el parámetro por si quieres adjuntar contexto
                ErrorCode = errorCode,
                TraceId = traceId
            };
        }
    }

    /// <summary>
    /// Resultado paginado genérico (para futuras implementaciones)
    /// </summary>
    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Información básica de una entidad creada
    /// </summary>
    public class CreatedEntityDTO
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}