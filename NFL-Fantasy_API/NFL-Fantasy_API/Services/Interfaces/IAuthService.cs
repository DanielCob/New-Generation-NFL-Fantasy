// Services/Interfaces/IAuthService.cs
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<ApiResponseDTO> LogoutAsync(Guid token);
        Task<ApiResponseDTO> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request);
        Task<ApiResponseDTO> ResetPasswordByAdminAsync(int adminUserId, ResetPasswordRequestDTO request);
        Task<(bool IsValid, int? UserID, string? UserType)> ValidateSessionTokenAsync(Guid token);
    }
}