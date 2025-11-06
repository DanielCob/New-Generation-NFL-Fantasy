using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Helpers.Extensions
{
    /// <summary>
    /// Extensiones de conveniencia para ControllerBase
    /// IMPORTANTE: Estos métodos son ATAJOS que delegan a HttpContextExtensions
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Atajo para obtener UserID del contexto actual
        /// </summary>
        public static int UserId(this ControllerBase controller)
            => controller.HttpContext.GetUserId();

        /// <summary>
        /// Atajo para obtener SessionID del contexto actual
        /// </summary>
        public static Guid SessionId(this ControllerBase controller)
            => controller.HttpContext.GetSessionId();

        /// <summary>
        /// Atajo para verificar autenticación
        /// </summary>
        public static bool IsAuthenticated(this ControllerBase controller)
            => controller.HttpContext.IsAuthenticated();

        /// <summary>
        /// Atajo para obtener la IP del cliente (método robusto)
        /// </summary>
        public static string ClientIp(this ControllerBase controller)
            => controller.HttpContext.GetClientIpAddress();

        /// <summary>
        /// Atajo para obtener User-Agent
        /// </summary>
        public static string UserAgent(this ControllerBase controller)
            => controller.HttpContext.GetUserAgent();

        /// <summary>
        /// Atajo para verificar ownership
        /// </summary>
        public static bool IsOwner(this ControllerBase controller, int targetUserId)
            => controller.HttpContext.IsOwner(targetUserId);
    }
}