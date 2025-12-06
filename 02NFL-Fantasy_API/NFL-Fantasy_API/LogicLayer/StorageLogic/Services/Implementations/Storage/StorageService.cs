using Microsoft.Extensions.Options;
using NFL_Fantasy_API.DataAccessLayer.StorageDatabase.Implementations;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.SharedSystems.StorageConfig;
using NFL_Fantasy_API.SharedSystems.Validators.Storage;

namespace NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Implementations.Storage
{
    /// <summary>
    /// Implementación del servicio de almacenamiento usando MinIO.
    /// 
    /// RESPONSABILIDAD:
    /// - Lógica de negocio para manejo de imágenes/JSON
    /// - Generación de nombres únicos de archivos
    /// - Organización en carpetas
    /// - Orquestación de operaciones
    /// 
    /// NO contiene:
    /// - Operaciones directas con MinIO (delegadas a DataAccess)
    /// - Reglas de validación de archivos (delegadas a StorageFileValidator)
    /// - Configuración (está en MinIOSettings)
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly MinIODataAccess _dataAccess;
        private readonly ILogger<StorageService> _logger;
        private readonly MinIOSettings _settings;

        public StorageService(
            MinIODataAccess dataAccess,
            IOptions<MinIOSettings> settings,
            ILogger<StorageService> logger)
        {
            _dataAccess = dataAccess;
            _settings = settings.Value;
            _logger = logger;
        }

        #region Upload Image

        /// <summary>
        /// Carga una imagen al almacenamiento.
        /// </summary>
        public async Task<string> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            string? folder = null)
        {
            try
            {
                var targetFolder = folder ?? _settings.ImagesFolder;
                var uniqueObjectName = GenerateUniqueObjectName(fileName, targetFolder);

                var publicUrl = await _dataAccess.UploadObjectAsync(
                    imageStream,
                    uniqueObjectName,
                    contentType
                );

                _logger.LogInformation(
                    "Imagen cargada: {FileName} -> {Url}",
                    fileName,
                    publicUrl
                );

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar imagen: {FileName}",
                    fileName
                );
                throw;
            }
        }

        /// <summary>
        /// Carga múltiples imágenes al almacenamiento (batch).
        /// Reutiliza UploadImageAsync internamente.
        /// </summary>
        public async Task<ApiResponseDTO> UploadImagesBatchAsync(
            List<IFormFile> files,
            int actorUserId,
            string? folder = null)
        {
            if (files == null || files.Count == 0)
            {
                return ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron imágenes."
                );
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                // VALIDACIÓN: delegada a StorageFileValidator
                var validationErrors = StorageFileValidator.ValidateImageFile(file);

                if (validationErrors.Any())
                {
                    var fileName = file?.FileName ?? "(sin nombre)";
                    foreach (var error in validationErrors)
                    {
                        errors.Add($"{fileName}: {error}");
                    }
                    continue;
                }

                try
                {
                    using var imageStream = file!.OpenReadStream();

                    var imageUrl = await UploadImageAsync(
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
                    var fileName = file?.FileName ?? "(sin nombre)";
                    errors.Add($"{fileName}: {ex.Message}");

                    _logger.LogError(
                        ex,
                        "Error al cargar imagen en batch: {FileName}",
                        fileName
                    );
                }
            }

            _logger.LogInformation(
                "User {UserId} uploaded {SuccessCount} images with {ErrorCount} errors",
                actorUserId,
                results.Count,
                errors.Count
            );

            var payload = new
            {
                UploadedImages = results,
                Errors = errors,
                TotalProcessed = files.Count,
                SuccessCount = results.Count,
                ErrorCount = errors.Count
            };

            return ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} imágenes cargadas, {errors.Count} errores.",
                payload
            );
        }

        #endregion

        #region Upload JSON

        public async Task<string> UploadJsonAsync(
            Stream jsonStream,
            string fileName,
            string contentType,
            string? folder = null)
        {
            try
            {
                var targetFolder = folder ?? _settings.JsonFolder;
                var uniqueObjectName = GenerateUniqueObjectName(fileName, targetFolder);

                var publicUrl = await _dataAccess.UploadObjectAsync(
                    jsonStream,
                    uniqueObjectName,
                    contentType
                );

                _logger.LogInformation(
                    "JSON cargado: {FileName} -> {Url}",
                    fileName,
                    publicUrl
                );

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar JSON: {FileName}",
                    fileName
                );
                throw;
            }
        }

        /// <summary>
        /// Carga múltiples archivos JSON al almacenamiento (batch).
        /// Reutiliza UploadJsonAsync internamente.
        /// </summary>
        public async Task<ApiResponseDTO> UploadJsonsBatchAsync(
            List<IFormFile> files,
            int actorUserId,
            string? folder = null)
        {
            if (files == null || files.Count == 0)
            {
                return ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron archivos JSON."
                );
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                // VALIDACIÓN: delegada a StorageFileValidator
                var validationErrors = StorageFileValidator.ValidateJsonFile(file);

                if (validationErrors.Any())
                {
                    var fileName = file?.FileName ?? "(sin nombre)";
                    foreach (var error in validationErrors)
                    {
                        errors.Add($"{fileName}: {error}");
                    }
                    continue;
                }

                try
                {
                    using var jsonStream = file!.OpenReadStream();

                    var jsonUrl = await UploadJsonAsync(
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
                    var fileName = file?.FileName ?? "(sin nombre)";
                    errors.Add($"{fileName}: {ex.Message}");

                    _logger.LogError(
                        ex,
                        "Error al cargar JSON en batch: {FileName}",
                        fileName
                    );
                }
            }

            _logger.LogInformation(
                "User {UserId} uploaded {SuccessCount} JSON files with {ErrorCount} errors",
                actorUserId,
                results.Count,
                errors.Count
            );

            var payload = new
            {
                UploadedJsons = results,
                Errors = errors,
                TotalProcessed = files.Count,
                SuccessCount = results.Count,
                ErrorCount = errors.Count
            };

            return ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} archivos JSON cargados, {errors.Count} errores.",
                payload
            );
        }

        #endregion

        #region Delete Object

        public async Task<bool> DeleteObjectAsync(string objectUrl)
        {
            try
            {
                var objectName = _dataAccess.ExtractObjectNameFromUrl(objectUrl);

                if (string.IsNullOrWhiteSpace(objectName))
                {
                    _logger.LogWarning(
                        "No se pudo extraer object name de URL: {Url}",
                        objectUrl
                    );
                    return false;
                }

                var deleted = await _dataAccess.DeleteObjectAsync(objectName);

                if (deleted)
                {
                    _logger.LogInformation(
                        "Objeto eliminado: {Url}",
                        objectUrl
                    );
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar objeto: {Url}",
                    objectUrl
                );
                return false;
            }
        }

        #endregion

        #region Object Exists

        public async Task<bool> ObjectExistsAsync(string objectUrl)
        {
            try
            {
                var objectName = _dataAccess.ExtractObjectNameFromUrl(objectUrl);

                if (string.IsNullOrWhiteSpace(objectName))
                {
                    return false;
                }

                return await _dataAccess.ObjectExistsAsync(objectName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar existencia de objeto: {Url}",
                    objectUrl
                );
                return false;
            }
        }

        #endregion

        #region Presigned URL

        /// <summary>
        /// Genera URL temporal firmada.
        /// </summary>
        public async Task<string> GetPresignedUrlAsync(
            string imageUrl,
            int expiryInSeconds = 3600)
        {
            try
            {
                var objectName = _dataAccess.ExtractObjectNameFromUrl(imageUrl);

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPresignedUrlAsync(
                    objectName,
                    expiryInSeconds
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al generar URL presignada: {Url}",
                    imageUrl
                );
                throw;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Genera un nombre único para el objeto en MinIO.
        /// Formato: [folder/]timestamp_cleanFileName.ext
        /// </summary>
        private string GenerateUniqueObjectName(string fileName, string? folder)
        {
            // Timestamp Unix para unicidad
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Limpiar nombre de archivo
            var cleanFileName = Path.GetFileNameWithoutExtension(fileName)
                .Replace(" ", "_")
                .Replace("-", "_");

            var extension = Path.GetExtension(fileName);

            var uniqueFileName = $"{timestamp}_{cleanFileName}{extension}";

            // Agregar carpeta si existe
            if (!string.IsNullOrWhiteSpace(folder))
            {
                return $"{folder.Trim('/')}/{uniqueFileName}";
            }

            return uniqueFileName;
        }

        #endregion
    }
}