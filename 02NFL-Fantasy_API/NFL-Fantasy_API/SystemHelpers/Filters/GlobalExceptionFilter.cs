using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Helpers.Filters
{
    /// <summary>
    /// ExceptionFilter global que captura y maneja todas las excepciones no controladas.
    /// 
    /// PROPÓSITO:
    /// - Eliminar try-catch repetitivos en controllers
    /// - Centralizar logging de errores
    /// - Formato consistente de respuestas de error
    /// - Manejo especializado de excepciones SQL Server
    /// - Prevenir exposición de detalles internos al cliente
    /// 
    /// FUNCIONAMIENTO:
    /// - Captura TODAS las excepciones no manejadas
    /// - Logea el error con detalles completos
    /// - Retorna respuesta genérica al cliente (sin detalles sensibles)
    /// - Marca la excepción como manejada
    /// 
    /// CÓDIGOS SQL PERSONALIZADOS:
    /// Los Stored Procedures usan códigos en el rango 50000-50999 para errores de negocio.
    /// Estos códigos se mapean a códigos HTTP apropiados.
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var (statusCode, userMessage, errorCode) = GetErrorResponse(context.Exception);

            // Log completo con acción y número de error (si aplica)
            if (context.Exception is SqlException sqlEx)
            {
                _logger.LogError(
                    context.Exception,
                    "Unhandled SQL exception in {Action}: {Message}. SqlError={SqlError}",
                    context.ActionDescriptor.DisplayName,
                    context.Exception.Message,
                    sqlEx.Number
                );
            }
            else
            {
                _logger.LogError(
                    context.Exception,
                    "Unhandled exception in {Action}: {Message}",
                    context.ActionDescriptor.DisplayName,
                    context.Exception.Message
                );
            }

            var traceId = context.HttpContext.TraceIdentifier;

            // Respuesta uniforme (con errorCode y traceId si agregaste el DTO extendido)
            context.Result = new ObjectResult(ApiResponseDTO.ErrorResponse(userMessage, errorCode, traceId))
            {
                StatusCode = statusCode
            };
            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Determina el código de estado HTTP y mensaje según el tipo de excepción.
        /// </summary>
        private static (int statusCode, string message, int? errorCode) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                SqlException sqlEx => HandleSqlException(sqlEx),

                ArgumentNullException => (400, "Parámetro requerido faltante.", null),
                ArgumentException argEx => (400, argEx.Message, null),
                InvalidOperationException invalidOp => (400, invalidOp.Message, null),

                UnauthorizedAccessException => (403, "No autorizado para realizar esta acción.", null),
                TimeoutException => (408, "La operación tardó demasiado. Intente nuevamente.", null),

                _ => (500, "Error interno del servidor.", null)
            };
        }

        /// <summary>
        /// Códigos personalizados mapeados:
        /// - 50320: Solo el comisionado puede remover equipos de la liga → 403 (Forbidden)
        /// - 50353: No tienes un equipo activo en esta liga → 404 (Not Found) [o 400 si prefieres "Bad Request"]
        /// 
        /// Recomendación de rangos por dominio:
        /// 50310-50339 → Permisos/rol en liga (403)
        /// 50340-50369 → Estado de membresía/equipo (404/409/400 según el caso)
        /// </summary>
        private static (int statusCode, string message, int? errorCode) HandleSqlException(SqlException sqlEx)
        {
            switch (sqlEx.Number)
            {
                // ======== NUEVOS CASOS ========
                case 50360:
                    // "Solo el comisionado puede transferir el comisionado."
                    return (403, sqlEx.Message, 50360);

                case 50362:
                    // "El nuevo comisionado debe tener un equipo en la liga."
                    // Precondición de estado no satisfecha -> Conflict
                    return (409, sqlEx.Message, 50362);

                case 50308:
                    // "Ya tienes una liga activa como comisionado. Debes desactivarla antes de unirte a otra."
                    return (409, sqlEx.Message, 50308);

                case 50303:
                    // "Contraseña de liga incorrecta."
                    // Autenticación a nivel de recurso (liga protegida por contraseña)
                    return (401, sqlEx.Message, 50303);

                // ======== TUS CASOS NUEVOS ========
                case 50320:
                    // "Solo el comisionado puede remover equipos de la liga."
                    return (403, sqlEx.Message, 50320);

                case 50353:
                    // "No tienes un equipo activo en esta liga."
                    // Semánticamente es "membership no encontrada" => 404
                    return (404, sqlEx.Message, 50353);

                // ======== PERSONALIZADOS GENERALES EXISTENTES ========
                case 50000: return (400, sqlEx.Message, 50000);
                case 50210: return (403, sqlEx.Message, 50210);
                case 50220: return (403, sqlEx.Message, 50220);
                case 50230: return (404, sqlEx.Message, 50230);
                case 50240: return (409, sqlEx.Message, 50240);
                case 50250: return (400, sqlEx.Message, 50250);

                // ======== SQL SERVER ESTÁNDAR ========
                case 2627: return (409, "Ya existe un registro con estos datos únicos.", 2627);
                case 2601: return (409, "Valor duplicado no permitido.", 2601);
                case 515: return (400, "Faltan datos requeridos.", 515);
                case 547: return (400, "Referencia a un registro que no existe.", 547);
                case -2: return (504, "Timeout de conexión a base de datos.", -2);
                case -1: return (503, "No se pudo conectar a la base de datos.", -1);
                case 1205: return (409, "Conflicto de concurrencia. Intente nuevamente.", 1205);
                case 208: return (500, "Error de configuración de base de datos.", 208);

                default: return (500, "Error de base de datos. Intente nuevamente.", sqlEx.Number);
            }
        }
    }
}