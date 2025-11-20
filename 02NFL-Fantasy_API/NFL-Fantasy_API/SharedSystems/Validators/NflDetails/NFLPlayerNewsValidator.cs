using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.SharedSystems.Validators.NflDetails
{
    /// <summary>
    /// Validador centralizado para noticias de jugadores NFL.
    /// Feature 10.3: Estado de Jugador
    /// </summary>
    public static class NFLPlayerNewsValidator
    {
        private const int MinNewsTextLength = 10;
        private const int MaxNewsTextLength = 300;
        private const int MaxInjurySummaryLength = 30;

        private static readonly string[] ValidDesignations = { "O", "D", "Q", "P", "FP", "IR", "PUP", "SUS" };

        /// <summary>
        /// Valida los datos para agregar una noticia de jugador.
        /// </summary>
        public static List<string> ValidateAddNews(AddNFLPlayerNewsDTO dto)
        {
            var errors = new List<string>();

            // Validar texto de la noticia
            if (string.IsNullOrWhiteSpace(dto.NewsText))
            {
                errors.Add("El texto de la noticia es requerido.");
            }
            else if (dto.NewsText.Length < MinNewsTextLength)
            {
                errors.Add($"El texto debe tener al menos {MinNewsTextLength} caracteres.");
            }
            else if (dto.NewsText.Length > MaxNewsTextLength)
            {
                errors.Add($"El texto no puede exceder {MaxNewsTextLength} caracteres.");
            }

            // Validaciones específicas para noticias de lesión
            if (dto.IsInjury)
            {
                if (string.IsNullOrWhiteSpace(dto.InjurySummary))
                {
                    errors.Add("El resumen de lesión es obligatorio para noticias de lesión.");
                }
                else if (dto.InjurySummary.Length > MaxInjurySummaryLength)
                {
                    errors.Add($"El resumen de lesión no puede exceder {MaxInjurySummaryLength} caracteres.");
                }

                if (string.IsNullOrWhiteSpace(dto.Designation))
                {
                    errors.Add("La designación es obligatoria para noticias de lesión.");
                }
                else if (!ValidDesignations.Contains(dto.Designation.ToUpper()))
                {
                    errors.Add($"Designación inválida. Valores permitidos: {string.Join(", ", ValidDesignations)}");
                }
            }
            else
            {
                // Si no es lesión, estos campos deben ser null
                if (!string.IsNullOrWhiteSpace(dto.InjurySummary))
                {
                    errors.Add("El resumen de lesión solo aplica para noticias de lesión.");
                }

                if (!string.IsNullOrWhiteSpace(dto.Designation))
                {
                    errors.Add("La designación solo aplica para noticias de lesión.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valida una designación específica.
        /// </summary>
        public static bool IsValidDesignation(string? designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                return false;

            return ValidDesignations.Contains(designation.ToUpper());
        }

        /// <summary>
        /// Obtiene el nombre descriptivo de una designación.
        /// </summary>
        public static string GetDesignationDisplayName(string designation)
        {
            return designation?.ToUpper() switch
            {
                "O" => "Fuera (OUT)",
                "D" => "Dudoso (DOUBTFUL)",
                "Q" => "Cuestionable (QUESTIONABLE)",
                "P" => "Probable (PROBABLE)",
                "FP" => "Participación Plena (FULL PRACTICE)",
                "IR" => "Reserva de Lesionados (INJURED RESERVE)",
                "PUP" => "Incapaz Físicamente de Jugar (PUP)",
                "SUS" => "Suspendido (SUSPENDED)",
                _ => "Desconocido"
            };
        }
    }
}