using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Models.DTOs.Images;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.StorageLogic.Controllers.Storage
{
    [ApiController]
    [Route("api/storage")]
    [Authorize]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<StorageController> _logger;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private const long MaxJsonSizeBytes = 10 * 1024 * 1024; // 10MB
        private static readonly string[] AllowedImageMimeTypes = { "image/jpeg", "image/png", "image/jpg" };
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedJsonMimeTypes = { "application/json", "text/json" };
        private static readonly string[] AllowedJsonExtensions = { ".json" };

        public StorageController(
            IStorageService storageService,
            ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDTO>> UploadImage(
            IFormFile file,
            string? folder = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se proporcionó ninguna imagen."
                ));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "La imagen no puede superar 5MB."
                ));
            }

            if (!AllowedImageMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Solo se permiten imágenes JPEG y PNG."
                ));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedImageExtensions.Contains(fileExtension))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Extensión de archivo no permitida."
                ));
            }

            using var imageStream = file.OpenReadStream();

            var imageUrl = await _storageService.UploadImageAsync(
                imageStream,
                file.FileName,
                file.ContentType,
                folder
            );

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} uploaded image: {ImageUrl}",
                userId,
                imageUrl
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Imagen cargada exitosamente.",
                new
                {
                    ImageUrl = imageUrl,
                    file.FileName,
                    file.ContentType,
                    Size = file.Length
                }
            ));
        }

        [HttpPost("upload-images")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDTO>> UploadImages(
            List<IFormFile> files,
            string? folder = null)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron imágenes."
                ));
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    errors.Add($"Archivo vacío: {file.FileName}");
                    continue;
                }

                if (file.Length > MaxFileSizeBytes)
                {
                    errors.Add($"{file.FileName}: La imagen no puede superar 5MB.");
                    continue;
                }

                if (!AllowedImageMimeTypes.Contains(file.ContentType.ToLower()))
                {
                    errors.Add($"{file.FileName}: Solo se permiten imágenes JPEG y PNG.");
                    continue;
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!AllowedImageExtensions.Contains(fileExtension))
                {
                    errors.Add($"{file.FileName}: Extensión de archivo no permitida.");
                    continue;
                }

                try
                {
                    using var imageStream = file.OpenReadStream();

                    var imageUrl = await _storageService.UploadImageAsync(
                        imageStream,
                        file.FileName,
                        file.ContentType,
                        folder
                    );

                    results.Add(new
                    {
                        ImageUrl = imageUrl,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Size = file.Length,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} uploaded {Count} images with {Errors} errors",
                userId,
                results.Count,
                errors.Count
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} imágenes cargadas, {errors.Count} errores.",
                new
                {
                    UploadedImages = results,
                    Errors = errors,
                    TotalProcessed = files.Count,
                    SuccessCount = results.Count,
                    ErrorCount = errors.Count
                }
            ));
        }

        [HttpPost("upload-json")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDTO>> UploadJson(
            IFormFile file,
            string? folder = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se proporcionó ningún archivo JSON."
                ));
            }

            if (file.Length > MaxJsonSizeBytes)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "El archivo JSON no puede superar 10MB."
                ));
            }

            if (!AllowedJsonMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Solo se permiten archivos JSON."
                ));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedJsonExtensions.Contains(fileExtension))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Extensión de archivo no permitida. Solo se permite .json"
                ));
            }

            using var jsonStream = file.OpenReadStream();

            var jsonUrl = await _storageService.UploadJsonAsync(
                jsonStream,
                file.FileName,
                file.ContentType,
                folder
            );

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} uploaded JSON: {JsonUrl}",
                userId,
                jsonUrl
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Archivo JSON cargado exitosamente.",
                new
                {
                    JsonUrl = jsonUrl,
                    file.FileName,
                    file.ContentType,
                    Size = file.Length
                }
            ));
        }

        [HttpPost("upload-jsons")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDTO>> UploadJsons(
            List<IFormFile> files,
            string? folder = null)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron archivos JSON."
                ));
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    errors.Add($"Archivo vacío: {file.FileName}");
                    continue;
                }

                if (file.Length > MaxJsonSizeBytes)
                {
                    errors.Add($"{file.FileName}: El archivo JSON no puede superar 10MB.");
                    continue;
                }

                if (!AllowedJsonMimeTypes.Contains(file.ContentType.ToLower()))
                {
                    errors.Add($"{file.FileName}: Solo se permiten archivos JSON.");
                    continue;
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!AllowedJsonExtensions.Contains(fileExtension))
                {
                    errors.Add($"{file.FileName}: Extensión de archivo no permitida. Solo se permite .json");
                    continue;
                }

                try
                {
                    using var jsonStream = file.OpenReadStream();

                    var jsonUrl = await _storageService.UploadJsonAsync(
                        jsonStream,
                        file.FileName,
                        file.ContentType,
                        folder
                    );

                    results.Add(new
                    {
                        JsonUrl = jsonUrl,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        Size = file.Length,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} uploaded {Count} JSON files with {Errors} errors",
                userId,
                results.Count,
                errors.Count
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} archivos JSON cargados, {errors.Count} errores.",
                new
                {
                    UploadedJsons = results,
                    Errors = errors,
                    TotalProcessed = files.Count,
                    SuccessCount = results.Count,
                    ErrorCount = errors.Count
                }
            ));
        }

        [HttpDelete("delete-object")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteObject(
            [FromBody] DeleteImageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "URL del objeto requerida."
                ));
            }

            var deleted = await _storageService.DeleteObjectAsync(dto.ImageUrl);

            if (!deleted)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se pudo eliminar el objeto."
                ));
            }

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} deleted object: {ObjectUrl}",
                userId,
                dto.ImageUrl
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Objeto eliminado exitosamente."
            ));
        }
    }
}