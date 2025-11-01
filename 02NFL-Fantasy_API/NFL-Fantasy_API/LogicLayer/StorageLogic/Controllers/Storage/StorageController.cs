using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Models.DTOs.Images;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage;

namespace NFL_Fantasy_API.LogicLayer.StorageLogic.Controllers.Storage
{
    /// <summary>
    /// Controller de gestión de almacenamiento de imágenes.
    /// </summary>
    [ApiController]
    [Route("api/storage")]
    [Authorize]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<StorageController> _logger;

        // Validaciones HTTP (no lógica de negocio)
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/jpg" };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

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
            // Validaciones HTTP
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

            if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Solo se permiten imágenes JPEG y PNG."
                ));
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "Extensión de archivo no permitida."
                ));
            }

            // Cargar imagen
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

        [HttpDelete("delete-image")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteImage(
            [FromBody] DeleteImageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "URL de imagen requerida."
                ));
            }

            var deleted = await _storageService.DeleteImageAsync(dto.ImageUrl);

            if (!deleted)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se pudo eliminar la imagen."
                ));
            }

            var userId = this.UserId();
            _logger.LogInformation(
                "User {UserId} deleted image: {ImageUrl}",
                userId,
                dto.ImageUrl
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Imagen eliminada exitosamente."
            ));
        }
    }
}