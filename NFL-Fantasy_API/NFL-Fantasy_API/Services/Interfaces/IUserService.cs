// Services/Interfaces/IUserService.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    public interface IUserService
    {
        // Create Users
        Task<ApiResponseDTO> CreateClientAsync(CreateClientDTO request);
        Task<ApiResponseDTO> CreateEngineerAsync(CreateEngineerDTO request);
        Task<ApiResponseDTO> CreateAdministratorAsync(CreateAdministratorDTO request);

        // Get Users by ID
        Task<UserResponseDTO?> GetClientByIdAsync(int id);
        Task<EngineerResponseDTO?> GetEngineerByIdAsync(int id);
        Task<AdministratorResponseDTO?> GetAdministratorByIdAsync(int id);

        // Views
        Task<IEnumerable<ClientViewModel>> GetActiveClientsAsync();
        Task<IEnumerable<EngineerViewModel>> GetActiveEngineersAsync();
        Task<IEnumerable<AdministratorViewModel>> GetActiveAdministratorsAsync();
        Task<IEnumerable<ClientViewModel>> GetAllClientsAsync();
        Task<IEnumerable<EngineerViewModel>> GetAllEngineersAsync();
        Task<IEnumerable<AdministratorViewModel>> GetAllAdministratorsAsync();
    }
}