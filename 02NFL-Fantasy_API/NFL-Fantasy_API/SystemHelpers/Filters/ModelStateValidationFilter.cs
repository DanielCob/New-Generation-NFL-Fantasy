using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.Helpers.Filters
{
    /// <summary>
    /// ActionFilter que valida automáticamente el ModelState en todos los endpoints.
    /// 
    /// PROPÓSITO:
    /// - Eliminar código repetitivo de validación en controllers
    /// - Centralizar formato de errores de validación
    /// - Ejecutarse ANTES de que el action del controller se ejecute
    /// 
    /// FUNCIONAMIENTO:
    /// - Si ModelState es inválido → retorna BadRequest con errores
    /// - Si ModelState es válido → permite que el action continúe
    /// 
    /// APLICACIÓN:
    /// Registrado globalmente en Program.cs, se aplica a TODOS los endpoints
    /// </summary>
    public class ModelStateValidationFilter : IActionFilter
    {
        /// <summary>
        /// Se ejecuta ANTES del action del controller
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                // Extraer todos los mensajes de error
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg));

                var errorMessage = string.Join(" ", errors);

                // Cortocircuitar la ejecución y retornar BadRequest
                context.Result = new BadRequestObjectResult(
                    ApiResponseDTO.ErrorResponse(errorMessage)
                );
            }
        }

        /// <summary>
        /// Se ejecuta DESPUÉS del action del controller (no lo usamos)
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No necesitamos hacer nada aquí
        }
    }
}