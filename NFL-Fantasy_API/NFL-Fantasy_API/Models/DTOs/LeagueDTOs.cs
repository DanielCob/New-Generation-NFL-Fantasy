using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    /// <summary>
    /// DTO para creación de liga (Feature 1.2 - Crear liga)
    /// El CreatorUserID se toma automáticamente del contexto de autenticación
    /// </summary>
    public class CreateLeagueDTO
    {
        [Required(ErrorMessage = "El nombre de la liga es obligatorio.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La cantidad de equipos es obligatoria.")]
        [Range(4, 20, ErrorMessage = "La cantidad de equipos debe ser 4, 6, 8, 10, 12, 14, 16, 18 o 20.")]
        public byte TeamSlots { get; set; }

        [Required(ErrorMessage = "La contraseña de la liga es obligatoria.")]
        [StringLength(12, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 12 caracteres.")]
        public string LeaguePassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del equipo inicial es obligatorio.")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre del equipo debe tener entre 1 y 50 caracteres.")]
        public string InitialTeamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La configuración de playoffs es obligatoria.")]
        [Range(4, 6, ErrorMessage = "PlayoffTeams debe ser 4 o 6.")]
        public byte PlayoffTeams { get; set; } = 4;

        public bool AllowDecimals { get; set; } = true;

        // Estos IDs son opcionales; si vienen NULL, el SP usa los defaults
        public int? PositionFormatID { get; set; }
        public int? ScoringSchemaID { get; set; }

        // Validación personalizada para TeamSlots (debe ser uno de los valores válidos)
        public static bool IsValidTeamSlots(byte value)
        {
            return new byte[] { 4, 6, 8, 10, 12, 14, 16, 18, 20 }.Contains(value);
        }
    }

    /// <summary>
    /// Respuesta de creación de liga exitosa
    /// </summary>
    public class CreateLeagueResponseDTO
    {
        public int LeagueID { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte TeamSlots { get; set; }
        public int AvailableSlots { get; set; }
        public byte Status { get; set; }
        public byte PlayoffTeams { get; set; }
        public bool AllowDecimals { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = "Liga creada exitosamente.";
    }

    /// <summary>
    /// DTO para cambiar el estado de una liga (Feature 1.2 - Administrar estado)
    /// Solo el comisionado principal puede hacerlo
    /// </summary>
    public class SetLeagueStatusDTO
    {
        [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
        [Range(0, 3, ErrorMessage = "El estado debe ser: 0=PreDraft, 1=Active, 2=Inactive, 3=Closed.")]
        public byte NewStatus { get; set; }

        [StringLength(300, ErrorMessage = "La razón no puede superar 300 caracteres.")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO para editar configuración de liga (Feature 1.2 - Editar configuración)
    /// Todos los campos son opcionales (solo se actualizan los que vienen con valor)
    /// Restricciones según estado de la liga (Pre-Draft vs otros)
    /// </summary>
    public class EditLeagueConfigDTO
    {
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres.")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
        public string? Description { get; set; }

        // Solo editable en Pre-Draft
        public byte? TeamSlots { get; set; }

        // Solo editable en Pre-Draft
        public int? PositionFormatID { get; set; }

        // Solo editable en Pre-Draft
        public int? ScoringSchemaID { get; set; }

        // Solo editable en Pre-Draft
        [Range(4, 6, ErrorMessage = "PlayoffTeams debe ser 4 o 6.")]
        public byte? PlayoffTeams { get; set; }

        // Solo editable en Pre-Draft
        public bool? AllowDecimals { get; set; }

        // Solo editable en Pre-Draft
        public bool? TradeDeadlineEnabled { get; set; }

        // Solo editable en Pre-Draft (y solo si TradeDeadlineEnabled=true)
        public DateTime? TradeDeadlineDate { get; set; }

        // Editables en cualquier momento
        [Range(1, 100, ErrorMessage = "MaxRosterChangesPerTeam debe estar entre 1 y 100, o null para sin límite.")]
        public int? MaxRosterChangesPerTeam { get; set; }

        // Editables en cualquier momento
        [Range(1, 100, ErrorMessage = "MaxFreeAgentAddsPerTeam debe estar entre 1 y 100, o null para sin límite.")]
        public int? MaxFreeAgentAddsPerTeam { get; set; }
    }

    /// <summary>
    /// Resumen completo de una liga (Feature 1.2 - Ver liga)
    /// </summary>
    public class LeagueSummaryDTO
    {
        public int LeagueID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int TeamsCount { get; set; }
        public int AvailableSlots { get; set; }
        public byte PlayoffTeams { get; set; }
        public bool AllowDecimals { get; set; }
        public bool TradeDeadlineEnabled { get; set; }
        public DateTime? TradeDeadlineDate { get; set; }
        public int? MaxRosterChangesPerTeam { get; set; }
        public int? MaxFreeAgentAddsPerTeam { get; set; }

        // Formatos
        public int PositionFormatID { get; set; }
        public string PositionFormatName { get; set; } = string.Empty;
        public int ScoringSchemaID { get; set; }
        public string ScoringSchemaName { get; set; } = string.Empty;
        public int ScoringVersion { get; set; }

        // Temporada
        public int SeasonID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Creator
        public int CreatedByUserID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Equipos en la liga
        public List<LeagueTeamDTO> Teams { get; set; } = new();
    }

    /// <summary>
    /// Equipo dentro de una liga
    /// </summary>
    public class LeagueTeamDTO
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int OwnerUserID { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Miembro de una liga (para listados de miembros)
    /// </summary>
    public class LeagueMemberDTO
    {
        public int LeagueID { get; set; }
        public int UserID { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public bool IsPrimaryCommissioner { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Entrada del directorio de ligas (para listados públicos/filtrados)
    /// </summary>
    public class LeagueDirectoryEntryDTO
    {
        public int LeagueID { get; set; }
        public string SeasonLabel { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public int TeamsCount { get; set; }
        public int AvailableSlots { get; set; }
        public int CreatedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}