using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs.Images
{
    /// <summary>
    /// DTO para eliminar una imagen del almacenamiento.
    /// </summary>
    public class DeleteImageDTO
    {
        /// <summary>
        /// URL completa o path relativo de la imagen a eliminar.
        /// </summary>
        /// <example>https://minio.example.com/images/team-logos/logo123.png</example>
        [Required(ErrorMessage = "La URL de la imagen es requerida.")]
        [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres.")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}