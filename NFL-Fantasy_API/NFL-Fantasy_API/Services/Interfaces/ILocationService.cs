// Services/Interfaces/ILocationService.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.Entities;

namespace NFL_Fantasy_API.Services.Interfaces
{
    public interface ILocationService
    {
        // Provinces
        Task<IEnumerable<ProvinceDTO>> GetProvincesAsync();
        Task<ProvinceDTO?> GetProvinceByIdAsync(int id);
        Task<ApiResponseDTO> CreateProvinceAsync(CreateProvinceDTO request);

        // Cantons
        Task<IEnumerable<CantonDTO>> GetCantonsByProvinceAsync(int provinceId);
        Task<CantonDTO?> GetCantonByIdAsync(int id);
        Task<ApiResponseDTO> CreateCantonAsync(CreateCantonDTO request);

        // Districts
        Task<IEnumerable<DistrictDTO>> GetDistrictsByCantonAsync(int cantonId);
        Task<DistrictDTO?> GetDistrictByIdAsync(int id);
        Task<ApiResponseDTO> CreateDistrictAsync(CreateDistrictDTO request);
    }
}