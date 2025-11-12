using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace NFL_Fantasy_API.SharedSystems.StorageConfig
{
    /// <summary>
    /// Inicializador de MinIO que se ejecuta al arrancar la aplicación.
    /// 
    /// RESPONSABILIDAD:
    /// - Crear bucket si no existe
    /// - Configurar política pública de lectura
    /// - Validar conectividad
    /// 
    /// EJECUCIÓN:
    /// Se registra como HostedService en Program.cs para ejecutarse al inicio.
    /// </summary>
    public class MinIOInitializer : IHostedService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinIOSettings _settings;
        private readonly ILogger<MinIOInitializer> _logger;

        public MinIOInitializer(
            IMinioClient minioClient,
            IOptions<MinIOSettings> settings,
            ILogger<MinIOInitializer> logger)
        {
            _minioClient = minioClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Inicializando MinIO: {Endpoint}, Bucket: {BucketName}",
                    _settings.Endpoint,
                    _settings.BucketName
                );

                // Verificar si el bucket existe
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_settings.BucketName);

                bool bucketExists = await _minioClient.BucketExistsAsync(
                    bucketExistsArgs,
                    cancellationToken
                );

                // Crear bucket si no existe
                if (!bucketExists)
                {
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_settings.BucketName);

                    await _minioClient.MakeBucketAsync(
                        makeBucketArgs,
                        cancellationToken
                    );

                    _logger.LogInformation(
                        "Bucket '{BucketName}' creado exitosamente",
                        _settings.BucketName
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Bucket '{BucketName}' ya existe",
                        _settings.BucketName
                    );
                }

                // Configurar política pública (read-only para todos)
                await ConfigurePublicPolicyAsync(cancellationToken);

                _logger.LogInformation("MinIO inicializado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al inicializar MinIO. Endpoint: {Endpoint}",
                    _settings.Endpoint
                );

                // No lanzar excepción para permitir que la app inicie
                // Las operaciones fallarán más tarde con errores descriptivos
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deteniendo MinIO Initializer");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Configura política pública de solo lectura para el bucket.
        /// </summary>
        private async Task ConfigurePublicPolicyAsync(CancellationToken cancellationToken)
        {
            try
            {
                var policyJson = $$"""
                {
                    "Version": "2012-10-17",
                    "Statement": [
                        {
                            "Effect": "Allow",
                            "Principal": {"AWS": ["*"]},
                            "Action": [
                                "s3:GetBucketLocation",
                                "s3:ListBucket"
                            ],
                            "Resource": ["arn:aws:s3:::{{_settings.BucketName}}"]
                        },
                        {
                            "Effect": "Allow",
                            "Principal": {"AWS": ["*"]},
                            "Action": ["s3:GetObject"],
                            "Resource": ["arn:aws:s3:::{{_settings.BucketName}}/*"]
                        }
                    ]
                }
                """;

                var setPolicyArgs = new SetPolicyArgs()
                    .WithBucket(_settings.BucketName)
                    .WithPolicy(policyJson);

                await _minioClient.SetPolicyAsync(
                    setPolicyArgs,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Política pública configurada para bucket '{BucketName}'",
                    _settings.BucketName
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo configurar política pública para bucket '{BucketName}'",
                    _settings.BucketName
                );
            }
        }
    }
}