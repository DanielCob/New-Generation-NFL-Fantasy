// SharedSystems/Validators/PaginationValidator.cs
namespace NFL_Fantasy_API.SharedSystems.Validators
{
    /// <summary>
    /// Validador centralizado de parámetros de paginación.
    /// Reutilizable en todos los servicios que implementen paginación.
    /// </summary>
    public static class PaginationValidator
    {
        private const int MinPageNumber = 1;
        private const int MinPageSize = 1;
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 50;

        /// <summary>
        /// Valida y ajusta los parámetros de paginación.
        /// </summary>
        /// <returns>Tupla con (PageNumber ajustado, PageSize ajustado, Lista de errores)</returns>
        public static (int PageNumber, int PageSize, List<string> Errors) ValidateAndAdjustPagination(
            int pageNumber,
            int pageSize)
        {
            var errors = new List<string>();
            var adjustedPageNumber = pageNumber;
            var adjustedPageSize = pageSize;

            // Validar PageNumber
            if (pageNumber < MinPageNumber)
            {
                errors.Add($"El número de página debe ser al menos {MinPageNumber}.");
                adjustedPageNumber = MinPageNumber;
            }

            // Validar PageSize
            if (pageSize < MinPageSize)
            {
                errors.Add($"El tamaño de página debe ser al menos {MinPageSize}.");
                adjustedPageSize = DefaultPageSize;
            }
            else if (pageSize > MaxPageSize)
            {
                errors.Add($"El tamaño de página no puede exceder {MaxPageSize}.");
                adjustedPageSize = MaxPageSize;
            }

            return (adjustedPageNumber, adjustedPageSize, errors);
        }

        /// <summary>
        /// Valida parámetros de paginación sin ajustar valores.
        /// </summary>
        public static List<string> ValidatePagination(int pageNumber, int pageSize)
        {
            var errors = new List<string>();

            if (pageNumber < MinPageNumber)
            {
                errors.Add($"El número de página debe ser al menos {MinPageNumber}.");
            }

            if (pageSize < MinPageSize || pageSize > MaxPageSize)
            {
                errors.Add($"El tamaño de página debe estar entre {MinPageSize} y {MaxPageSize}.");
            }

            return errors;
        }
    }
}