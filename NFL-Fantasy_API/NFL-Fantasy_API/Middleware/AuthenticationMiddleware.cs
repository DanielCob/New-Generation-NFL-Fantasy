// Middleware/AuthenticationMiddleware.cs
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AuthenticationMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for certain paths
            if (ShouldSkipAuthentication(context))
            {
                await _next(context);
                return;
            }

            // Check if token is provided in headers
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing Authorization header");
                return;
            }

            var token = authHeader.ToString();

            // Remove "Bearer " prefix if present
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring(7);
            }

            // Validate GUID format
            if (!Guid.TryParse(token, out var sessionToken))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token format");
                return;
            }

            // Validate token using AuthService
            using var scope = _serviceScopeFactory.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var (isValid, userId, userType) = await authService.ValidateSessionTokenAsync(sessionToken);

            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired token");
                return;
            }

            // Add user information to HttpContext for use in controllers
            context.Items["UserId"] = userId;
            context.Items["UserType"] = userType;
            context.Items["SessionToken"] = sessionToken;

            // Check role-based access
            if (!HasRequiredRole(context, userType))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Insufficient permissions");
                return;
            }

            await _next(context);
        }

        private static bool ShouldSkipAuthentication(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method.ToUpper();

            // SIEMPRE permitir OPTIONS para CORS
            if (method == "OPTIONS")
                return true;

            // Endpoints completamente públicos (POST para registro, GET para ubicaciones)
            var publicEndpoints = new[]
            {
                "/api/auth/login",           // Login
                "/api/user/clients",        // Crear cliente
                "/api/user/engineers",      // Crear ingeniero 
            };

            // Permitir todos los métodos en estos endpoints
            if (publicEndpoints.Any(endpoint => path.StartsWith(endpoint)))
                return true;

            // Endpoints de ubicaciones - solo GET público
            var locationEndpoints = new[]
            {
                "/api/location/provinces",
                "/api/location/cantons",
                "/api/location/districts"
            };

            if (locationEndpoints.Any(endpoint => path.StartsWith(endpoint)) && method == "GET")
                return true;

            // Swagger y documentación
            if (path.Contains("/swagger") ||
                path.StartsWith("/swagger-ui") ||
                path.Contains("swagger.json") ||
                path.StartsWith("/_vs/") ||
                path.StartsWith("/_framework/"))
                return true;

            // Archivos estáticos
            if (path.Contains(".css") ||
                path.Contains(".js") ||
                path.Contains(".ico") ||
                path.Contains(".png") ||
                path.Contains(".jpg"))
                return true;

            return false;
        }

        private static bool HasRequiredRole(HttpContext context, string? userType)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Admin-only endpoints
            if (path.StartsWith("/api/admin/") ||
                path.StartsWith("/api/auth/reset-password") ||
                path.StartsWith("/api/views/"))
            {
                return userType == "ADMIN";
            }

            // Any authenticated user can access these
            if (path.StartsWith("/api/auth/logout") ||
                path.StartsWith("/api/auth/change-password") ||
                path.StartsWith("/api/users/") ||
                path.StartsWith("/api/location/"))
            {
                return true;
            }

            return true; // Default allow for authenticated users
        }
    }

    // Extension method for easier registration
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}