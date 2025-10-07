// Services/Implementations/AuthService.cs
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly DatabaseHelper _dbHelper;

        public AuthService(IConfiguration configuration)
        {
            _dbHelper = new DatabaseHelper(configuration);
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Email", request.Email),
                    new("@Password", request.Password),
                    new("@LoginSuccess", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new("@Message", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output },
                    new("@UserID", SqlDbType.Int) { Direction = ParameterDirection.Output },
                    new("@UserType", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output },
                    new("@SessionToken", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output }
                };

                var (success, message, outputValues) = await _dbHelper.ExecuteStoredProcedureWithOutputAsync("sp_UserLogin", parameters);

                if (!success)
                {
                    return new LoginResponseDTO
                    {
                        Success = false,
                        Message = message
                    };
                }

                var loginSuccess = outputValues.ContainsKey("@LoginSuccess") && (bool)(outputValues["@LoginSuccess"] ?? false);
                var responseMessage = outputValues.ContainsKey("@Message") ? outputValues["@Message"]?.ToString() ?? "Unknown error" : "Unknown error";

                var response = new LoginResponseDTO
                {
                    Success = loginSuccess,
                    Message = responseMessage
                };

                if (loginSuccess)
                {
                    response.UserID = outputValues.ContainsKey("@UserID") ? (int?)(outputValues["@UserID"]) : null;
                    response.UserType = outputValues.ContainsKey("@UserType") ? outputValues["@UserType"]?.ToString() : null;
                    response.SessionToken = outputValues.ContainsKey("@SessionToken") ? (Guid?)(outputValues["@SessionToken"]) : null;
                }

                return response;
            }
            catch (Exception ex)
            {
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = $"Error during login: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> LogoutAsync(Guid token)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Token", token)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<string>(
                    "sp_UserLogout",
                    parameters,
                    reader => reader["Message"].ToString() ?? "Logout completed"
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = result ?? "Session closed successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error during logout: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@UserID", userId),
                    new("@OldPassword", request.OldPassword),
                    new("@NewPassword", request.NewPassword),
                    new("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new("@Message", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output }
                };

                var (success, message, outputValues) = await _dbHelper.ExecuteStoredProcedureWithOutputAsync("sp_ChangePassword", parameters);

                if (!success)
                {
                    return new ApiResponseDTO
                    {
                        Success = false,
                        Message = message
                    };
                }

                var changeSuccess = outputValues.ContainsKey("@Success") && (bool)(outputValues["@Success"] ?? false);
                var responseMessage = outputValues.ContainsKey("@Message") ? outputValues["@Message"]?.ToString() ?? "Password change completed" : "Password change completed";

                return new ApiResponseDTO
                {
                    Success = changeSuccess,
                    Message = responseMessage
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error changing password: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> ResetPasswordByAdminAsync(int adminUserId, ResetPasswordRequestDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@AdminUserID", adminUserId),
                    new("@TargetUserID", request.TargetUserID),
                    new("@NewPassword", request.NewPassword),
                    new("@Success", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new("@Message", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output }
                };

                var (success, message, outputValues) = await _dbHelper.ExecuteStoredProcedureWithOutputAsync("sp_ResetPasswordByAdmin", parameters);

                if (!success)
                {
                    return new ApiResponseDTO
                    {
                        Success = false,
                        Message = message
                    };
                }

                var resetSuccess = outputValues.ContainsKey("@Success") && (bool)(outputValues["@Success"] ?? false);
                var responseMessage = outputValues.ContainsKey("@Message") ? outputValues["@Message"]?.ToString() ?? "Password reset completed" : "Password reset completed";

                return new ApiResponseDTO
                {
                    Success = resetSuccess,
                    Message = responseMessage
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error resetting password: {ex.Message}"
                };
            }
        }

        public async Task<(bool IsValid, int? UserID, string? UserType)> ValidateSessionTokenAsync(Guid token)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Token", token),
                    new("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new("@UserID", SqlDbType.Int) { Direction = ParameterDirection.Output },
                    new("@UserType", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output }
                };

                var (success, message, outputValues) = await _dbHelper.ExecuteStoredProcedureWithOutputAsync("sp_ValidateSessionToken", parameters);

                if (!success)
                {
                    return (false, null, null);
                }

                var isValid = outputValues.ContainsKey("@IsValid") && (bool)(outputValues["@IsValid"] ?? false);
                var userId = outputValues.ContainsKey("@UserID") && outputValues["@UserID"] != DBNull.Value
                    ? (int?)(outputValues["@UserID"])
                    : null;
                var userType = outputValues.ContainsKey("@UserType") && outputValues["@UserType"] != DBNull.Value
                    ? outputValues["@UserType"]?.ToString()
                    : null;

                return (isValid, userId, userType);
            }
            catch (Exception)
            {
                return (false, null, null);
            }
        }
    }
}