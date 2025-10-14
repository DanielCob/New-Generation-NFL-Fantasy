using System.Text.RegularExpressions;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Middleware
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

        // Rutas públicas que NO requieren autenticación
        private static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/request-reset",
            "/api/auth/reset-with-token",
            "/api/reference/current-season",
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

        // Rutas que requieren rol ADMIN (futuro si implementas roles)
        // Por ahora, /api/views/* está abierto a cualquier usuario autenticado
        // Puedes agregar validación de roles en futuras features
        private static readonly List<Regex> AdminOnlyPatterns = new()
        {
            // Ejemplo: new Regex(@"^/api/admin/.*", RegexOptions.IgnoreCase)
            // Por ahora vacío; todos los autenticados pueden acceder a todo
        };

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // 1. Verificar si la ruta es pública
            if (ShouldSkipAuthentication(path, method))
            {
                await _next(context);
                return;
            }

            // 2. Extraer token Bearer del header Authorization
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
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

            // 3. Validar que el token sea un GUID válido
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

            // 4. Validar sesión con el servicio (también refresca sliding expiration)
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

                // 5. Sesión válida: agregar información al contexto para uso en controllers
                context.Items["UserID"] = validation.UserID;
                context.Items["SessionID"] = sessionId;
                context.Items["IsAuthenticated"] = true;

                _logger.LogInformation("User {UserID} authenticated successfully for {Method} {Path}",
                    validation.UserID, method, path);

                // 6. Verificar permisos ADMIN si la ruta lo requiere (futuro)
                if (RequiresAdminRole(path))
                {
                    // Por ahora no tenemos sistema de roles en UserAccount
                    // En el futuro, puedes agregar un campo Role en auth.UserAccount
                    // y verificar aquí si el usuario tiene rol ADMIN

                    // Ejemplo futuro:
                    // var userRole = await GetUserRole(validation.UserID);
                    // if (userRole != "ADMIN")
                    // {
                    //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    //     await context.Response.WriteAsJsonAsync(new { success = false, message = "Acceso denegado. Requiere rol de administrador." });
                    //     return;
                    // }
                }

                // 7. Continuar al siguiente middleware/controller
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for route {Path}", path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Error interno al validar autenticación."
                });
            }
        }

        /// <summary>
        /// Determina si una ruta debe omitir la autenticación
        /// </summary>
        private bool ShouldSkipAuthentication(string path, string method)
        {
            // Rutas explícitamente públicas
            if (PublicRoutes.Contains(path))
            {
                return true;
            }

            // Patrones GET públicos (reference data, swagger, etc.)
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var pattern in PublicGetPatterns)
                {
                    if (pattern.IsMatch(path))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determina si una ruta requiere rol ADMIN
        /// Por ahora retorna false; implementar cuando se agreguen roles a UserAccount
        /// </summary>
        private bool RequiresAdminRole(string path)
        {
            foreach (var pattern in AdminOnlyPatterns)
            {
                if (pattern.IsMatch(path))
                {
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
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}