namespace NFL_Fantasy_API.Extensions
{
    /// <summary>
    /// Extensiones para HttpContext que facilitan acceso a datos de autenticación
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Obtiene el UserID del usuario autenticado desde el contexto
        /// </summary>
        public static int GetUserId(this HttpContext context)
        {
            if (context.Items.TryGetValue("UserID", out var userIdObj) && userIdObj is int userId)
            {
                return userId;
            }
            return 0;
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

        /// <summary>
        /// Obtiene la IP del cliente, considerando proxies (X-Forwarded-For)
        /// </summary>
        public static string GetClientIpAddress(this HttpContext context)
        {
            // Verificar si viene de proxy/load balancer
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Tomar la primera IP de la cadena
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
                // Mapear IPv6 loopback a IPv4
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
            return userAgent.Length > 300 ? userAgent.Substring(0, 300) : userAgent;
        }
    }
}