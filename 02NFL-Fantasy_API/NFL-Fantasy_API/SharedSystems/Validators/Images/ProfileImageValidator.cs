namespace NFL_Fantasy_API.SharedSystems.Validators.Images
{
    /// <summary>
    /// Validador centralizado de imágenes de perfil.
    /// </summary>
    public static class ProfileImageValidator
    {
        private const int MinDimension = 300;
        private const int MaxDimension = 1024;
        private const int MaxSizeBytes = 5242880; // 5 MB

        public static List<string> ValidateProfileImage(
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

                // Validar ancho
                if (imageWidth.Value < MinDimension || imageWidth.Value > MaxDimension)
                {
                    errors.Add($"El ancho de imagen debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                // Validar alto
                if (imageHeight.Value < MinDimension || imageHeight.Value > MaxDimension)
                {
                    errors.Add($"El alto de imagen debe estar entre {MinDimension} y {MaxDimension} píxeles.");
                }

                // Validar tamaño
                if (imageBytes.Value > MaxSizeBytes)
                {
                    errors.Add("El tamaño de imagen no puede superar 5MB.");
                }
            }

            return errors;
        }
    }
}