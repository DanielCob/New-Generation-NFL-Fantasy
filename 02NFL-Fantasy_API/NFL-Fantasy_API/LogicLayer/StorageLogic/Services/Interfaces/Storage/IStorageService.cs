namespace NFL_Fantasy_API.LogicLayer.StorageLogic.Services.Interfaces.Storage
{
    /// <summary>
    /// Contrato para servicios de almacenamiento de archivos.
    /// 
    /// PROPÓSITO:
    /// - Abstracción que permite cambiar proveedores (MinIO, S3, Azure Blob, etc.)
    /// - Facilita testing con implementaciones mock
    /// - Centraliza lógica de manejo de archivos
    /// 
    /// IMPLEMENTACIONES:
    /// - StorageService: Implementación con MinIO (actual)
    /// 
    /// FUTURAS IMPLEMENTACIONES:
    /// - AzureBlobStorageService
    /// - AWSS3StorageService
    /// - LocalFileStorageService (para desarrollo)
    /// 
    /// CASOS DE USO:
    /// - Logos de equipos
    /// - Avatares de usuarios
    /// - Imágenes de perfil
    /// - Documentos adjuntos
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Carga una imagen al almacenamiento.
        /// </summary>
        /// <param name="imageStream">Stream de la imagen</param>
        /// <param name="fileName">Nombre original del archivo</param>
        /// <param name="contentType">Tipo MIME (image/jpeg, image/png, etc.)</param>
        /// <param name="folder">Carpeta opcional dentro del bucket</param>
        /// <returns>URL pública de la imagen cargada</returns>
        Task<string> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            string? folder = null);

        /// <summary>
        /// Elimina una imagen del almacenamiento.
        /// </summary>
        /// <param name="imageUrl">URL completa o nombre del objeto</param>
        /// <returns>True si se eliminó exitosamente</returns>
        Task<bool> DeleteImageAsync(string imageUrl);

        /// <summary>
        /// Verifica si una imagen existe en el almacenamiento.
        /// </summary>
        /// <param name="imageUrl">URL completa o nombre del objeto</param>
        /// <returns>True si existe</returns>
        Task<bool> ImageExistsAsync(string imageUrl);

        /// <summary>
        /// Genera una URL temporal con firma para acceso privado.
        /// </summary>
        /// <param name="imageUrl">URL completa o nombre del objeto</param>
        /// <param name="expiryInSeconds">Segundos hasta expiración (default: 1 hora)</param>
        /// <returns>URL temporal firmada</returns>
        Task<string> GetPresignedUrlAsync(
            string imageUrl,
            int expiryInSeconds = 3600);
    }
}