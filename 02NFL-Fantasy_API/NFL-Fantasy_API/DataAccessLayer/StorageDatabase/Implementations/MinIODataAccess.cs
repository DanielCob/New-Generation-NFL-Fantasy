using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using NFL_Fantasy_API.SharedSystems.StorageConfig;

namespace NFL_Fantasy_API.DataAccessLayer.StorageDatabase.Implementations
{
    /// <summary>
    /// Capa de acceso a datos para MinIO (S3-compatible object storage).
    /// 
    /// RESPONSABILIDAD:
    /// - Operaciones CRUD en MinIO/S3
    /// - Construcción de URLs públicas
    /// - Manejo de errores de bajo nivel
    /// 
    /// NO contiene:
    /// - Lógica de negocio
    /// - Validaciones de archivos (están en Controller)
    /// - Inicialización de buckets (está en MinIOInitializer)
    /// </summary>
    public class MinIODataAccess
    {
        private readonly IMinioClient _minioClient;
        private readonly MinIOSettings _settings;
        private readonly ILogger<MinIODataAccess> _logger;

        public MinIODataAccess(
            IMinioClient minioClient,
            IOptions<MinIOSettings> settings,
            ILogger<MinIODataAccess> logger)
        {
            _minioClient = minioClient;
            _settings = settings.Value;
            _logger = logger;
        }

        #region Upload

        /// <summary>
        /// Carga un objeto (imagen) a MinIO.
        /// </summary>
        /// <param name="stream">Stream del archivo</param>
        /// <param name="objectName">Nombre del objeto en el bucket</param>
        /// <param name="contentType">Tipo MIME del archivo</param>
        /// <returns>URL pública del objeto</returns>
        public async Task<string> UploadObjectAsync(
            Stream stream,
            string objectName,
            string contentType)
        {
            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);

                // Construir URL pública
                var publicUrl = BuildPublicUrl(objectName);

                _logger.LogDebug(
                    "Objeto cargado: {ObjectName} -> {Url}",
                    objectName,
                    publicUrl
                );

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar objeto: {ObjectName}",
                    objectName
                );
                throw;
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Elimina un objeto de MinIO.
        /// </summary>
        /// <param name="objectName">Nombre del objeto en el bucket</param>
        /// <returns>True si se eliminó exitosamente</returns>
        public async Task<bool> DeleteObjectAsync(string objectName)
        {
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                _logger.LogDebug(
                    "Objeto eliminado: {ObjectName}",
                    objectName
                );

                return true;
            }
            catch (ObjectNotFoundException)
            {
                _logger.LogWarning(
                    "Objeto no encontrado para eliminar: {ObjectName}",
                    objectName
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar objeto: {ObjectName}",
                    objectName
                );
                return false;
            }
        }

        #endregion

        #region Exists

        /// <summary>
        /// Verifica si un objeto existe en MinIO.
        /// </summary>
        /// <param name="objectName">Nombre del objeto en el bucket</param>
        /// <returns>True si existe</returns>
        public async Task<bool> ObjectExistsAsync(string objectName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName);

                await _minioClient.StatObjectAsync(statObjectArgs);
                return true;
            }
            catch (ObjectNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar existencia de objeto: {ObjectName}",
                    objectName
                );
                return false;
            }
        }

        #endregion

        #region Presigned URL

        /// <summary>
        /// Genera una URL temporal firmada para acceso privado.
        /// </summary>
        /// <param name="objectName">Nombre del objeto</param>
        /// <param name="expiryInSeconds">Segundos hasta expiración</param>
        /// <returns>URL temporal</returns>
        public async Task<string> GetPresignedUrlAsync(
            string objectName,
            int expiryInSeconds = 3600)
        {
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(_settings.BucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

                var url = await _minioClient.PresignedGetObjectAsync(
                    presignedGetObjectArgs
                );

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al generar URL presignada: {ObjectName}",
                    objectName
                );
                throw;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Construye la URL pública de un objeto.
        /// </summary>
        private string BuildPublicUrl(string objectName)
        {
            return $"{_settings.Protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
        }

        /// <summary>
        /// Extrae el nombre del objeto desde una URL completa.
        /// </summary>
        public string ExtractObjectNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Skip bucket name (first segment) and get the rest
                if (segments.Length >= 2)
                {
                    return string.Join("/", segments.Skip(1));
                }

                // Si no es una URL válida, asumimos que ya es el object name
                return url;
            }
            catch
            {
                // Si falla el parsing, devolver el string original
                return url;
            }
        }

        #endregion
    }
}