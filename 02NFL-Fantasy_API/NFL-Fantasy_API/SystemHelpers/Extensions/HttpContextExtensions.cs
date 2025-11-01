using System.Security.Claims;

namespace NFL_Fantasy_API.Helpers.Extensions
{
    /// <summary>
    /// Extensiones para HttpContext - ÚNICA fuente de verdad para datos del contexto
    /// </summary>
    public static class HttpContextExtensions
    {
        #region User Authentication & Identity

        /// <summary>
        /// Obtiene el UserID del usuario autenticado desde el contexto
        /// </summary>
        public static int GetUserId(this HttpContext context)
        {
            // Primero de Items (middleware)
            if (context.Items.TryGetValue("UserID", out var userIdObj) && userIdObj is int userId && userId > 0)
            {
                return userId;
            }

            // Fallback desde Claims
            var claim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtiene el SessionID del usuario autenticado desde el contexto
        /// </summary>
        public static Guid GetSessionId(this HttpContext context)
        {
            if (context.Items.TryGetValue("SessionID", out var sessionIdObj) && sessionIdObj is Guid sessionId)
            {
                return sessionId;
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Verifica si el usuario está autenticado
        /// </summary>
        public static bool IsAuthenticated(this HttpContext context)
        {
            if (context.Items.TryGetValue("IsAuthenticated", out var isAuthObj) && isAuthObj is bool isAuth)
            {
                return isAuth;
            }
            return false;
        }

        /// <summary>
        /// Verifica si el UserID del contexto coincide con un targetUserId
        /// Útil para validar que un usuario solo pueda modificar su propio perfil
        /// </summary>
        public static bool IsOwner(this HttpContext context, int targetUserId)
        {
            return context.GetUserId() == targetUserId;
        }

        #endregion

        #region Client Information

        /// <summary>
        /// Obtiene la IP del cliente, considerando proxies (X-Forwarded-For)
        /// MÉTODO ROBUSTO - Usar este siempre
        /// </summary>
        public static string GetClientIpAddress(this HttpContext context)
        {
            // Verificar si viene de proxy/load balancer
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            // Verificar X-Real-IP (nginx)
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback a RemoteIpAddress
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    return remoteIp.MapToIPv4().ToString();
                }
                return remoteIp.ToString();
            }

            return "unknown";
        }

        /// <summary>
        /// Obtiene el User-Agent del cliente
        /// </summary>
        public static string GetUserAgent(this HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            if (string.IsNullOrEmpty(userAgent))
            {
                return "unknown";
            }

            // Limitar longitud para evitar desbordamiento (DB tiene 300 chars)
            return userAgent.Length > 300 ? userAgent[..300] : userAgent;
        }

        #endregion
    }
}