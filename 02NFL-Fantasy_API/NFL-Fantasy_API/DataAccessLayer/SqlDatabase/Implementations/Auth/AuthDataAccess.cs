using Microsoft.Data.SqlClient;
using System.Data;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Auth
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de autenticación.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs.
    /// NO contiene lógica de negocio ni validaciones.
    /// </summary>
    public class AuthDataAccess
    {
        private readonly IDatabaseHelper _db;

        public AuthDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Register

        public async Task<RegisterResponseDTO?> RegisterUserAsync(
            RegisterUserDTO dto,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@Name", dto.Name),
                SqlParameterExtensions.CreateParameter("@Email", dto.Email),
                SqlParameterExtensions.CreateParameter("@Alias", dto.Alias),
                SqlParameterExtensions.CreateParameter("@Password", dto.Password),
                SqlParameterExtensions.CreateParameter("@PasswordConfirm", dto.PasswordConfirm),
                SqlParameterExtensions.CreateParameter("@LanguageCode", dto.LanguageCode ?? "en"),
                SqlParameterExtensions.CreateParameter("@ProfileImageUrl", dto.ProfileImageUrl),
                SqlParameterExtensions.CreateParameter("@ProfileImageWidth", dto.ProfileImageWidth),
                SqlParameterExtensions.CreateParameter("@ProfileImageHeight", dto.ProfileImageHeight),
                SqlParameterExtensions.CreateParameter("@ProfileImageBytes", dto.ProfileImageBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_RegisterUser",
                parameters,
                reader => new RegisterResponseDTO
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        #endregion

        #region Login

        public async Task<LoginResultDataAccess> LoginAsync(
            LoginDTO dto,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@Email", dto.Email),
                SqlParameterExtensions.CreateParameter("@Password", dto.Password),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent),
                SqlParameterExtensions.CreateOutputParameter("@SessionID", SqlDbType.UniqueIdentifier),
                SqlParameterExtensions.CreateOutputParameter("@Message", SqlDbType.NVarChar, 200)
            };

            var (success, errorMessage, outputValues) = await _db.ExecuteStoredProcedureWithOutputAsync(
                "app.sp_Login",
                parameters
            );

            var sessionId = outputValues.ContainsKey("@SessionID") && outputValues["@SessionID"] != null
                ? (Guid)outputValues["@SessionID"]
                : Guid.Empty;

            var message = outputValues.ContainsKey("@Message") && outputValues["@Message"] != null
                ? outputValues["@Message"].ToString()
                : success ? "Login exitoso." : errorMessage ?? "Error desconocido.";

            return new LoginResultDataAccess
            {
                Success = success,
                SessionId = sessionId,
                Message = message
            };
        }

        #endregion

        #region Validate Session

        public async Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@SessionID", sessionId),
                SqlParameterExtensions.CreateOutputParameter("@IsValid", SqlDbType.Bit),
                SqlParameterExtensions.CreateOutputParameter("@UserID", SqlDbType.Int)
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

        #endregion

        #region Logout

        public async Task<string> LogoutAsync(Guid sessionId, string? sourceIp, string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@SessionID", sessionId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_Logout",
                parameters
            );
        }

        public async Task<string> LogoutAllAsync(int userId, string? sourceIp, string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", userId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_LogoutAllSessions",
                parameters
            );
        }

        #endregion

        #region Password Reset

        public async Task<PasswordResetTokenResult> RequestPasswordResetAsync(
            RequestPasswordResetDTO dto,
            string? sourceIp)
        {
            var pEmail = SqlParameterExtensions.CreateParameter("@Email", dto.Email);
            var pSourceIp = SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp);
            var pToken = SqlParameterExtensions.CreateOutputParameter("@Token", SqlDbType.NVarChar, 100);
            var pExpiresAt = SqlParameterExtensions.CreateOutputParameter("@ExpiresAt", SqlDbType.DateTime2);

            await _db.ExecuteStoredProcedureWithOutputAsync(
                "app.sp_RequestPasswordReset",
                new[] { pEmail, pSourceIp, pToken, pExpiresAt }
            );

            var token = pToken.Value == DBNull.Value ? null : pToken.Value?.ToString();
            var expiresAtUtc = pExpiresAt.Value == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(pExpiresAt.Value);

            return new PasswordResetTokenResult
            {
                Token = token,
                ExpiresAt = expiresAtUtc
            };
        }

        public async Task ResetPasswordWithTokenAsync(
            ResetPasswordWithTokenDTO dto,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@Token", dto.Token),
                SqlParameterExtensions.CreateParameter("@NewPassword", dto.NewPassword),
                SqlParameterExtensions.CreateParameter("@ConfirmPassword", dto.ConfirmPassword),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            await _db.ExecuteStoredProcedureNonQueryAsync(
                "app.sp_ResetPasswordWithToken",
                parameters
            );
        }

        #endregion

        #region Helper Methods

        public async Task<UserProfileBasicDTO?> GetUserBasicInfoAsync(string email)
        {
            var users = await _db.ExecuteViewAsync(
                "vw_UserProfileHeader",
                reader => new UserProfileBasicDTO
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    Email = reader.GetSafeString("Email"),
                    Name = reader.GetSafeString("Name"),
                    Alias = reader.GetSafeNullableString("Alias"),
                    LanguageCode = reader.GetSafeString("LanguageCode"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    AccountStatus = reader.GetSafeByte("AccountStatus"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    SystemRoleCode = reader.GetSafeString("SystemRoleCode")
                },
                whereClause: $"Email = '{email.Replace("'", "''")}'"
            );

            return users.FirstOrDefault();
        }

        #endregion
    }
}