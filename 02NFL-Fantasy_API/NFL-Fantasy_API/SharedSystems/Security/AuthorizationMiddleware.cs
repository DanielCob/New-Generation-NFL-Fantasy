using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.SharedSystems.Security
{
    /// <summary>
    /// ============================================
    /// MIDDLEWARE DE AUTORIZACIÓN (Authorization)
    /// ============================================
    /// 
    /// RESPONSABILIDAD: Determinar "¿QUÉ puedes hacer?"
    /// 
    /// Este middleware se encarga de:
    /// 1. Verificar permisos basados en roles (ADMIN vs USER)
    /// 2. Controlar acceso a recursos específicos según rutas
    /// 3. Aplicar reglas de negocio de autorización
    /// 
    /// IMPORTANTE: Este middleware debe ejecutarse DESPUÉS del
    /// AuthenticationMiddleware, ya que requiere que el usuario
    /// ya esté autenticado y tenga un ClaimsPrincipal establecido.
    /// </summary>
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationMiddleware> _logger;

        public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // Solo aplicar autorización si el usuario está autenticado
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                // Si llegamos aquí sin autenticación, algo falló
                // (normalmente el AuthenticationMiddleware ya lo habría manejado)
                await _next(context);
                return;
            }

            // Obtener rol del usuario autenticado
            var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "USER";
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            // ========================================
            // VERIFICAR SI LA RUTA REQUIERE ROL ADMIN
            // ========================================
            if (RouteConfiguration.RequiresAdminRole(path, method))
            {
                if (!string.Equals(userRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Access denied - User {UserID} with role {Role} attempted to access admin route: {Method} {Path}",
                        userId, userRole, method, path
                    );

                    await RespondForbidden(context, "Acceso denegado. Esta operación requiere rol de ADMINISTRADOR.");
                    return;
                }

                _logger.LogInformation(
                    "Admin access granted - User {UserID} accessing: {Method} {Path}",
                    userId, method, path
                );
            }

            // Usuario autorizado, continuar con el pipeline
            await _next(context);
        }

        /// <summary>
        /// Responde con 403 Forbidden y mensaje descriptivo
        /// </summary>
        private static async Task RespondForbidden(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message
            });
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware de autorización
    /// </summary>
    public static class AuthorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<AuthorizationMiddleware>();
    }
}