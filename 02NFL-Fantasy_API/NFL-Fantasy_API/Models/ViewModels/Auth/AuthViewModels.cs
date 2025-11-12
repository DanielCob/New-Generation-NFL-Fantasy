namespace NFL_Fantasy_API.Models.ViewModels.Auth
{
    /// <summary>
    /// Mapea vw_UserProfileHeader
    /// Vista: información básica del perfil del usuario para header/navegación
    /// </summary>
    public class UserProfileHeaderVM
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
        public string SystemRoleDisplay { get; set; } = string.Empty; // NUEVO
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public byte AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserActiveSessions
    /// Vista: sesiones activas y válidas de un usuario
    /// </summary>
    public class UserActiveSessionVM
    {
        public int UserID { get; set; }
        public Guid SessionID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Mapea vw_UserProfileBasic
    /// Vista: perfil básico completo del usuario con todos los campos visibles
    /// </summary>
    public class UserProfileBasicVM
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string SystemRoleCode { get; set; } = "USER";
        public string SystemRoleDisplay { get; set; } = string.Empty; // NUEVO
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public short? ProfileImageWidth { get; set; }
        public short? ProfileImageHeight { get; set; }
        public int? ProfileImageBytes { get; set; }
        public byte AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // NOTA: Se eliminó el campo "Role" que estaba hardcodeado como 'MANAGER' en la vista antigua
    }

    /// <summary>
    /// Mapea vw_SystemRoles
    /// Vista: roles del sistema disponibles (para dropdowns/selección)
    /// </summary>
    public class SystemRoleVM
    {
        public string RoleCode { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Mapea vw_UsersWithRoles
    /// Vista: usuarios con información completa de roles y estadísticas
    /// Para pantallas de administración
    /// </summary>
    public class UserWithFullRoleVM
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string SystemRoleCode { get; set; } = string.Empty;
        public string SystemRoleDisplay { get; set; } = string.Empty;
        public string? SystemRoleDescription { get; set; }
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public byte AccountStatus { get; set; }
        public string AccountStatusDisplay { get; set; } = string.Empty;
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Estadísticas
        public int TeamsCount { get; set; }
        public int CommissionedLeaguesCount { get; set; }
    }
}