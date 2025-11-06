namespace NFL_Fantasy_API.SharedSystems.StorageConfig
{
    /// <summary>
    /// Configuración de MinIO desde appsettings.json (sección "MinIO").
    /// 
    /// CONFIGURACIÓN EN appsettings.json:
    /// {
    ///   "MinIO": {
    ///     "Endpoint": "127.0.0.1:9000",
    ///     "AccessKey": "admin",
    ///     "SecretKey": "supersecret",
    ///     "UseSSL": false,
    ///     "BucketName": "nfl-fantasy-images",
    ///     "DefaultFolder": "images"
    ///   }
    /// }
    /// 
    /// REGISTRO EN Program.cs:
    /// builder.Services.Configure&lt;MinIOSettings&gt;(builder.Configuration.GetSection("MinIO"));
    /// </summary>
    public class MinIOSettings
    {
        /// <summary>
        /// Endpoint del servidor MinIO.
        /// Ejemplos: "127.0.0.1:9000", "minio.example.com:9000"
        /// </summary>
        public string Endpoint { get; set; } = "127.0.0.1:9000";

        /// <summary>
        /// Access Key (usuario) de MinIO.
        /// Por defecto: "minioadmin" o "admin"
        /// </summary>
        public string AccessKey { get; set; } = "admin";

        /// <summary>
        /// Secret Key (contraseña) de MinIO.
        /// ⚠️ NUNCA hardcodear en código, usar User Secrets o Variables de Entorno.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Usar SSL/TLS para la conexión.
        /// Debe ser true en producción.
        /// </summary>
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// Nombre del bucket donde se almacenarán las imágenes.
        /// Debe ser minúsculas, sin espacios, sin caracteres especiales.
        /// </summary>
        public string BucketName { get; set; } = "nfl-fantasy-images";

        /// <summary>
        /// Carpeta por defecto dentro del bucket (opcional).
        /// </summary>
        public string? DefaultFolder { get; set; } = "images";

        /// <summary>
        /// Protocolo para construcción de URLs públicas.
        /// "http" para desarrollo, "https" para producción.
        /// </summary>
        public string Protocol => UseSSL ? "https" : "http";
    }
}