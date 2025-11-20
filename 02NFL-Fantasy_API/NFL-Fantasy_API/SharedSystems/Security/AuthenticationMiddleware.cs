using System.Security.Claims;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.Auth;

namespace NFL_Fantasy_API.SharedSystems.Security
{
    /// <summary>
    /// ============================================
    /// MIDDLEWARE DE AUTENTICACIÓN (Authentication)
    /// ============================================
    /// 
    /// RESPONSABILIDAD: Determinar "¿QUIÉN eres?"
    /// 
    /// Este middleware se encarga ÚNICAMENTE de:
    /// 1. Verificar si existe un token Bearer válido
    /// 2. Validar la sesión contra el AuthService
    /// 3. Establecer la identidad del usuario (ClaimsPrincipal)
    /// 4. Guardar información del usuario en HttpContext
    /// 
    /// NO maneja permisos ni acceso a recursos específicos.
    /// Eso es responsabilidad del AuthorizationMiddleware.
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService, IUserService userService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // Verificar si la ruta requiere autenticación
            if (RouteConfiguration.IsPublicRoute(path, method))
            {
                _logger.LogDebug("Public route accessed: {Method} {Path}", method, path);
                await _next(context);
                return;
            }

            // ========================================
            // PASO 1: Extraer y validar formato del token
            // ========================================
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Missing or invalid Authorization header for {Path}", path);
                await RespondUnauthenticated(context, "Token de autenticación requerido. Incluya 'Authorization: Bearer {SessionID}' en el header.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!Guid.TryParse(token, out Guid sessionId))
            {
                _logger.LogWarning("Invalid token format: {Token}", token);
                await RespondUnauthenticated(context, "Formato de token inválido. Debe ser un GUID válido.");
                return;
            }

            // ========================================
            // PASO 2: Validar sesión activa
            // ========================================
            try
            {
                var validation = await authService.ValidateSessionAsync(sessionId);
                if (!validation.IsValid || validation.UserID <= 0)
                {
                    _logger.LogWarning("Invalid or expired session: {SessionID}", sessionId);
                    await RespondUnauthenticated(context, "Sesión inválida o expirada. Por favor, inicie sesión nuevamente.");
                    return;
                }

                // ========================================
                // PASO 3: Obtener información del usuario
                // ========================================
                var userBasicInfo = await userService.GetUserBasicAsync(validation.UserID);
                var userRole = userBasicInfo?.SystemRoleCode ?? "USER";
                var userEmail = userBasicInfo?.Email ?? string.Empty;

                // ========================================
                // PASO 4: Establecer identidad (ClaimsPrincipal)
                // ========================================
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, validation.UserID.ToString()),
                    new(ClaimTypes.Role, userRole),
                    new("SessionID", sessionId.ToString())
                };

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    claims.Add(new(ClaimTypes.Email, userEmail));
                }

                var identity = new ClaimsIdentity(claims, authenticationType: "Session");
                context.User = new ClaimsPrincipal(identity);

                // ========================================
                // PASO 5: Guardar datos en HttpContext.Items
                // (Para compatibilidad con código legacy)
                // ========================================
                context.Items["UserID"] = validation.UserID;
                context.Items["SessionID"] = sessionId;
                context.Items["IsAuthenticated"] = true;
                context.Items["SystemRoleCode"] = userRole;

                _logger.LogInformation(
                    "User authenticated successfully - UserID: {UserID}, Role: {Role}, Path: {Path}",
                    validation.UserID, userRole, path
                );

                // Usuario autenticado, continuar con el pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for route {Path}", path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Error interno al validar autenticación."
                });
            }
        }

        /// <summary>
        /// Responde con 401 Unauthorized y mensaje descriptivo
        /// </summary>
        private static async Task RespondUnauthenticated(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message
            });
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware de autenticación
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<AuthenticationMiddleware>();
    }
}