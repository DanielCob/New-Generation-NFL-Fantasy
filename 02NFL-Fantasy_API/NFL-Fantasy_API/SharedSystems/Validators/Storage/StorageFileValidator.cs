namespace NFL_Fantasy_API.SharedSystems.Validators.Storage
{
    /// <summary>
    /// Validador centralizado para archivos subidos a Storage (imágenes y JSON).
    /// 
    /// RESPONSABILIDADES:
    /// - Validar tamaño máximo
    /// - Validar MIME type permitido
    /// - Validar extensión permitida
    /// 
    /// NOTA:
    /// - No realiza I/O ni llamadas externas
    /// - No conoce detalles del proveedor (MinIO, S3, etc.)
    /// </summary>
    public static class StorageFileValidator
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;    // 5MB
        private const long MaxJsonSizeBytes = 10 * 1024 * 1024;   // 10MB

        private static readonly string[] AllowedImageMimeTypes =
            { "image/jpeg", "image/png", "image/jpg" };

        private static readonly string[] AllowedImageExtensions =
            { ".jpg", ".jpeg", ".png" };

        private static readonly string[] AllowedJsonMimeTypes =
            { "application/json", "text/json" };

        private static readonly string[] AllowedJsonExtensions =
            { ".json" };

        /// <summary>
        /// Valida un archivo de imagen subido vía multipart/form-data.
        /// </summary>
        public static List<string> ValidateImageFile(IFormFile? file)
        {
            var errors = new List<string>();

            if (file == null)
            {
                errors.Add("Archivo de imagen no proporcionado.");
                return errors;
            }

            if (file.Length == 0)
            {
                errors.Add("El archivo de imagen está vacío.");
                return errors;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                errors.Add("La imagen no puede superar 5MB.");
            }

            var contentType = file.ContentType?.ToLower() ?? string.Empty;
            if (!AllowedImageMimeTypes.Contains(contentType))
            {
                errors.Add("Solo se permiten imágenes JPEG y PNG.");
            }

            var fileExtension = Path.GetExtension(file.FileName ?? string.Empty).ToLower();
            if (!AllowedImageExtensions.Contains(fileExtension))
            {
                errors.Add("Extensión de archivo no permitida para imagen.");
            }

            return errors;
        }

        /// <summary>
        /// Valida un archivo JSON subido vía multipart/form-data.
        /// </summary>
        public static List<string> ValidateJsonFile(IFormFile? file)
        {
            var errors = new List<string>();

            if (file == null)
            {
                errors.Add("Archivo JSON no proporcionado.");
                return errors;
            }

            if (file.Length == 0)
            {
                errors.Add("El archivo JSON está vacío.");
                return errors;
            }

            if (file.Length > MaxJsonSizeBytes)
            {
                errors.Add("El archivo JSON no puede superar 10MB.");
            }

            var contentType = file.ContentType?.ToLower() ?? string.Empty;
            if (!AllowedJsonMimeTypes.Contains(contentType))
            {
                errors.Add("Solo se permiten archivos JSON.");
            }

            var fileExtension = Path.GetExtension(file.FileName ?? string.Empty).ToLower();
            if (!AllowedJsonExtensions.Contains(fileExtension))
            {
                errors.Add("Extensión de archivo no permitida. Solo se permite .json.");
            }

            return errors;
        }
    }
}
