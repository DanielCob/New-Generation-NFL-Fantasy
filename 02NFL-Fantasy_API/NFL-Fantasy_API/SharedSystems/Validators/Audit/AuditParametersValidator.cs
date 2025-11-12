using NFL_Fantasy_API.Models.DTOs.Audit;

namespace NFL_Fantasy_API.SharedSystems.Validators.Audit
{
    /// <summary>
    /// Validador centralizado de parámetros de auditoría.
    /// </summary>
    public static class AuditParametersValidator
    {
        private const int MinRetentionDays = 1;
        private const int MaxRetentionDays = 180;
        private const int MinStatsDays = 1;
        private const int MaxStatsDays = 365;
        private const int MinTopRecords = 1;
        private const int MaxTopRecords = 500;

        /// <summary>
        /// Valida el parámetro de días de retención para limpieza.
        /// </summary>
        public static List<string> ValidateRetentionDays(int retentionDays)
        {
            var errors = new List<string>();

            if (retentionDays < MinRetentionDays || retentionDays > MaxRetentionDays)
            {
                errors.Add($"Los días de retención deben estar entre {MinRetentionDays} y {MaxRetentionDays}.");
            }

            return errors;
        }

        /// <summary>
        /// Valida el parámetro de días para estadísticas.
        /// </summary>
        public static List<string> ValidateStatsDays(int days)
        {
            var errors = new List<string>();

            if (days < MinStatsDays || days > MaxStatsDays)
            {
                errors.Add($"Los días para estadísticas deben estar entre {MinStatsDays} y {MaxStatsDays}.");
            }

            return errors;
        }

        /// <summary>
        /// Valida el parámetro de cantidad de registros TOP.
        /// </summary>
        public static List<string> ValidateTopRecords(int top)
        {
            var errors = new List<string>();

            if (top < MinTopRecords || top > MaxTopRecords)
            {
                errors.Add($"La cantidad de registros debe estar entre {MinTopRecords} y {MaxTopRecords}.");
            }

            return errors;
        }

        /// <summary>
        /// Valida el filtro completo de logs de auditoría.
        /// </summary>
        public static List<string> ValidateAuditLogFilter(AuditLogFilterDTO filter)
        {
            var errors = new List<string>();

            // Validar TOP
            var topErrors = ValidateTopRecords(filter.Top);
            errors.AddRange(topErrors);

            // Validar rango de fechas si ambas están presentes
            if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            {
                if (filter.StartDate.Value > filter.EndDate.Value)
                {
                    errors.Add("La fecha de inicio no puede ser mayor que la fecha de fin.");
                }
            }

            return errors;
        }
    }
}