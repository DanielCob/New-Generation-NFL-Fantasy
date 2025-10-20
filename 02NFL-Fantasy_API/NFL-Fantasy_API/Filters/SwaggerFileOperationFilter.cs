using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NFL_Fantasy_API.Filters
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.MethodInfo
                .GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           p.ParameterType == typeof(IFormFileCollection))
                .ToArray();

            if (fileParams.Length == 0)
                return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                },
                                ["folder"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Carpeta opcional donde guardar la imagen"
                                }
                            },
                            Required = new HashSet<string> { "file" }
                        }
                    }
                }
            };

            // Limpiar parámetros que ya están en el body
            operation.Parameters.Clear();
        }
    }
}