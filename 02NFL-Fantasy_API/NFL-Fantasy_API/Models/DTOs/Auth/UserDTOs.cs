using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Auth
{
    /// <summary>
    /// DTO para actualización de perfil de usuario (Feature 1.1 - Gestión de perfil)
    /// IMPORTANTE: No se pueden editar Email, UserID, CreatedAt, AccountStatus, ni Role
    /// Solo campos editables del perfil
    /// </summary>
    public class UpdateUserProfileDTO
    {
        [StringLength(50, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 50 caracteres.")]
        public string? Name { get; set; }

        [StringLength(50, ErrorMessage = "El alias no puede superar 50 caracteres.")]
        public string? Alias { get; set; }

        [StringLength(10, ErrorMessage = "El código de idioma no puede superar 10 caracteres.")]
        public string? LanguageCode { get; set; }

        // Campos de imagen de perfil (opcionales, todos o ninguno)
        [StringLength(400, ErrorMessage = "La URL de imagen no puede superar 400 caracteres.")]
        public string? ProfileImageUrl { get; set; }

        [Range(300, 1024, ErrorMessage = "El ancho de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? ProfileImageWidth { get; set; }

        [Range(300, 1024, ErrorMessage = "El alto de imagen debe estar entre 300 y 1024 píxeles.")]
        public short? ProfileImageHeight { get; set; }

        [Range(1, 5242880, ErrorMessage = "El tamaño de imagen debe estar entre 1 byte y 5MB.")]
        public int? ProfileImageBytes { get; set; }
    }

    /// <summary>
    /// Respuesta completa del perfil de usuario (Feature 1.1 - Ver perfil)
    /// Incluye datos del usuario + ligas donde es comisionado + sus equipos
    /// </summary>
    public class UserProfileResponseDTO
    {
        // Datos básicos del usuario
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public byte AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string SystemRoleCode { get; set; } = "USER"; // Rol global inicial

        // Ligas donde soy comisionado (principal o co-comisionado)
        public List<UserCommissionedLeagueDTO> CommissionedLeagues { get; set; } = new();

        // Mis equipos en cada liga
        public List<UserTeamDTO> Teams { get; set; } = new();
    }

    /// <summary>
    /// Liga donde el usuario es comisionado
    /// </summary>
    public class UserCommissionedLeagueDTO
    {
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public byte Status { get; set; }
        public byte TeamSlots { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>
    /// Equipo del usuario en una liga
    /// </summary>
    public class UserTeamDTO
    {
        public int TeamID { get; set; }
        public int LeagueID { get; set; }
        public string LeagueName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Información básica del perfil (para vistas simples)
    /// </summary>
    public class UserProfileBasicDTO
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public short? ProfileImageWidth { get; set; }
        public short? ProfileImageHeight { get; set; }
        public int? ProfileImageBytes { get; set; }
        public byte AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
    }

    /// <summary>
    /// Sesión activa del usuario
    /// </summary>
    public class UserActiveSessionDTO
    {
        public int UserID { get; set; }
        public Guid SessionID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsValid { get; set; }
    }

    public class UserHeaderDTO
    {
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string Email { get; set; } = string.Empty;
        public string SystemRoleCode { get; set; } = "USER";
        public string? ProfileImageUrl { get; set; }
        public byte AccountStatus { get; set; }
        public string LanguageCode { get; set; } = "en";
    }

    public class UserWithRoleVM
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
        public string? SystemRoleDisplay { get; set; }
        public byte AccountStatus { get; set; }
        public int TeamsCount { get; set; }
        public int CommissionedLeaguesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }



}