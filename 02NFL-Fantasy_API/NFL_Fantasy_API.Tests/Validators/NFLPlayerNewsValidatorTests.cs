using FluentAssertions;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Validators.NflDetails;

namespace NFL_Fantasy_API.Tests.Validators
{
    /// <summary>
    /// Tests unitarios para NFLPlayerNewsValidator
    /// Cobertura: Validación de noticias de jugadores
    /// </summary>
    public class NFLPlayerNewsValidatorTests
    {
        #region ValidateAddNews - Noticias Regulares

        [Fact]
        public void ValidateAddNews_ConNoticiaRegularValida_DebeRetornarListaVacia()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes tuvo una excelente práctica hoy con el equipo.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty("una noticia regular válida no debe tener errores");
        }

        [Fact]
        public void ValidateAddNews_ConTextoNulo_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = null!,
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("texto de la noticia es requerido");
        }

        [Fact]
        public void ValidateAddNews_ConTextoVacio_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "   ",
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("texto de la noticia es requerido");
        }

        [Theory]
        [InlineData("Corto")]      // 5 caracteres
        [InlineData("Muy corto")]  // 9 caracteres
        [InlineData("a")]          // 1 carácter
        public void ValidateAddNews_ConTextoMenorA10Caracteres_DebeRetornarError(string textoCorto)
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = textoCorto,
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("al menos 10 caracteres");
        }

        [Fact]
        public void ValidateAddNews_ConTextoMayorA300Caracteres_DebeRetornarError()
        {
            // Arrange
            var textoLargo = new string('a', 301); // 301 caracteres
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = textoLargo,
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("no puede exceder 300 caracteres");
        }

        [Fact]
        public void ValidateAddNews_ConTextoExactamente10Caracteres_DebeSerValido()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "1234567890", // Exactamente 10 caracteres
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty("10 caracteres es el mínimo válido");
        }

        [Fact]
        public void ValidateAddNews_ConTextoExactamente300Caracteres_DebeSerValido()
        {
            // Arrange
            var textoExacto = new string('a', 300); // Exactamente 300 caracteres
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = textoExacto,
                IsInjury = false
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty("300 caracteres es el máximo válido");
        }

        [Fact]
        public void ValidateAddNews_NoticiaRegularConInjurySummary_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes tuvo una gran práctica hoy.",
                IsInjury = false,
                InjurySummary = "Tobillo derecho", // No debería tener esto
                Designation = null
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("solo aplica para noticias de lesión");
        }

        [Fact]
        public void ValidateAddNews_NoticiaRegularConDesignation_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes tuvo una gran práctica hoy.",
                IsInjury = false,
                InjurySummary = null,
                Designation = "Q" // No debería tener esto
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("designación solo aplica para noticias de lesión");
        }

        #endregion

        #region ValidateAddNews - Noticias de Lesión

        [Theory]
        [InlineData("O")]
        [InlineData("D")]
        [InlineData("Q")]
        [InlineData("P")]
        [InlineData("FP")]
        [InlineData("IR")]
        [InlineData("PUP")]
        [InlineData("SUS")]
        public void ValidateAddNews_ConNoticiaLesionValidaYDesignacionValida_DebeRetornarListaVacia(string designation)
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = designation
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty($"designación {designation} es válida para lesiones");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionSinInjurySummary_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = null, // Falta el resumen
                Designation = "Q"
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("resumen de lesión es obligatorio");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionConInjurySummaryVacio_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "   ", // Solo espacios
                Designation = "Q"
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("resumen de lesión es obligatorio");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionConInjurySummaryMayorA30Caracteres_DebeRetornarError()
        {
            // Arrange
            var resumenLargo = new string('a', 31); // 31 caracteres
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = resumenLargo,
                Designation = "Q"
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("no puede exceder 30 caracteres");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionConInjurySummaryExactamente30Caracteres_DebeSerValido()
        {
            // Arrange
            var resumenExacto = new string('a', 30); // Exactamente 30 caracteres
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = resumenExacto,
                Designation = "Q"
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty("30 caracteres es el máximo válido para InjurySummary");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionSinDesignation_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = null // Falta la designación
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("designación es obligatoria");
        }

        [Fact]
        public void ValidateAddNews_NoticiaLesionConDesignationVacia_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = "   " // Solo espacios
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("La designación es obligatoria para noticias de lesión.");
        }

        [Theory]
        [InlineData("INVALID")]
        [InlineData("X")]
        [InlineData("123")]
        [InlineData("OUT")]
        [InlineData("QUESTIONABLE")]
        public void ValidateAddNews_NoticiaLesionConDesignacionInvalida_DebeRetornarError(string invalidDesignation)
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = invalidDesignation
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().ContainSingle()
                .Which.Should().Contain("Designación inválida");
        }

        [Theory]
        [InlineData("o", "O")]    // lowercase
        [InlineData("q", "Q")]    // lowercase
        [InlineData("ir", "IR")]  // lowercase
        [InlineData("pup", "PUP")] // lowercase
        public void ValidateAddNews_NoticiaLesionConDesignacionEnMinusculas_DebeAceptarla(string lowercase, string expected)
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = lowercase
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().BeEmpty($"designación {lowercase} debe ser aceptada como {expected}");
        }

        #endregion

        #region ValidateAddNews - Múltiples Errores

        [Fact]
        public void ValidateAddNews_ConMultiplesErrores_DebeRetornarTodosLosErrores()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 1,
                NewsText = "Corto", // Error: menos de 10 caracteres
                IsInjury = true,
                InjurySummary = null, // Error: falta resumen
                Designation = "INVALID" // Error: designación inválida
            };

            // Act
            var errors = NFLPlayerNewsValidator.ValidateAddNews(dto);

            // Assert
            errors.Should().HaveCount(3, "debe reportar todos los errores encontrados");
            errors.Should().Contain(e => e.Contains("al menos 10 caracteres"));
            errors.Should().Contain(e => e.Contains("resumen de lesión es obligatorio"));
            errors.Should().Contain(e => e.Contains("Designación inválida"));
        }

        #endregion

        #region IsValidDesignation

        [Theory]
        [InlineData("O", true)]
        [InlineData("D", true)]
        [InlineData("Q", true)]
        [InlineData("P", true)]
        [InlineData("FP", true)]
        [InlineData("IR", true)]
        [InlineData("PUP", true)]
        [InlineData("SUS", true)]
        public void IsValidDesignation_ConDesignacionesValidas_DebeRetornarTrue(string designation, bool expected)
        {
            // Act
            var result = NFLPlayerNewsValidator.IsValidDesignation(designation);

            // Assert
            result.Should().Be(expected, $"designación {designation} debe ser válida");
        }

        [Theory]
        [InlineData("INVALID")]
        [InlineData("X")]
        [InlineData("")]
        [InlineData("OUT")]
        [InlineData("123")]
        public void IsValidDesignation_ConDesignacionesInvalidas_DebeRetornarFalse(string invalidDesignation)
        {
            // Act
            var result = NFLPlayerNewsValidator.IsValidDesignation(invalidDesignation);

            // Assert
            result.Should().BeFalse($"designación {invalidDesignation} debe ser inválida");
        }

        [Fact]
        public void IsValidDesignation_ConNull_DebeRetornarFalse()
        {
            // Act
            var result = NFLPlayerNewsValidator.IsValidDesignation(null);

            // Assert
            result.Should().BeFalse("null no es una designación válida");
        }

        [Theory]
        [InlineData("o")]    // lowercase
        [InlineData("q")]    // lowercase
        [InlineData("ir")]   // lowercase
        [InlineData("pup")]  // lowercase
        public void IsValidDesignation_ConMinusculas_DebeRetornarTrue(string lowercaseDesignation)
        {
            // Act
            var result = NFLPlayerNewsValidator.IsValidDesignation(lowercaseDesignation);

            // Assert
            result.Should().BeTrue($"designación {lowercaseDesignation} en minúsculas debe ser válida");
        }

        #endregion

        #region GetDesignationDisplayName

        [Theory]
        [InlineData("O", "Fuera (OUT)")]
        [InlineData("D", "Dudoso (DOUBTFUL)")]
        [InlineData("Q", "Cuestionable (QUESTIONABLE)")]
        [InlineData("P", "Probable (PROBABLE)")]
        [InlineData("FP", "Participación Plena (FULL PRACTICE)")]
        [InlineData("IR", "Reserva de Lesionados (INJURED RESERVE)")]
        [InlineData("PUP", "Incapaz Físicamente de Jugar (PUP)")]
        [InlineData("SUS", "Suspendido (SUSPENDED)")]
        public void GetDesignationDisplayName_ConDesignacionesValidas_DebeRetornarNombreCompleto(
            string designation, string expectedName)
        {
            // Act
            var result = NFLPlayerNewsValidator.GetDesignationDisplayName(designation);

            // Assert
            result.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("INVALID")]
        [InlineData("X")]
        [InlineData("")]
        [InlineData(null)]
        public void GetDesignationDisplayName_ConDesignacionInvalida_DebeRetornarDesconocido(string? invalidDesignation)
        {
            // Act
            var result = NFLPlayerNewsValidator.GetDesignationDisplayName(invalidDesignation!);

            // Assert
            result.Should().Be("Desconocido");
        }

        [Theory]
        [InlineData("o", "Fuera (OUT)")]
        [InlineData("q", "Cuestionable (QUESTIONABLE)")]
        [InlineData("ir", "Reserva de Lesionados (INJURED RESERVE)")]
        public void GetDesignationDisplayName_ConMinusculas_DebeRetornarNombreCompleto(
            string lowercaseDesignation, string expectedName)
        {
            // Act
            var result = NFLPlayerNewsValidator.GetDesignationDisplayName(lowercaseDesignation);

            // Assert
            result.Should().Be(expectedName);
        }

        #endregion
    }
}