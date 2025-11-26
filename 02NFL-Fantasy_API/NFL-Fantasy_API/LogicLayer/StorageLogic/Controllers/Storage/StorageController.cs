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
            var userId = this.UserId();

            var result = await _storageService.UploadImagesBatchAsync(
                files,
                userId,
                folder
            );

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se pudo procesar la carga de imágenes."
                ));
            }

            return result.Success ? Ok(result) : BadRequest(result);
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
            var userId = this.UserId();

            var result = await _storageService.UploadJsonsBatchAsync(
                files,
                userId,
                folder
            );

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se pudo procesar la carga de archivos JSON."
                ));
            }

            return result.Success ? Ok(result) : BadRequest(result);
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