using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.SharedSystems.Validators.Fantasy
{
    /// <summary>
    /// Validador centralizado para operaciones de ligas.
    /// </summary>
    public static class LeagueValidator
    {
        private static readonly byte[] ValidTeamSlots = { 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        private static readonly byte[] ValidPlayoffTeams = { 4, 6 };

        /// <summary>
        /// Valida que TeamSlots sea uno de los valores permitidos.
        /// Valores válidos: 4, 6, 8, 10, 12, 14, 16, 18, 20
        /// </summary>
        public static bool IsValidTeamSlots(byte teamSlots)
        {
            return ValidTeamSlots.Contains(teamSlots);
        }

        /// <summary>
        /// Valida que PlayoffTeams sea válido (4 o 6).
        /// </summary>
        public static bool IsValidPlayoffTeams(byte playoffTeams)
        {
            return ValidPlayoffTeams.Contains(playoffTeams);
        }

        /// <summary>
        /// Valida complejidad de contraseña de liga (misma política que usuarios).
        /// Reglas: 8-12 caracteres, alfanumérica, al menos 1 mayúscula, 1 minúscula, 1 dígito
        /// </summary>
        public static List<string> ValidateLeaguePasswordComplexity(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("La contraseña de liga es obligatoria.");
                return errors;
            }

            if (password.Length < 8 || password.Length > 12)
            {
                errors.Add("La contraseña de liga debe tener entre 8 y 12 caracteres.");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos una letra mayúscula.");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos una letra minúscula.");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos un dígito.");
            }

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
            {
                errors.Add("La contraseña de liga debe ser alfanumérica (solo letras y números).");
            }

            return errors;
        }

        /// <summary>
        /// Valida límites de cambios de roster por equipo (1-100).
        /// </summary>
        public static List<string> ValidateRosterLimits(int? maxRosterChanges, int? maxFreeAgentAdds)
        {
            var errors = new List<string>();

            if (maxRosterChanges.HasValue)
            {
                if (maxRosterChanges.Value < 1 || maxRosterChanges.Value > 100)
                {
                    errors.Add("MaxRosterChangesPerTeam debe estar entre 1 y 100, o null para sin límite.");
                }
            }

            if (maxFreeAgentAdds.HasValue)
            {
                if (maxFreeAgentAdds.Value < 1 || maxFreeAgentAdds.Value > 100)
                {
                    errors.Add("MaxFreeAgentAddsPerTeam debe estar entre 1 y 100, o null para sin límite.");
                }
            }

            return errors;
        }
    }
}