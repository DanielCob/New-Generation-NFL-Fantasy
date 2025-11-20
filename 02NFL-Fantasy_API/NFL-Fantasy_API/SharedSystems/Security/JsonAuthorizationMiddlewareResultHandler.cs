using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace NFL_Fantasy_API.SharedSystems.Security
{
    /// <summary>
    /// ============================================
    /// MANEJADOR DE RESULTADOS DE AUTORIZACIÓN
    /// ============================================
    /// 
    /// Formatea las respuestas JSON cuando falla la autorización
    /// usando atributos [Authorize] o policies de ASP.NET Core.
    /// 
    /// Diferencia entre:
    /// - 401 Unauthorized: Falta autenticación (¿Quién eres?)
    /// - 403 Forbidden: Falta autorización (No tienes permiso)
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
            // ========================================
            // CHALLENGED: No autenticado
            // ========================================
            if (authorizeResult.Challenged)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Token de autenticación requerido o inválido."
                });
                return;
            }

            // ========================================
            // FORBIDDEN: Autenticado pero sin permisos
            // ========================================
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Acceso denegado. No tiene permisos suficientes para esta operación."
                });
                return;
            }

            // Usuario autorizado, continuar normalmente
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}