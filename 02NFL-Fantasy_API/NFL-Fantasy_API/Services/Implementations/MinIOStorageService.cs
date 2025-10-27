using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using NFL_Fantasy_API.Services.Interfaces;
using System.Text.Json;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de almacenamiento usando MinIO
    /// </summary>
    public class MinIOStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinIOStorageService> _logger;
        private readonly string _bucketName;
        private readonly string _endpoint;
        private bool _bucketInitialized = false;

        public MinIOStorageService(IConfiguration configuration, ILogger<MinIOStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Leer configuración de MinIO
            var minioConfig = configuration.GetSection("MinIO");
            _endpoint = minioConfig["Endpoint"] ?? "127.0.0.1:9000";
            var accessKey = minioConfig["AccessKey"] ?? "admin";
            var secretKey = minioConfig["SecretKey"] ?? "supersecret";
            var useSSL = bool.Parse(minioConfig["UseSSL"] ?? "false");
            _bucketName = minioConfig["BucketName"] ?? "nfl-fantasy-images";

            // Crear cliente de MinIO
            _minioClient = new MinioClient()
                .WithEndpoint(_endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSSL)
                .Build();

            _logger.LogInformation("MinIO client inicializado con endpoint: {Endpoint}", _endpoint);
        }

        /// <summary>
        /// Asegura que el bucket existe y tiene política pública
        /// </summary>
        private async Task EnsureBucketExistsAsync()
        {
            if (_bucketInitialized)
                return;

            try
            {
                var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
                bool found = await _minioClient.BucketExistsAsync(beArgs);

                if (!found)
                {
                    var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                    await _minioClient.MakeBucketAsync(mbArgs);
                    _logger.LogInformation("Bucket {BucketName} creado exitosamente.", _bucketName);
                }

                // Configurar política pública para permitir lectura anónima
                var policyJson = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Effect": "Allow",
                            "Principal": {
                                "AWS": ["*"]
                            },
                            "Action": [
                                "s3:GetBucketLocation",
                                "s3:ListBucket"
                            ],
                            "Resource": ["arn:aws:s3:::{{_bucketName}}"]
                        },
                        {
                            "Effect": "Allow",
                            "Principal": {
                                "AWS": ["*"]
                            },
                            "Action": ["s3:GetObject"],
                            "Resource": ["arn:aws:s3:::{{_bucketName}}/*"]
                        }
                    ]
                }
                """;

                var spArgs = new SetPolicyArgs()
                    .WithBucket(_bucketName)
                    .WithPolicy(policyJson);

                await _minioClient.SetPolicyAsync(spArgs);

                _logger.LogInformation("Política pública configurada para bucket {BucketName}.", _bucketName);
                _bucketInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asegurando que el bucket existe");
                throw;
            }
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, string? folder = null)
        {
            try
            {
                // Asegurar que el bucket existe antes de cargar
                await EnsureBucketExistsAsync();

                // Generar nombre único para evitar colisiones
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var cleanFileName = Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_");
                var extension = Path.GetExtension(fileName);
                var uniqueFileName = $"{timestamp}_{cleanFileName}{extension}";

                // Si hay carpeta, agregar al path
                var objectName = string.IsNullOrEmpty(folder)
                    ? uniqueFileName
                    : $"{folder}/{uniqueFileName}";

                // Configurar argumentos de carga
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(imageStream)
                    .WithObjectSize(imageStream.Length)
                    .WithContentType(contentType);

                // Cargar objeto
                await _minioClient.PutObjectAsync(putObjectArgs);

                // Construir URL pública
                var protocol = "http";
                var imageUrl = $"{protocol}://{_endpoint}/{_bucketName}/{objectName}";

                _logger.LogInformation("Imagen cargada exitosamente: {ObjectName}", objectName);

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar imagen a MinIO");
                throw new Exception($"Error al cargar imagen: {ex.Message}");
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                var objectName = ExtractObjectNameFromUrl(imageUrl);

                if (string.IsNullOrEmpty(objectName))
                {
                    _logger.LogWarning("No se pudo extraer el nombre del objeto de la URL: {Url}", imageUrl);
                    return false;
                }

                var rmArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName);

                await _minioClient.RemoveObjectAsync(rmArgs);

                _logger.LogInformation("Imagen eliminada exitosamente: {ObjectName}", objectName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar imagen de MinIO");
                return false;
            }
        }

        public async Task<bool> ImageExistsAsync(string imageUrl)
        {
            try
            {
                var objectName = ExtractObjectNameFromUrl(imageUrl);

                if (string.IsNullOrEmpty(objectName))
                    return false;

                var args = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName);

                await _minioClient.StatObjectAsync(args);
                return true;
            }
            catch (ObjectNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar si la imagen existe");
                return false;
            }
        }

        public async Task<string> GetPresignedUrlAsync(string objectName, int expiryInSeconds = 3600)
        {
            try
            {
                var args = new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

                var url = await _minioClient.PresignedGetObjectAsync(args);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar URL presignada");
                throw;
            }
        }

        private string ExtractObjectNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/');

                if (segments.Length >= 3)
                {
                    return string.Join("/", segments.Skip(2));
                }

                return url;
            }
            catch
            {
                return url;
            }
        }
    }
}