using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de almacenamiento de imágenes
    /// Maneja la carga de imágenes a MinIO
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<StorageController> _logger;

        public StorageController(IStorageService storageService, ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        /// <summary>
        /// Carga una imagen a MinIO
        /// POST /api/storage/upload-image
        /// Requiere autenticación
        /// </summary>
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponseDTO>> UploadImage(IFormFile file, string? folder = null)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                // Validar que hay un archivo
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("No se proporcionó ninguna imagen."));
                }

                // Validar tamaño (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("La imagen no puede superar 5MB."));
                }

                // Validar tipo de archivo
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("Solo se permiten imágenes JPEG y PNG."));
                }

                // Validar extensión del archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("Extensión de archivo no permitida."));
                }

                // Cargar a MinIO
                using var imageStream = file.OpenReadStream();

                // Cargar imagen
                var imageUrl = await _storageService.UploadImageAsync(
                    imageStream,
                    file.FileName,
                    file.ContentType,
                    folder
                );

                var userId = HttpContext.GetUserId();
                _logger.LogInformation("User {UserID} uploaded image: {ImageUrl}", userId, imageUrl);

                // Preparar respuesta
                var response = new
                {
                    ImageUrl = imageUrl,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.Length
                };

                return Ok(ApiResponseDTO.SuccessResponse("Imagen cargada exitosamente.", response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, ApiResponseDTO.ErrorResponse($"Error al cargar imagen: {ex.Message}"));
            }
        }

        /// <summary>
        /// Elimina una imagen de MinIO
        /// DELETE /api/storage/delete-image
        /// Requiere autenticación
        /// </summary>
        [HttpDelete("delete-image")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteImage([FromBody] DeleteImageDTO dto)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                if (string.IsNullOrEmpty(dto.ImageUrl))
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("URL de imagen requerida."));
                }

                var deleted = await _storageService.DeleteImageAsync(dto.ImageUrl);

                if (deleted)
                {
                    var userId = HttpContext.GetUserId();
                    _logger.LogInformation("User {UserID} deleted image: {ImageUrl}", userId, dto.ImageUrl);
                    return Ok(ApiResponseDTO.SuccessResponse("Imagen eliminada exitosamente."));
                }

                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo eliminar la imagen."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return StatusCode(500, ApiResponseDTO.ErrorResponse($"Error al eliminar imagen: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// DTO para eliminar imagen
    /// </summary>
    public class DeleteImageDTO
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}