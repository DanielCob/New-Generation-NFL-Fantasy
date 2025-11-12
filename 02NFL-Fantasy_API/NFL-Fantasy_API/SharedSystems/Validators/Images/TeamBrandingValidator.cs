using NFL_Fantasy_API.Models.DTOs.Fantasy;

namespace NFL_Fantasy_API.SharedSystems.Validators.Images
{
    /// <summary>
    /// Validador centralizado de imágenes de branding de equipos.
    /// </summary>
    public static class TeamBrandingValidator
    {
        private const int MinDimension = 300;
        private const int MaxDimension = 1024;
        private const int MaxSizeBytes = 5242880; // 5 MB

        /// <summary>
        /// Valida las dimensiones de imagen de equipo.
        /// </summary>
        public static List<string> ValidateTeamImage(
            int? imageWidth,
            int? imageHeight,
            int? imageBytes)
        {
            var errors = new List<string>();

            // Si hay bytes, debe haber dimensiones
            if (imageBytes.HasValue)
            {
                if (!imageWidth.HasValue || !imageHeight.HasValue)
                {
                    errors.Add("Si proporciona tamaño de imagen, debe incluir ancho y alto.");
                    return errors;
                }

                // Validar dimensiones (reutilizando lógica similar a ProfileImageValidator)
                if (imageWidth.Value < MinDimension || imageWidth.Value > MaxDimension)
                {
                    errors.Add($"El ancho de imagen debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                if (imageHeight.Value < MinDimension || imageHeight.Value > MaxDimension)
                {
                    errors.Add($"El alto de imagen debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                if (imageBytes.Value > MaxSizeBytes)
                {
                    errors.Add("El tamaño de imagen no puede superar 5MB.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valida las dimensiones del thumbnail.
        /// </summary>
        public static List<string> ValidateThumbnail(
            int? thumbWidth,
            int? thumbHeight,
            int? thumbBytes)
        {
            var errors = new List<string>();

            // Si hay bytes de thumbnail, debe haber dimensiones
            if (thumbBytes.HasValue)
            {
                if (!thumbWidth.HasValue || !thumbHeight.HasValue)
                {
                    errors.Add("Si proporciona tamaño de thumbnail, debe incluir ancho y alto.");
                    return errors;
                }

                // Validar dimensiones
                if (thumbWidth.Value < MinDimension || thumbWidth.Value > MaxDimension)
                {
                    errors.Add($"El ancho del thumbnail debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                if (thumbHeight.Value < MinDimension || thumbHeight.Value > MaxDimension)
                {
                    errors.Add($"El alto del thumbnail debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                if (thumbBytes.Value > MaxSizeBytes)
                {
                    errors.Add("El tamaño del thumbnail no puede superar 5MB.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valida todo el branding de equipo (imagen + thumbnail).
        /// </summary>
        public static List<string> ValidateTeamBranding(UpdateTeamBrandingDTO dto)
        {
            var errors = new List<string>();

            // Validar imagen principal
            var imageErrors = ValidateTeamImage(
                dto.TeamImageWidth,
                dto.TeamImageHeight,
                dto.TeamImageBytes
            );
            errors.AddRange(imageErrors);

            // Validar thumbnail
            var thumbErrors = ValidateThumbnail(
                dto.ThumbnailWidth,
                dto.ThumbnailHeight,
                dto.ThumbnailBytes
            );
            errors.AddRange(thumbErrors);

            return errors;
        }
    }
}