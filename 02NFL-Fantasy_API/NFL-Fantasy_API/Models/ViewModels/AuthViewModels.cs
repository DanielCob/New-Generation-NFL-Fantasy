namespace NFL_Fantasy_API.Models.ViewModels
{
    /// <summary>
    /// Mapea vw_UserProfileHeader
    /// Vista: información de encabezado del perfil de usuario
    /// </summary>
    public class UserProfileHeaderVM
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Alias { get; set; }
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
        public string LanguageCode { get; set; } = "en";
        public string? ProfileImageUrl { get; set; }
        public short? ProfileImageWidth { get; set; }
        public short? ProfileImageHeight { get; set; }
        public int? ProfileImageBytes { get; set; }
        public byte AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Role { get; set; } = "MANAGER"; // Rol global inicial
    }
}