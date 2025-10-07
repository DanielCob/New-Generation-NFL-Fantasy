// Services/Implementations/AdminService.cs
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly DatabaseHelper _dbHelper;

        public AdminService(IConfiguration configuration)
        {
            _dbHelper = new DatabaseHelper(configuration);
        }

        #region Update Users
        public async Task<ApiResponseDTO> UpdateClientAsync(int userId, UpdateClientDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId),
                    new("@Username", (object?)request.Username ?? DBNull.Value),
                    new("@FirstName", (object?)request.FirstName ?? DBNull.Value),
                    new("@LastSurname", (object?)request.LastSurname ?? DBNull.Value),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", (object?)request.Email ?? DBNull.Value),
                    new("@Password", (object?)request.Password ?? DBNull.Value),
                    new("@BirthDate", (object?)request.BirthDate ?? DBNull.Value),
                    new("@ProvinceID", (object?)request.ProvinceID ?? DBNull.Value),
                    new("@CantonID", (object?)request.CantonID ?? DBNull.Value),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_UpdateClient",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Client updated successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Client updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error updating client: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> UpdateEngineerAsync(int userId, UpdateEngineerDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId),
                    new("@Username", (object?)request.Username ?? DBNull.Value),
                    new("@FirstName", (object?)request.FirstName ?? DBNull.Value),
                    new("@LastSurname", (object?)request.LastSurname ?? DBNull.Value),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", (object?)request.Email ?? DBNull.Value),
                    new("@Password", (object?)request.Password ?? DBNull.Value),
                    new("@BirthDate", (object?)request.BirthDate ?? DBNull.Value),
                    new("@ProvinceID", (object?)request.ProvinceID ?? DBNull.Value),
                    new("@CantonID", (object?)request.CantonID ?? DBNull.Value),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value),
                    new("@Career", (object?)request.Career ?? DBNull.Value),
                    new("@Specialization", (object?)request.Specialization ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_UpdateEngineer",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Engineer updated successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Engineer updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error updating engineer: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> UpdateAdministratorAsync(int userId, UpdateAdministratorDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId),
                    new("@Username", (object?)request.Username ?? DBNull.Value),
                    new("@FirstName", (object?)request.FirstName ?? DBNull.Value),
                    new("@LastSurname", (object?)request.LastSurname ?? DBNull.Value),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", (object?)request.Email ?? DBNull.Value),
                    new("@Password", (object?)request.Password ?? DBNull.Value),
                    new("@BirthDate", (object?)request.BirthDate ?? DBNull.Value),
                    new("@ProvinceID", (object?)request.ProvinceID ?? DBNull.Value),
                    new("@CantonID", (object?)request.CantonID ?? DBNull.Value),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value),
                    new("@Detail", (object?)request.Detail ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_UpdateAdministrator",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Administrator updated successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Administrator updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error updating administrator: {ex.Message}"
                };
            }
        }
        #endregion

        #region Delete Users
        public async Task<ApiResponseDTO> DeleteClientAsync(int userId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_DeleteClient",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Client deleted successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Client deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error deleting client: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> DeleteEngineerAsync(int userId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_DeleteEngineer",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Engineer deleted successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Engineer deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error deleting engineer: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> DeleteAdministratorAsync(int userId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_DeleteAdministrator",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Administrator deleted successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Administrator deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error deleting administrator: {ex.Message}"
                };
            }
        }
        #endregion

        #region Utility Functions
        public async Task<ApiResponseDTO> CleanExpiredTokensAsync()
        {
            try
            {
                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_CleanExpiredTokens",
                    null,
                    reader => "Expired tokens cleaned successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Expired tokens cleaned successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error cleaning expired tokens: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> SyncActiveStatusAsync()
        {
            try
            {
                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_SyncIsActiveWithTokens",
                    null,
                    reader => reader["Message"].ToString() ?? "Active status synchronized successfully"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Active status synchronized successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error synchronizing active status: {ex.Message}"
                };
            }
        }
        #endregion
    }
}