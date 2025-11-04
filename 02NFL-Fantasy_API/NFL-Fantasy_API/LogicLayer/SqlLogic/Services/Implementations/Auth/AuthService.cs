using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Auth;
using NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Interfaces.Email;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.SharedSystems.EmailConfig;
using NFL_Fantasy_API.SharedSystems.Validators;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Auth
{
    /// <summary>
    /// Implementación del servicio de autenticación.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a AuthDataAccess).
    /// NO valida directamente (delegado a Validators).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AuthDataAccess _dataAccess;
        private readonly IEmailSender _email;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AuthDataAccess dataAccess,
            IConfiguration configuration,
            IEmailSender email,
            ILogger<AuthService> logger)
        {
            _dataAccess = dataAccess;
            _email = email;
            _config = configuration;
            _logger = logger;
        }

        #region Register

        public async Task<ApiResponseDTO> RegisterAsync(
            RegisterUserDTO dto,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a PasswordValidator
                var passwordErrors = AuthPasswordValidator.ValidateComplexity(dto.Password);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                // VALIDACIÓN: Delegada a EmailValidator
                if (!EmailValidator.IsValid(dto.Email))
                {
                    return ApiResponseDTO.ErrorResponse("Formato de correo electronico invalido.");
                }

                // VALIDACIÓN: Lógica de negocio
                if (dto.ProfileImageBytes.HasValue)
                {
                    if (!dto.ProfileImageWidth.HasValue || !dto.ProfileImageHeight.HasValue)
                    {
                        return ApiResponseDTO.ErrorResponse(
                            "Si proporciona tamano de imagen, debe incluir ancho y alto.");
                    }
                }

                // EJECUCIÓN: Delegada a DataAccess (construcción de parámetros)
                var result = await _dataAccess.RegisterUserAsync(dto, sourceIp, userAgent);

                if (result != null)
                {
                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al registrar usuario.");
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

        #region Login

        public async Task<ApiResponseDTO> LoginAsync(
            LoginDTO dto,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.LoginAsync(dto, sourceIp, userAgent);

                if (!result.Success || result.SessionId == Guid.Empty)
                {
                    return ApiResponseDTO.ErrorResponse(result.Message ?? "Credenciales invalidas.");
                }

                // LÓGICA DE NEGOCIO: Obtener info adicional del usuario
                var userInfo = await _dataAccess.GetUserBasicInfoAsync(dto.Email);

                var loginResponse = new LoginResponseDTO
                {
                    SessionID = result.SessionId,
                    Message = result.Message ?? "Login exitoso.",
                    UserID = userInfo?.UserID ?? 0,
                    Email = userInfo?.Email ?? dto.Email,
                    Name = userInfo?.Name ?? string.Empty,
                    SystemRoleCode = userInfo?.SystemRoleCode ?? "USER"
                };

                return ApiResponseDTO.SuccessResponse(result.Message ?? "Login exitoso.", loginResponse);
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

        public async Task<SessionValidationDTO> ValidateSessionAsync(Guid sessionId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.ValidateSessionAsync(sessionId);
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region Logout

        public async Task<ApiResponseDTO> LogoutAsync(
            Guid sessionId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.LogoutAsync(sessionId, sourceIp, userAgent);
                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al cerrar sesion: {ex.Message}");
            }
        }

        public async Task<ApiResponseDTO> LogoutAllAsync(
            int userId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.LogoutAllAsync(userId, sourceIp, userAgent);
                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al cerrar sesiones: {ex.Message}");
            }
        }

        #endregion

        #region Password Reset

        public async Task<ApiResponseDTO> RequestPasswordResetAsync(
            RequestPasswordResetDTO dto,
            string? sourceIp = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.RequestPasswordResetAsync(dto, sourceIp);

                // LÓGICA DE NEGOCIO: Envío de email
                if (!string.IsNullOrWhiteSpace(result.Token) && result.ExpiresAt.HasValue)
                {
                    var baseUrl = _config["Frontend:ResetPasswordUrl"];
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        baseUrl = "https://example.com/reset-password";
                        _logger.LogWarning(
                            "Frontend:ResetPasswordUrl no esta configurado; usando fallback {Url}",
                            baseUrl);
                    }

                    var resetUrl = $"{baseUrl}?token={Uri.EscapeDataString(result.Token)}";
                    var appName = _config["Application:Name"] ?? "X-NFL Fantasy API";

                    var html = EmailTemplates.PasswordReset(appName, resetUrl, result.ExpiresAt.Value);

                    await _email.SendAsync(dto.Email, $"{appName} - Restablecimiento de contrasena", html);

                    _logger.LogInformation(
                        "Reset solicitado para {Email}. Token generado y enviado por correo.",
                        dto.Email);
                }
                else
                {
                    _logger.LogInformation(
                        "Reset solicitado para {Email}. No se genero token (posible email inexistente).",
                        dto.Email);
                }

                return ApiResponseDTO.SuccessResponse(
                    "Si el correo existe, se ha enviado un enlace de restablecimiento.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar restablecimiento para {Email}", dto.Email);
                return ApiResponseDTO.ErrorResponse($"Error al solicitar restablecimiento: {ex.Message}");
            }
        }

        public async Task<ApiResponseDTO> ResetPasswordWithTokenAsync(
            ResetPasswordWithTokenDTO dto,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a PasswordValidator
                var passwordErrors = AuthPasswordValidator.ValidateComplexity(dto.NewPassword);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                await _dataAccess.ResetPasswordWithTokenAsync(dto, sourceIp, userAgent);

                return ApiResponseDTO.SuccessResponse("Contrasena restablecida exitosamente.");
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al restablecer contrasena: {ex.Message}");
            }
        }

        #endregion
    }
}