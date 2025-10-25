namespace NFL_Fantasy_API.Models.DTOs
{
    public class SystemRoleDTO
    {
        public string RoleCode { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    public class ChangeUserSystemRoleDTO
    {
        public string NewRoleCode { get; set; } = "USER"; // ADMIN/USER/BRAND_MANAGER
        public string? Reason { get; set; }
    }

    public class ChangeUserSystemRoleResponseDTO
    {
        public int UserID { get; set; }
        public string OldRoleCode { get; set; } = "USER";
        public string NewRoleCode { get; set; } = "USER";
        public string Message { get; set; } = "Rol actualizado correctamente.";
    }

    public class SystemRoleChangeLogDTO
    {
        public long ChangeID { get; set; }
        public int UserID { get; set; }
        public int ChangedByUserID { get; set; }
        public string OldRoleCode { get; set; } = string.Empty;
        public string NewRoleCode { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Reason { get; set; }
        public string? SourceIp { get; set; }
    }

    public class UsersByRolePageDTO
    {
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<UserWithRoleVM> Items { get; set; } = new();
    }
}
