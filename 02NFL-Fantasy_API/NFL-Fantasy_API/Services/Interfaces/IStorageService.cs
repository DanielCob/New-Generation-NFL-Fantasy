namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Servicio de almacenamiento de objetos (imágenes) usando MinIO
    /// Maneja la carga, descarga y eliminación de archivos
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Carga una imagen a MinIO
        /// </summary>
        /// <param name="imageStream">Stream de la imagen</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <param name="contentType">Tipo MIME (image/jpeg, image/png, etc)</param>
        /// <param name="folder">Carpeta dentro del bucket (opcional)</param>
        /// <returns>URL pública de la imagen</returns>
        Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, string? folder = null);

        /// <summary>
        /// Elimina una imagen de MinIO
        /// </summary>
        /// <param name="imageUrl">URL completa o path relativo de la imagen</param>
        /// <returns>True si se eliminó exitosamente</returns>
        Task<bool> DeleteImageAsync(string imageUrl);

        /// <summary>
        /// Verifica si una imagen existe
        /// </summary>
        /// <param name="imageUrl">URL completa o path relativo de la imagen</param>
        /// <returns>True si existe</returns>
        Task<bool> ImageExistsAsync(string imageUrl);

        /// <summary>
        /// Genera una URL con tiempo de expiración
        /// </summary>
        /// <param name="objectName">Nombre del objeto en el bucket</param>
        /// <param name="expiryInSeconds">Segundos hasta expiración</param>
        /// <returns>URL temporal firmada</returns>
        Task<string> GetPresignedUrlAsync(string objectName, int expiryInSeconds = 3600);
    }
}