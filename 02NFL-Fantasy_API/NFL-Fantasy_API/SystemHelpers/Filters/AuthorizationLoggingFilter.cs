using Microsoft.AspNetCore.Mvc.Filters;

namespace NFL_Fantasy_API.Helpers.Filters
{
    /// <summary>
    /// ActionFilter que logea automáticamente información de autenticación en endpoints protegidos.
    /// 
    /// PROPÓSITO:
    /// - Auditoría automática de accesos
    /// - Detectar patrones sospechosos
    /// - Facilitar troubleshooting
    /// 
    /// OPCIONAL: Puedes aplicarlo solo en endpoints críticos con [ServiceFilter]
    /// </summary>
    public class AuthorizationLoggingFilter : IActionFilter
    {
        private readonly ILogger<AuthorizationLoggingFilter> _logger;

        public AuthorizationLoggingFilter(ILogger<AuthorizationLoggingFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Solo logear en endpoints que requieren autenticación
            var endpoint = context.HttpContext.GetEndpoint();
            var requiresAuth = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>() != null;

            if (requiresAuth && context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.HttpContext.Items["UserID"];
                var sessionId = context.HttpContext.Items["SessionID"];
                var action = context.ActionDescriptor.DisplayName;

                _logger.LogInformation(
                    "Authenticated request: User={UserID}, Session={SessionID}, Action={Action}",
                    userId,
                    sessionId,
                    action
                );
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No se usa
        }
    }
}