using NFL_Fantasy_API.Models.DTOs;

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
        /// Carga múltiples imágenes al almacenamiento (batch).
        /// Reutiliza UploadImageAsync internamente.
        /// </summary>
        Task<ApiResponseDTO> UploadImagesBatchAsync(
            List<IFormFile> files,
            int actorUserId,
            string? folder = null);

        /// <summary>
        /// Carga un objeto de tipo json al almacenamiento.
        /// </summary>
        /// <param name="jsonStream">Stream del json</param>
        /// <param name="fileName">Nombre original del archivo</param>
        /// <param name="contentType">Tipo MIME (image/jpeg, image/png, etc.)</param>
        /// <param name="folder">Carpeta opcional dentro del bucket</param>
        /// <returns>URL pública de la imagen cargada</returns>
        Task<string> UploadJsonAsync(
            Stream jsonStream,
            string fileName,
            string contentType,
            string? folder = null);

        /// <summary>
        /// Carga múltiples archivos JSON al almacenamiento (batch).
        /// Reutiliza UploadJsonAsync internamente.
        /// </summary>
        Task<ApiResponseDTO> UploadJsonsBatchAsync(
            List<IFormFile> files,
            int actorUserId,
            string? folder = null);

        /// <summary>
        /// Elimina un objeto del almacenamiento.
        /// </summary>
        /// <param name="objectUrl">URL completa o nombre del objeto</param>
        /// <returns>True si se eliminó exitosamente</returns>
        Task<bool> DeleteObjectAsync(string objectUrl);

        /// <summary>
        /// Verifica si una imagen existe en el almacenamiento.
        /// </summary>
        /// <param name="objectUrl">URL completa o nombre del objeto</param>
        /// <returns>True si existe</returns>
        Task<bool> ObjectExistsAsync(string objectUrl);

        /// <summary>
        /// Genera una URL temporal con firma para acceso privado.
        /// </summary>
        /// <param name="objectUrl">URL completa o nombre del objeto</param>
        /// <param name="expiryInSeconds">Segundos hasta expiración (default: 1 hora)</param>
        /// <returns>URL temporal firmada</returns>
        Task<string> GetPresignedUrlAsync(
            string objectUrl,
            int expiryInSeconds = 3600);
    }
}