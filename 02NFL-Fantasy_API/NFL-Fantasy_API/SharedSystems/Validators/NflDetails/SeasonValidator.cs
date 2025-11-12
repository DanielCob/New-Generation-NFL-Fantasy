using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.SharedSystems.Validators.NflDetails
{
    /// <summary>
    /// Validador centralizado de temporadas NFL.
    /// </summary>
    public static class SeasonValidator
    {
        private const int MinWeekCount = 1;
        private const int MaxWeekCount = 25; // Incluyendo playoffs
        private const int MaxNameLength = 100;

        /// <summary>
        /// Valida los datos para crear una temporada.
        /// </summary>
        public static List<string> ValidateCreateSeason(CreateSeasonRequestDTO dto)
        {
            var errors = new List<string>();

            // Validar nombre
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add("El nombre de la temporada es requerido.");
            }
            else if (dto.Name.Length > MaxNameLength)
            {
                errors.Add($"El nombre de la temporada no puede exceder {MaxNameLength} caracteres.");
            }

            // Validar WeekCount
            if (dto.WeekCount < MinWeekCount || dto.WeekCount > MaxWeekCount)
            {
                errors.Add($"El número de semanas debe estar entre {MinWeekCount} y {MaxWeekCount}.");
            }

            // Validar fechas
            var dateErrors = ValidateDateRange(dto.StartDate, dto.EndDate);
            errors.AddRange(dateErrors);

            // Validar que el rango de fechas sea suficiente para las semanas
            if (!dateErrors.Any())
            {
                var totalDays = (dto.EndDate - dto.StartDate).TotalDays;
                var minDaysRequired = dto.WeekCount * 7; // Al menos 7 días por semana

                if (totalDays < minDaysRequired)
                {
                    errors.Add($"El rango de fechas debe ser de al menos {minDaysRequired} días para {dto.WeekCount} semanas.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valida los datos para actualizar una temporada.
        /// </summary>
        public static List<string> ValidateUpdateSeason(UpdateSeasonRequestDTO dto)
        {
            var errors = new List<string>();

            // Validar nombre
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add("El nombre de la temporada es requerido.");
            }
            else if (dto.Name.Length > MaxNameLength)
            {
                errors.Add($"El nombre de la temporada no puede exceder {MaxNameLength} caracteres.");
            }

            // Validar WeekCount
            if (dto.WeekCount < MinWeekCount || dto.WeekCount > MaxWeekCount)
            {
                errors.Add($"El número de semanas debe estar entre {MinWeekCount} y {MaxWeekCount}.");
            }

            // Validar fechas
            var dateErrors = ValidateDateRange(dto.StartDate, dto.EndDate);
            errors.AddRange(dateErrors);

            // Validar que el rango de fechas sea suficiente para las semanas
            if (!dateErrors.Any())
            {
                var totalDays = (dto.EndDate - dto.StartDate).TotalDays;
                var minDaysRequired = dto.WeekCount * 7;

                if (totalDays < minDaysRequired)
                {
                    errors.Add($"El rango de fechas debe ser de al menos {minDaysRequired} días para {dto.WeekCount} semanas.");
                }
            }

            // Validar lógica de SetAsCurrent
            if (dto.SetAsCurrent == true && !dto.ConfirmMakeCurrent)
            {
                errors.Add("Debe confirmar el cambio de temporada actual.");
            }

            return errors;
        }

        /// <summary>
        /// Valida un rango de fechas.
        /// </summary>
        public static List<string> ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            var errors = new List<string>();

            // La fecha de inicio debe ser anterior a la fecha de fin
            if (startDate >= endDate)
            {
                errors.Add("La fecha de inicio debe ser anterior a la fecha de fin.");
            }

            // Validar que las fechas no sean muy lejanas en el futuro
            var maxFutureYears = 3;
            var maxFutureDate = DateTime.Today.AddYears(maxFutureYears);

            if (startDate > maxFutureDate)
            {
                errors.Add($"La fecha de inicio no puede ser más de {maxFutureYears} años en el futuro.");
            }

            if (endDate > maxFutureDate)
            {
                errors.Add($"La fecha de fin no puede ser más de {maxFutureYears} años en el futuro.");
            }

            return errors;
        }

        /// <summary>
        /// Valida el flag de confirmación para operaciones peligrosas.
        /// </summary>
        public static List<string> ValidateConfirmation(bool confirm, string operationName)
        {
            var errors = new List<string>();

            if (!confirm)
            {
                errors.Add($"Debe confirmar la operación de {operationName}.");
            }

            return errors;
        }
    }
}