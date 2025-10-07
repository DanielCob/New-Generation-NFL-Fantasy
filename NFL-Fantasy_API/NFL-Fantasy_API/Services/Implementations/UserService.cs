// Services/Implementations/UserService.cs - VERSIÓN CORREGIDA
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly DatabaseHelper _dbHelper;

        public UserService(IConfiguration configuration)
        {
            _dbHelper = new DatabaseHelper(configuration);
        }

        #region Create Users
        public async Task<ApiResponseDTO> CreateClientAsync(CreateClientDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Username", request.Username),
                    new("@FirstName", request.FirstName),
                    new("@LastSurname", request.LastSurname),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", request.Email),
                    new("@Password", request.Password),
                    new("@BirthDate", request.BirthDate),
                    new("@ProvinceID", request.ProvinceID),
                    new("@CantonID", request.CantonID),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_CreateClient",
                    parameters,
                    reader => new
                    {
                        NewUserID = DatabaseHelper.GetSafeInt32(reader, "NewUserID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "Client created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating client: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> CreateEngineerAsync(CreateEngineerDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Username", request.Username),
                    new("@FirstName", request.FirstName),
                    new("@LastSurname", request.LastSurname),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", request.Email),
                    new("@Password", request.Password),
                    new("@BirthDate", request.BirthDate),
                    new("@ProvinceID", request.ProvinceID),
                    new("@CantonID", request.CantonID),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value),
                    new("@Career", request.Career),
                    new("@Specialization", (object?)request.Specialization ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_CreateEngineer",
                    parameters,
                    reader => new
                    {
                        NewUserID = DatabaseHelper.GetSafeInt32(reader, "NewUserID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "Engineer created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating engineer: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponseDTO> CreateAdministratorAsync(CreateAdministratorDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Username", request.Username),
                    new("@FirstName", request.FirstName),
                    new("@LastSurname", request.LastSurname),
                    new("@SecondSurname", (object?)request.SecondSurname ?? DBNull.Value),
                    new("@Email", request.Email),
                    new("@Password", request.Password),
                    new("@BirthDate", request.BirthDate),
                    new("@ProvinceID", request.ProvinceID),
                    new("@CantonID", request.CantonID),
                    new("@DistrictID", (object?)request.DistrictID ?? DBNull.Value),
                    new("@Detail", (object?)request.Detail ?? DBNull.Value)
                };

                var result = await _dbHelper.ExecuteStoredProcedureAsync<object>(
                    "sp_CreateAdministrator",
                    parameters,
                    reader => new
                    {
                        NewUserID = DatabaseHelper.GetSafeInt32(reader, "NewUserID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                return new ApiResponseDTO
                {
                    Success = true,
                    Message = "Administrator created successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Error creating administrator: {ex.Message}"
                };
            }
        }
        #endregion

        #region Get Users by ID
        public async Task<UserResponseDTO?> GetClientByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<UserResponseDTO>(
                    @"Users u
                      INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
                      INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
                      INNER JOIN Cantons c ON u.CantonID = c.CantonID
                      LEFT JOIN Districts d ON u.DistrictID = d.DistrictID",
                    reader => new UserResponseDTO
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = CalculateAge(DatabaseHelper.GetSafeDateTime(reader, "BirthDate")),
                        UserType = DatabaseHelper.GetSafeString(reader, "TypeName"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                    },
                    $"u.UserID = {id} AND u.UserTypeID = 1"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<EngineerResponseDTO?> GetEngineerByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<EngineerResponseDTO>(
                    @"Users u
                      INNER JOIN Engineers e ON u.UserID = e.UserID
                      INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
                      INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
                      INNER JOIN Cantons c ON u.CantonID = c.CantonID
                      LEFT JOIN Districts d ON u.DistrictID = d.DistrictID",
                    reader => new EngineerResponseDTO
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = CalculateAge(DatabaseHelper.GetSafeDateTime(reader, "BirthDate")),
                        UserType = DatabaseHelper.GetSafeString(reader, "TypeName"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        Career = DatabaseHelper.GetSafeString(reader, "Career"),
                        Specialization = DatabaseHelper.GetSafeNullableString(reader, "Specialization")
                    },
                    $"u.UserID = {id} AND u.UserTypeID = 2"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<AdministratorResponseDTO?> GetAdministratorByIdAsync(int id)
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<AdministratorResponseDTO>(
                    @"Users u
                      INNER JOIN Administrators a ON u.UserID = a.UserID
                      INNER JOIN UserTypes ut ON u.UserTypeID = ut.UserTypeID
                      INNER JOIN Provinces p ON u.ProvinceID = p.ProvinceID
                      INNER JOIN Cantons c ON u.CantonID = c.CantonID
                      LEFT JOIN Districts d ON u.DistrictID = d.DistrictID",
                    reader => new AdministratorResponseDTO
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = CalculateAge(DatabaseHelper.GetSafeDateTime(reader, "BirthDate")),
                        UserType = DatabaseHelper.GetSafeString(reader, "TypeName"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        Detail = DatabaseHelper.GetSafeNullableString(reader, "Detail")
                    },
                    $"u.UserID = {id} AND u.UserTypeID = 3"
                ).ContinueWith(task => task.Result.FirstOrDefault());
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region Views - Active Users
        public async Task<IEnumerable<ClientViewModel>> GetActiveClientsAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<ClientViewModel>(
                    "vw_ActiveClients",
                    reader => new ClientViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = true // Always true for active views
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving active clients: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<EngineerViewModel>> GetActiveEngineersAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<EngineerViewModel>(
                    "vw_ActiveEngineers",
                    reader => new EngineerViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        Career = DatabaseHelper.GetSafeString(reader, "Career"),
                        Specialization = DatabaseHelper.GetSafeNullableString(reader, "Specialization"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = true // Always true for active views
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving active engineers: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<AdministratorViewModel>> GetActiveAdministratorsAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<AdministratorViewModel>(
                    "vw_ActiveAdministrators",
                    reader => new AdministratorViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        Detail = DatabaseHelper.GetSafeNullableString(reader, "Detail"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = true // Always true for active views
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving active administrators: {ex.Message}", ex);
            }
        }
        #endregion

        #region Views - All Users
        public async Task<IEnumerable<ClientViewModel>> GetAllClientsAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<ClientViewModel>(
                    "vw_AllClients",
                    reader => new ClientViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = DatabaseHelper.GetSafeIntToBool(reader, "HasActiveSession")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all clients: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<EngineerViewModel>> GetAllEngineersAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<EngineerViewModel>(
                    "vw_AllEngineers",
                    reader => new EngineerViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        Career = DatabaseHelper.GetSafeString(reader, "Career"),
                        Specialization = DatabaseHelper.GetSafeNullableString(reader, "Specialization"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = DatabaseHelper.GetSafeIntToBool(reader, "HasActiveSession")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all engineers: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<AdministratorViewModel>> GetAllAdministratorsAsync()
        {
            try
            {
                return await _dbHelper.ExecuteViewAsync<AdministratorViewModel>(
                    "vw_AllAdministrators",
                    reader => new AdministratorViewModel
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Username = DatabaseHelper.GetSafeString(reader, "Username"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastSurname = DatabaseHelper.GetSafeString(reader, "LastSurname"),
                        SecondSurname = DatabaseHelper.GetSafeNullableString(reader, "SecondSurname"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        BirthDate = DatabaseHelper.GetSafeDateTime(reader, "BirthDate"),
                        Age = DatabaseHelper.GetSafeInt32(reader, "Age"),
                        Detail = DatabaseHelper.GetSafeNullableString(reader, "Detail"),
                        ProvinceName = DatabaseHelper.GetSafeString(reader, "ProvinceName"),
                        CantonName = DatabaseHelper.GetSafeString(reader, "CantonName"),
                        DistrictName = DatabaseHelper.GetSafeNullableString(reader, "DistrictName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        HasActiveSession = DatabaseHelper.GetSafeIntToBool(reader, "HasActiveSession")
                    }
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all administrators: {ex.Message}", ex);
            }
        }
        #endregion

        #region Helper Methods
        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
                age--;
            return age;
        }
        #endregion
    }
}