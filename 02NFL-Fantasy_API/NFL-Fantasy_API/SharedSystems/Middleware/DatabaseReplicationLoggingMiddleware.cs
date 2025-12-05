namespace NFL_Fantasy_API.SharedSystems.Middleware
{
    /// <summary>
    /// Middleware opcional para logging detallado de operaciones de replicación.
    /// </summary>
    public class DatabaseReplicationLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DatabaseReplicationLoggingMiddleware> _logger;

        public DatabaseReplicationLoggingMiddleware(
            RequestDelegate next,
            ILogger<DatabaseReplicationLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            // Solo loggear operaciones de escritura (POST, PUT, DELETE)
            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                _logger.LogDebug("Operación de escritura detectada: {Method} {Path}", method, path);
            }

            await _next(context);
        }
    }

    public static class DatabaseReplicationLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseDatabaseReplicationLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DatabaseReplicationLoggingMiddleware>();
        }
    }
}