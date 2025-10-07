// Services/Implementations/LocationService.cs - VERSIÓN CORREGIDA
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class LocationService : ILocationService
    {
        private readonly DatabaseHelper _dbHelper;

        public LocationService(IConfiguration configuration)
        {
            _dbHelper = new DatabaseHelper(configuration);
        }

        #region Provinces
        public async Task<IEnumerable<ProvinceDTO>> GetProvincesAsync()
        {
            try
            {
                return await _dbHelper.ExecuteStoredProcedureListAsync<ProvinceDTO>(
                    "sp_GetProvinces",
                    null,
                    reader => new ProvinceDTO
                    {
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving provinces: {ex.Message}", ex);
            }
        }

        public async Task<ProvinceDTO?> GetProvinceByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<ProvinceDTO>(
                    "Provinces",
                    reader => new ProvinceDTO
                    {
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    $"ProvinceID = {id}"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving province by ID: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponseDTO> CreateProvinceAsync(CreateProvinceDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@ProvinceName", request.ProvinceName),
                    new("@ProvinceID", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_AddProvince",
                    parameters,
                    reader => new
                    {
                        NewProvinceID = DatabaseHelper.GetSafeInt32(reader, "NewProvinceID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "Province created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating province: {ex.Message}"
                };
            }
        }
        #endregion

        #region Cantons
        public async Task<IEnumerable<CantonDTO>> GetCantonsByProvinceAsync(int provinceId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@ProvinceID", provinceId)
                };

                return await _dbHelper.ExecuteStoredProcedureListAsync<CantonDTO>(
                    "sp_GetCantonsByProvince",
                    parameters,
                    reader => new CantonDTO
                    {
                        CantonID = DatabaseHelper.GetSafeInt32(reader, "CantonID"),
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving cantons by province: {ex.Message}", ex);
            }
        }

        public async Task<CantonDTO?> GetCantonByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<CantonDTO>(
                    "Cantons c INNER JOIN Provinces p ON c.ProvinceID = p.ProvinceID",
                    reader => new CantonDTO
                    {
                        CantonID = DatabaseHelper.GetSafeInt32(reader, "CantonID"),
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    $"c.CantonID = {id}"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving canton by ID: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponseDTO> CreateCantonAsync(CreateCantonDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@CantonName", request.CantonName),
                    new("@ProvinceID", request.ProvinceID),
                    new("@CantonID", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_AddCanton",
                    parameters,
                    reader => new
                    {
                        NewCantonID = DatabaseHelper.GetSafeInt32(reader, "NewCantonID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "Canton created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating canton: {ex.Message}"
                };
            }
        }
        #endregion

        #region Districts
        public async Task<IEnumerable<DistrictDTO>> GetDistrictsByCantonAsync(int cantonId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@CantonID", cantonId)
                };

                return await _dbHelper.ExecuteStoredProcedureListAsync<DistrictDTO>(
                    "sp_GetDistrictsByCanton",
                    parameters,
                    reader => new DistrictDTO
                    {
                        DistrictID = DatabaseHelper.GetSafeInt32(reader, "DistrictID"),
                        CantonID = DatabaseHelper.GetSafeInt32(reader, "CantonID"),
                        DistrictName = DatabaseHelper.GetSafeString(reader, "DistrictName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving districts by canton: {ex.Message}", ex);
            }
        }

        public async Task<DistrictDTO?> GetDistrictByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<DistrictDTO>(
                    @"Districts d 
                      INNER JOIN Cantons c ON d.CantonID = c.CantonID 
                      INNER JOIN Provinces p ON c.ProvinceID = p.ProvinceID",
                    reader => new DistrictDTO
                    {
                        DistrictID = DatabaseHelper.GetSafeInt32(reader, "DistrictID"),
                        CantonID = DatabaseHelper.GetSafeInt32(reader, "CantonID"),
                        DistrictName = DatabaseHelper.GetSafeString(reader, "DistrictName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        ProvinceID = DatabaseHelper.GetSafeInt32(reader, "ProvinceID"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    $"d.DistrictID = {id}"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving district by ID: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponseDTO> CreateDistrictAsync(CreateDistrictDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@DistrictName", request.DistrictName),
                    new("@CantonID", request.CantonID),
                    new("@DistrictID", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_AddDistrict",
                    parameters,
                    reader => new
                    {
                        NewDistrictID = DatabaseHelper.GetSafeInt32(reader, "NewDistrictID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "District created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating district: {ex.Message}"
                };
            }
        }
        #endregion
    }
}