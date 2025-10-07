// Services/Interfaces/IAdminService.cs
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Services.Interfaces
{
    public interface IAdminService
    {
        // Update Users
        Task<ApiResponseDTO> UpdateClientAsync(int userId, UpdateClientDTO request);
        Task<ApiResponseDTO> UpdateEngineerAsync(int userId, UpdateEngineerDTO request);
        Task<ApiResponseDTO> UpdateAdministratorAsync(int userId, UpdateAdministratorDTO request);

        // Delete Users
        Task<ApiResponseDTO> DeleteClientAsync(int userId);
        Task<ApiResponseDTO> DeleteEngineerAsync(int userId);
        Task<ApiResponseDTO> DeleteAdministratorAsync(int userId);

        // Utility functions
        Task<ApiResponseDTO> CleanExpiredTokensAsync();
        Task<ApiResponseDTO> SyncActiveStatusAsync();
    }
}