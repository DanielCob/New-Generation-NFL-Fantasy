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
            // Determinar respuesta según tipo de excepción
            var (statusCode, userMessage) = GetErrorResponse(context.Exception);

            // Log completo del error con stack trace
            _logger.LogError(
                context.Exception,
                "Unhandled exception in {Action}: {Message}",
                context.ActionDescriptor.DisplayName,
                context.Exception.Message
            );

            // Retornar respuesta al cliente
            context.Result = new ObjectResult(ApiResponseDTO.ErrorResponse(userMessage))
            {
                StatusCode = statusCode
            };

            // Marcar como manejada para evitar que se propague
            context.ExceptionHandled = true;
        }

        /// <summary>
        /// Determina el código de estado HTTP y mensaje según el tipo de excepción.
        /// </summary>
        private (int statusCode, string message) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                // ===== SQL SERVER EXCEPTIONS =====
                SqlException sqlEx => HandleSqlException(sqlEx),

                // ===== VALIDATION EXCEPTIONS =====
                ArgumentNullException => (400, "Parámetro requerido faltante."),
                ArgumentException argEx => (400, argEx.Message),
                InvalidOperationException invalidOp => (400, invalidOp.Message),

                // ===== AUTHORIZATION EXCEPTIONS =====
                UnauthorizedAccessException => (403, "No autorizado para realizar esta acción."),

                // ===== TIMEOUT EXCEPTIONS =====
                TimeoutException => (408, "La operación tardó demasiado. Intente nuevamente."),

                // ===== GENERIC EXCEPTIONS =====
                _ => (500, "Error interno del servidor.")
            };
        }

        /// <summary>
        /// Maneja excepciones SQL Server según el número de error.
        /// </summary>
        /// <remarks>
        /// CÓDIGOS PERSONALIZADOS DE STORED PROCEDURES (50000-50999):
        /// Estos códigos son lanzados por nuestros SPs usando THROW para indicar errores de negocio.
        /// 
        /// RANGO 50000-50099: Errores generales de negocio
        /// - 50000: Error de negocio genérico (validación, regla de negocio violada)
        /// 
        /// RANGO 50200-50299: Errores de permisos y autorización
        /// - 50210: Permiso denegado - Solo ADMIN puede realizar esta operación
        /// - 50220: Operación no permitida - Violación de regla de negocio específica
        /// 
        /// RANGO 50230-50249: Errores de recursos
        /// - 50230: Recurso no encontrado
        /// - 50240: Conflicto - El recurso ya existe
        /// 
        /// RANGO 50250-50299: Errores de validación
        /// - 50250: Validación fallida - Datos inválidos
        /// 
        /// CÓDIGOS ESTÁNDAR SQL SERVER:
        /// - 2627: Violación de UNIQUE constraint
        /// - 2601: Duplicate key
        /// - 515: Cannot insert NULL into NOT NULL column
        /// - 547: Violación de FOREIGN KEY constraint
        /// - -2: Connection timeout
        /// </remarks>
        private (int statusCode, string message) HandleSqlException(SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                // ===== CÓDIGOS PERSONALIZADOS (50000-50999) =====

                // 50000: Error de negocio genérico
                // Los SPs usan este código para errores de validación y reglas de negocio
                // Ejemplos: "Liga llena", "Ya eres miembro", "Contraseña incorrecta"
                50000 => (400, sqlEx.Message),

                // 50210: Permiso denegado - Solo ADMIN puede hacer esta operación
                // Ejemplo: "Solo un ADMIN puede cambiar roles de sistema"
                50210 => (403, sqlEx.Message),

                // 50220: Violación de regla de negocio
                50220 => (403, sqlEx.Message),

                // 50230: Recurso no encontrado
                // Ejemplo: "Usuario no existe", "Liga no encontrada"
                50230 => (404, sqlEx.Message),

                // 50240: Conflicto - Ya existe
                // Ejemplo: "Email ya está registrado", "Username ya en uso"
                50240 => (409, sqlEx.Message),

                // 50250: Validación fallida
                // Ejemplo: "El rol especificado no es válido"
                50250 => (400, sqlEx.Message),

                // ===== CÓDIGOS ESTÁNDAR SQL SERVER =====

                // 2627: Violación de UNIQUE constraint
                2627 => (409, "Ya existe un registro con estos datos únicos."),

                // 2601: Duplicate key (similar a 2627)
                2601 => (409, "Valor duplicado no permitido."),

                // 515: Cannot insert NULL into NOT NULL column
                515 => (400, "Faltan datos requeridos."),

                // 547: Violación de FOREIGN KEY constraint
                547 => (400, "Referencia a un registro que no existe."),

                // -2: Connection timeout
                -2 => (504, "Timeout de conexión a base de datos."),

                // -1: Connection error general
                -1 => (503, "No se pudo conectar a la base de datos."),

                // 1205: Deadlock victim
                1205 => (409, "Conflicto de concurrencia. Intente nuevamente."),

                // 208: Invalid object name (tabla/vista no existe)
                208 => (500, "Error de configuración de base de datos."),

                // Cualquier otro error SQL
                _ => (500, "Error de base de datos. Intente nuevamente.")
            };
        }
    }
}