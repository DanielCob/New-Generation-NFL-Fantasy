using System.Text.RegularExpressions;
using System.Security.Claims;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;

namespace NFL_Fantasy_API.SharedSystems.Middleware
{
    /// <summary>
    /// Middleware de autenticación basado en Bearer token (SessionID GUID)
    /// Valida y refresca sesiones automáticamente (sliding expiration)
    /// Aplica control de acceso basado en rutas
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

        // Rutas públicas que NO requieren autenticación
        private static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/request-reset",
            "/api/auth/reset-with-token",
            "/api/seasons/current",
            "/api/reference/position-formats",
            "/api/scoring/schemas"
        };

        // Patrones regex para rutas públicas GET
        private static readonly List<Regex> PublicGetPatterns = new()
        {
            new Regex(@"^/api/reference/position-formats/\d+/slots$", RegexOptions.IgnoreCase),
            new Regex(@"^/api/scoring/schemas/\d+/rules$", RegexOptions.IgnoreCase),
            new Regex(@"^/swagger.*", RegexOptions.IgnoreCase),
            new Regex(@"^/$", RegexOptions.IgnoreCase) // root
        };

        // Determina si la ruta requiere ADMIN (según path + método HTTP)
        private static bool RequiresAdminRole(string path, string method)
        {
            // Mutaciones de NFLTeam (POST/PUT/* que no sean GET)
            if (Regex.IsMatch(path, @"^/api/nflteam($|/)", RegexOptions.IgnoreCase) &&
                !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                return true;

            // Gestión de roles del sistema (cualquier verbo)
            if (Regex.IsMatch(path, @"^/api/system-roles($|/)", RegexOptions.IgnoreCase))
                return true;

            // (Opcional) vistas administrativas
            if (Regex.IsMatch(path, @"^/api/views/.*", RegexOptions.IgnoreCase))
                return true;

            // Administración de temporadas: TODO menos GET /api/seasons/current
            if (Regex.IsMatch(path, @"^/api/seasons($|/)", RegexOptions.IgnoreCase))
            {
                if (Regex.IsMatch(path, @"^/api/seasons/current$", RegexOptions.IgnoreCase))
                    return false; // público

                return true; // resto requiere ADMIN
            }

            return false;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService, IUserService userService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // 1) Rutas públicas
            if (ShouldSkipAuthentication(path, method))
            {
                await _next(context);
                return;
            }

            // 2) Header Authorization: Bearer {GUID}
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Request to protected route {Path} without valid Authorization header", path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Token de autenticación requerido. Incluya 'Authorization: Bearer {SessionID}' en el header."
                });
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (!Guid.TryParse(token, out Guid sessionId))
            {
                _logger.LogWarning("Invalid token format for route {Path}: {Token}", path, token);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Formato de token inválido. Debe ser un GUID válido."
                });
                return;
            }

            // 3) Validar sesión (y refrescar sliding expiration)
            try
            {
                var validation = await authService.ValidateSessionAsync(sessionId);
                if (!validation.IsValid || validation.UserID <= 0)
                {
                    _logger.LogWarning("Invalid or expired session {SessionID} for route {Path}", sessionId, path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Sesión inválida o expirada. Por favor, inicie sesión nuevamente."
                    });
                    return;
                }

                // 4) Cargar rol del usuario para Claims/Policies
                var basic = await userService.GetUserBasicAsync(validation.UserID);
                var role = basic?.SystemRoleCode ?? "USER";
                var email = basic?.Email ?? string.Empty;

                // 5) Guardar info en HttpContext.Items (compatibilidad con tu código)
                context.Items["UserID"] = validation.UserID;
                context.Items["SessionID"] = sessionId;
                context.Items["IsAuthenticated"] = true;
                context.Items["SystemRoleCode"] = role;

                // 6) Construir ClaimsPrincipal para [Authorize] y Policies
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, validation.UserID.ToString()),
                    new(ClaimTypes.Role, role)
                };
                if (!string.IsNullOrWhiteSpace(email))
                    claims.Add(new(ClaimTypes.Email, email));

                var identity = new ClaimsIdentity(claims, authenticationType: "Session");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogInformation("User {UserID} ({Role}) authenticated for {Method} {Path}", validation.UserID, role, method, path);

                // 7) Enforzar ADMIN cuando corresponda
                if (RequiresAdminRole(path, method) && !string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Acceso denegado. Requiere rol ADMIN."
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                // Si algo falla en la etapa de autenticación, responder aquí
                _logger.LogError(ex, "Error during authentication for route {Path}", path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Error interno al validar autenticación."
                });
                return;
            }

            // 8) Continuar con el pipeline. Cualquier excepción de los controllers/servicios
            // ya NO será atrapada por este middleware (no se enmascara).
            await _next(context);
        }

        /// <summary>
        /// Determina si una ruta debe omitir la autenticación.
        /// </summary>
        private static bool ShouldSkipAuthentication(string path, string method)
        {
            // Rutas explícitamente públicas
            if (PublicRoutes.Contains(path))
                return true;

            // Patrones GET públicos (reference data, swagger, etc.)
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pattern in PublicGetPatterns)
                {
                    if (pattern.IsMatch(path))
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware fácilmente en Program.cs
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
