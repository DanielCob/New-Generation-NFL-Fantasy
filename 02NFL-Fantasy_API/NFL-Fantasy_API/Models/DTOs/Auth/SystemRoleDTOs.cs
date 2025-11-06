using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Auth
{
    // ============================================================
    // SYSTEM ROLES DTOs
    // ============================================================

    /// <summary>
    /// DTO que representa un rol del sistema.
    /// Usado para listar roles disponibles.
    /// </summary>
    public class SystemRoleDTO
    {
        /// <summary>
        /// Código único del rol (ej: ADMIN, USER, BRAND_MANAGER)
        /// </summary>
        public string RoleCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre legible del rol para mostrar en UI
        /// </summary>
        public string Display { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada de los permisos y capacidades del rol
        /// </summary>
        public string? Description { get; set; }
    }

    // ============================================================
    // CHANGE USER ROLE DTOs
    // ============================================================

    /// <summary>
    /// DTO para solicitar cambio de rol de sistema de un usuario.
    /// Usado en PUT /api/system-roles/users/{targetUserId}
    /// </summary>
    public class ChangeUserSystemRoleDTO
    {
        /// <summary>
        /// Código del nuevo rol a asignar.
        /// Valores válidos: USER, ADMIN, BRAND_MANAGER
        /// </summary>
        [Required(ErrorMessage = "El código de rol es requerido.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El código de rol debe tener entre 3 y 20 caracteres.")]
        public string NewRoleCode { get; set; } = "USER";

        /// <summary>
        /// Razón o justificación del cambio de rol (opcional pero recomendado para auditoría)
        /// </summary>
        [StringLength(500, ErrorMessage = "La razón no puede exceder 500 caracteres.")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO de respuesta al cambiar el rol de un usuario.
    /// Retornado por PUT /api/system-roles/users/{targetUserId}
    /// </summary>
    public class ChangeUserRoleResultDTO
    {
        /// <summary>
        /// ID del usuario cuyo rol fue modificado
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Código del rol anterior
        /// </summary>
        public string OldRoleCode { get; set; } = "USER";

        /// <summary>
        /// Código del nuevo rol asignado
        /// </summary>
        public string NewRoleCode { get; set; } = "USER";

        /// <summary>
        /// Mensaje de confirmación del cambio
        /// </summary>
        public string Message { get; set; } = "Rol actualizado correctamente.";
    }

    // ============================================================
    // ROLE CHANGE HISTORY DTOs
    // ============================================================

    /// <summary>
    /// DTO que representa un cambio de rol en el historial.
    /// Usado para auditoría de cambios de roles.
    /// Retornado por GET /api/system-roles/users/{userId}/changes
    /// </summary>
    public class UserRoleChangeHistoryDTO
    {
        /// <summary>
        /// ID único del registro de cambio
        /// </summary>
        public long ChangeID { get; set; }

        /// <summary>
        /// ID del usuario cuyo rol fue modificado
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// ID del administrador que realizó el cambio
        /// </summary>
        public int ChangedByUserID { get; set; }

        /// <summary>
        /// Código del rol anterior
        /// </summary>
        public string OldRoleCode { get; set; } = string.Empty;

        /// <summary>
        /// Código del nuevo rol asignado
        /// </summary>
        public string NewRoleCode { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora (UTC) en que se realizó el cambio
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Razón o justificación del cambio (si se proporcionó)
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Dirección IP desde donde se realizó el cambio
        /// </summary>
        public string? SourceIp { get; set; }

        /// <summary>
        /// User-Agent del navegador/cliente que realizó el cambio
        /// </summary>
        public string? UserAgent { get; set; }
    }

    // ============================================================
    // PAGINATION DTOs (Para futuros endpoints)
    // ============================================================

    /// <summary>
    /// DTO de paginación para listar usuarios filtrados por rol.
    /// Usado para endpoints futuros de búsqueda de usuarios.
    /// </summary>
    /// <remarks>
    /// Este DTO está preparado para futuros endpoints como:
    /// GET /api/system-roles/{roleCode}/users?page=1&pageSize=20
    /// </remarks>
    public class UsersByRolePageDTO
    {
        /// <summary>
        /// Número total de registros que coinciden con el filtro (sin paginación)
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Número de página actual (1-based)
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Cantidad de registros por página
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Número total de páginas disponibles
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Lista de usuarios en la página actual
        /// </summary>
        public List<UserWithRoleVM> Items { get; set; } = new();
    }

    // ============================================================
    // VIEW MODELS (Para composición de datos)
    // ============================================================
}