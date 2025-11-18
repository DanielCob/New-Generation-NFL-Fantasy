using NFL_Fantasy_API.DataAccessLayer.StorageDatabase.Implementations;
using NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage;

namespace NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Implementations.Storage
{
    /// <summary>
    /// Implementación del servicio de almacenamiento usando MinIO.
    /// 
    /// RESPONSABILIDAD:
    /// - Lógica de negocio para manejo de imágenes
    /// - Generación de nombres únicos de archivos
    /// - Organización en carpetas
    /// - Orquestación de operaciones
    /// 
    /// NO contiene:
    /// - Operaciones directas con MinIO (delegadas a DataAccess)
    /// - Validaciones de archivos (están en Controller)
    /// - Configuración (está en MinIOSettings)
    /// </summary>
    public class StorageService : IStorageService
    {
        private readonly MinIODataAccess _dataAccess;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            MinIODataAccess dataAccess,
            ILogger<StorageService> logger)
        {
            _dataAccess = dataAccess;
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
                var uniqueObjectName = GenerateUniqueObjectName(fileName, folder);

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
                var uniqueObjectName = GenerateUniqueObjectName(fileName, folder);

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