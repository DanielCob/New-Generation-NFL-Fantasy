using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Services.Interfaces
{
    using NFL_Fantasy_API.Models.DTOs;

    public interface ISystemRolesService
    {
        Task<List<SystemRoleDTO>> GetRolesAsync();
        Task<ChangeUserSystemRoleResponseDTO> ChangeUserRoleAsync(
            int actorUserId, int targetUserId, ChangeUserSystemRoleDTO dto, string? sourceIp = null, string? userAgent = null);
        Task<List<SystemRoleChangeLogDTO>> GetUserRoleChangesAsync(int actorUserId, int targetUserId, int top = 50);
    }
}
