using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace NFL_Fantasy_API.SharedSystems.Middleware
{
    /// <summary>
    /// Formatea las respuestas de autorización:
    /// - 401: Token requerido o inválido
    /// - 403: Requiere rol ADMIN (u otra policy)
    /// </summary>
    public class JsonAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Challenged)
            {
                // No autenticado (no hubo usuario válido)
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Token de autenticación requerido o inválido."
                });
                return;
            }

            if (authorizeResult.Forbidden)
            {
                // Autenticado pero sin permisos (policy falló, p.ej. AdminOnly)
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Acceso denegado. Requiere rol ADMIN."
                });
                return;
            }

            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
