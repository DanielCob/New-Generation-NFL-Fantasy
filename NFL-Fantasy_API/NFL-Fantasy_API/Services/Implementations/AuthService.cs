using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementaci�n del servicio de autenticaci�n
    /// Maneja registro, login, logout, reset de contrase�a y validaci�n de sesiones
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly DatabaseHelper _db;

        public AuthService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Register

        /// <summary>
        /// Registra un nuevo usuario
        /// SP: app.sp_RegisterUser
        /// Feature 1.1 - Registro de usuario
        /// </summary>
        public async Task<ApiResponseDTO> RegisterAsync(RegisterUserDTO dto, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validaci�n adicional de contrase�a (complejidad)
                var passwordErrors = ValidatePasswordComplexity(dto.Password);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                // Validaci�n de formato de email (adicional a [EmailAddress])
                if (!IsValidEmail(dto.Email))
                {
                    return ApiResponseDTO.ErrorResponse("Formato de correo electr�nico inv�lido.");
                }

                // Validaci�n de imagen si viene
                if (dto.ProfileImageBytes.HasValue)
                {
                    if (!dto.ProfileImageWidth.HasValue || !dto.ProfileImageHeight.HasValue)
                    {
                        return ApiResponseDTO.ErrorResponse("Si proporciona tama�o de imagen, debe incluir ancho y alto.");
                    }
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Name", dto.Name),
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Alias", DatabaseHelper.DbNullIfNull(dto.Alias)),
                    new SqlParameter("@Password", dto.Password),
                    new SqlParameter("@PasswordConfirm", dto.PasswordConfirm),
                    new SqlParameter("@LanguageCode", dto.LanguageCode ?? "en"),
                    new SqlParameter("@ProfileImageUrl", DatabaseHelper.DbNullIfNull(dto.ProfileImageUrl)),
                    new SqlParameter("@ProfileImageWidth", DatabaseHelper.DbNullIfNull(dto.ProfileImageWidth)),
                    new SqlParameter("@ProfileImageHeight", DatabaseHelper.DbNullIfNull(dto.ProfileImageHeight)),
                    new SqlParameter("@ProfileImageBytes", DatabaseHelper.DbNullIfNull(dto.ProfileImageBytes)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var result = await _db.ExecuteStoredProcedureAsync<RegisterResponseDTO>(
                    "app.sp_RegisterUser",
                    parameters,
                    reader => new RegisterResponseDTO
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                if (result != null)
                {
                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al registrar usuario.");
            }
            catch (SqlException ex)
            {
                // Los THROW del SP llegan aqu� con mensajes espec�ficos
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Login

        /// <summary>
        /// Inicia sesi�n de usuario
        /// SP: app.sp_Login (usa OUTPUT parameters)
        /// Feature 1.1 - Inicio de sesi�n
        /// </summary>
        public async Task<ApiResponseDTO> LoginAsync(LoginDTO dto, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Password", dto.Password),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent)),
                    new SqlParameter("@SessionID", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output },
                    new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output }
                };

                var (success, errorMessage, outputValues) = await _db.ExecuteStoredProcedureWithOutputAsync(
                    "app.sp_Login",
                    parameters
                );

                // Leer OUTPUT params
                var sessionId = outputValues.ContainsKey("@SessionID") && outputValues["@SessionID"] != null
                    ? (Guid)outputValues["@SessionID"]
                    : Guid.Empty;

                var message = outputValues.ContainsKey("@Message") && outputValues["@Message"] != null
                    ? outputValues["@Message"].ToString()
                    : (success ? "Login exitoso." : errorMessage ?? "Error desconocido.");

                if (!success || sessionId == Guid.Empty)
                {
                    return ApiResponseDTO.ErrorResponse(message ?? "Credenciales inv�lidas.");
                }

                // Obtener datos adicionales del usuario para la respuesta
                var userInfo = await GetUserBasicInfoAsync(dto.Email);

                var loginResponse = new LoginResponseDTO
                {
                    SessionID = sessionId,
                    Message = message ?? "Login exitoso.",
                    UserID = userInfo?.UserID ?? 0,
                    Email = userInfo?.Email ?? dto.Email,
                    Name = userInfo?.Name ?? string.Empty
                };

                return ApiResponseDTO.SuccessResponse(message ?? "Login exitoso.", loginResponse);
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Validate Session

        /// <summary>
        /// Valida y refresca una sesi�n (sliding expiration)
        /// SP: app.sp_ValidateAndRefreshSession
        /// Usado por: AuthenticationMiddleware
        /// </summary>
        public async Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@SessionID", sessionId),
                    new SqlParameter("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@UserID", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                var (success, _, outputValues) = await _db.ExecuteStoredProcedureWithOutputAsync(
                    "app.sp_ValidateAndRefreshSession",
                    parameters
                );

                var isValid = outputValues.ContainsKey("@IsValid") && outputValues["@IsValid"] != null
                    ? Convert.ToBoolean(outputValues["@IsValid"])
                    : false;

                var userId = outputValues.ContainsKey("@UserID") && outputValues["@UserID"] != null
                    ? Convert.ToInt32(outputValues["@UserID"])
                    : 0;

                return new SessionValidationDTO
                {
                    IsValid = isValid,
                    UserID = userId
                };
            }
            catch
            {
                return new SessionValidationDTO { IsValid = false, UserID = 0 };
            }
        }

        #endregion

        #region Logout

        /// <summary>
        /// Cierra una sesi�n espec�fica
        /// SP: app.sp_Logout
        /// Feature 1.1 - Cierre de sesi�n
        /// </summary>
        public async Task<ApiResponseDTO> LogoutAsync(Guid sessionId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@SessionID", sessionId),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_Logout",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al cerrar sesi�n: {ex.Message}");
            }
        }

        /// <summary>
        /// Cierra todas las sesiones activas de un usuario
        /// SP: app.sp_LogoutAllSessions
        /// Feature 1.1 - Cierre de sesi�n global
        /// </summary>
        public async Task<ApiResponseDTO> LogoutAllAsync(int userId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", userId),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_LogoutAllSessions",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al cerrar sesiones: {ex.Message}");
            }
        }

        #endregion

        #region Password Reset

        /// <summary>
        /// Solicita restablecimiento de contrase�a
        /// SP: app.sp_RequestPasswordReset
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// </summary>
        public async Task<ApiResponseDTO> RequestPasswordResetAsync(RequestPasswordResetDTO dto, string? sourceIp = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@Token", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output },
                    new SqlParameter("@ExpiresAt", SqlDbType.DateTime2) { Direction = ParameterDirection.Output }
                };

                var (success, errorMessage, outputValues) = await _db.ExecuteStoredProcedureWithOutputAsync(
                    "app.sp_RequestPasswordReset",
                    parameters
                );

                // Por seguridad, siempre devolvemos mensaje gen�rico
                // (no revelar si el email existe o no)
                var response = new PasswordResetRequestResponseDTO
                {
                    Token = outputValues.ContainsKey("@Token") && outputValues["@Token"] != null
                        ? outputValues["@Token"].ToString() ?? string.Empty
                        : string.Empty,
                    ExpiresAt = outputValues.ContainsKey("@ExpiresAt") && outputValues["@ExpiresAt"] != null
                        ? Convert.ToDateTime(outputValues["@ExpiresAt"])
                        : DateTime.UtcNow.AddHours(1),
                    Message = "Si el correo existe, se ha enviado un enlace de restablecimiento."
                };

                return ApiResponseDTO.SuccessResponse(response.Message, response);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al solicitar restablecimiento: {ex.Message}");
            }
        }

        /// <summary>
        /// Restablece contrase�a con token
        /// SP: app.sp_ResetPasswordWithToken
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// </summary>
        public async Task<ApiResponseDTO> ResetPasswordWithTokenAsync(ResetPasswordWithTokenDTO dto, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validaci�n de complejidad de contrase�a
                var passwordErrors = ValidatePasswordComplexity(dto.NewPassword);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Token", dto.Token),
                    new SqlParameter("@NewPassword", dto.NewPassword),
                    new SqlParameter("@ConfirmPassword", dto.ConfirmPassword),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
            new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                await _db.ExecuteStoredProcedureNonQueryAsync(
                    "app.sp_ResetPasswordWithToken",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse("Contrase�a restablecida exitosamente.");
            }
            catch (SqlException ex)
            {
                // Errores espec�ficos del SP (token inv�lido, expirado, etc.)
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al restablecer contrase�a: {ex.Message}");
            }
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Valida complejidad de contrase�a seg�n reglas del sistema
        /// Reglas: 8-12 caracteres, alfanum�rica, al menos 1 may�scula, 1 min�scula, 1 d�gito
        /// </summary>
        public List<string> ValidatePasswordComplexity(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("La contrase�a es obligatoria.");
                return errors;
            }

            if (password.Length < 8 || password.Length > 12)
            {
                errors.Add("La contrase�a debe tener entre 8 y 12 caracteres.");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("La contrase�a debe incluir al menos una letra may�scula.");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("La contrase�a debe incluir al menos una letra min�scula.");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("La contrase�a debe incluir al menos un d�gito.");
            }

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
            {
                errors.Add("La contrase�a debe ser alfanum�rica (solo letras y n�meros, sin caracteres especiales).");
            }

            return errors;
        }

        /// <summary>
        /// Valida formato de email
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Regex b�sico para validaci�n de email
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene informaci�n b�sica de un usuario por email (helper interno)
        /// </summary>
        private async Task<UserProfileBasicDTO?> GetUserBasicInfoAsync(string email)
        {
            try
            {
                var users = await _db.ExecuteViewAsync<UserProfileBasicDTO>(
                    "auth.UserAccount",
                    reader => new UserProfileBasicDTO
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias"),
                        LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode"),
                        ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                        AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                    },
                    whereClause: $"Email = '{email}'"
                );

                return users.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}