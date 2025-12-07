using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces.NflDetails;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Implementations.NflDetails;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using System.Runtime.Serialization;

namespace NFL_Fantasy_API.Tests.Services
{
    /// <summary>
    /// Tests unitarios para NFLPlayerService
    /// Cobertura: Lógica de negocio para noticias de jugadores
    /// 
    /// ESTRATEGIA DE TESTING:
    /// - Mock de NFLPlayerDataAccess para simular respuestas de BD
    /// - Validación de lógica de negocio
    /// - Verificación de respuestas ApiResponseDTO
    /// - Validación de logging
    /// - Manejo de excepciones
    /// </summary>
    public class NFLPlayerServiceTests
    {
        private readonly Mock<INFLPlayerDataAccess> _mockDataAccess;
        private readonly Mock<ILogger<NFLPlayerService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly NFLPlayerService _service;

        public NFLPlayerServiceTests()
        {
            _mockDataAccess = new Mock<INFLPlayerDataAccess>();

            _mockLogger = new Mock<ILogger<NFLPlayerService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _service = new NFLPlayerService(
                _mockDataAccess.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        private static SqlException CreateSqlException()
        {
            return (SqlException)FormatterServices.GetUninitializedObject(typeof(SqlException));
        }

        #region AddPlayerNewsAsync Tests

        [Fact]
        public async Task AddPlayerNewsAsync_ConDatosValidos_DebeRetornarSuccess()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes tuvo una excelente práctica hoy.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            var dataAccessResponse = new AddNFLPlayerNewsResponseDTO
            {
                NewsID = 1,
                Message = "Noticia agregada exitosamente."
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            var result = await _service.AddPlayerNewsAsync(
                dto,
                actorUserId: 1,
                sourceIp: "192.168.1.1",
                userAgent: "Test Agent"
            );

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Noticia agregada exitosamente.");
            result.Data.Should().NotBeNull();

            var responseData = result.Data as AddNFLPlayerNewsResponseDTO;
            responseData.Should().NotBeNull();
            responseData!.NewsID.Should().Be(1);
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConDatosValidos_DebeInvocarDataAccessConParametrosCorrectos()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó el tobillo.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = "Q"
            };

            var dataAccessResponse = new AddNFLPlayerNewsResponseDTO
            {
                NewsID = 1,
                Message = "OK"
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            await _service.AddPlayerNewsAsync(
                dto,
                actorUserId: 5,
                sourceIp: "10.0.0.1",
                userAgent: "Mozilla/5.0"
            );

            // Assert
            _mockDataAccess.Verify(
                da => da.AddPlayerNewsAsync(
                    It.Is<AddNFLPlayerNewsDTO>(d =>
                        d.NFLPlayerID == 100 &&
                        d.NewsText == "Mahomes se lesionó el tobillo." &&
                        d.IsInjury == true &&
                        d.InjurySummary == "Tobillo derecho" &&
                        d.Designation == "Q"),
                    5,
                    "10.0.0.1",
                    "Mozilla/5.0"),
                Times.Once,
                "Debe invocar DataAccess exactamente una vez con los parámetros correctos"
            );
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConDatosValidos_DebeLoguearInformacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                // >= 10 caracteres para pasar la validación
                NewsText = "Test news ok", // 12 caracteres
                IsInjury = true,
                InjurySummary = "Test injury",
                Designation = "Q"
            };

            var dataAccessResponse = new AddNFLPlayerNewsResponseDTO
            {
                NewsID = 123,
                Message = "OK"
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("added news 123")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear la creación exitosa de la noticia"
            );
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConTextoVacio_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "",  // Inválido
                IsInjury = false
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("texto de la noticia es requerido");

            // No debe llamar a DataAccess si falla validación
            _mockDataAccess.Verify(
                da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never,
                "No debe invocar DataAccess si la validación falla"
            );
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConTextoMuyCorto_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Corto",  // Menos de 10 caracteres
                IsInjury = false
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("al menos 10 caracteres");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConLesionSinInjurySummary_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó en práctica.",
                IsInjury = true,
                InjurySummary = null,  // Falta el resumen
                Designation = "Q"
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("resumen de lesión es obligatorio");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConLesionSinDesignation_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo",
                Designation = null  // Falta la designación
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("designación es obligatoria");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConDesignacionInvalida_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo",
                Designation = "INVALID"  // Designación inválida
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Designación inválida");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConMultiplesErroresDeValidacion_DebeRetornarTodos()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Corto",  // Error 1: muy corto
                IsInjury = true,
                InjurySummary = null,  // Error 2: falta resumen
                Designation = "INVALID"  // Error 3: designación inválida
            };

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();

            // Debe contener indicación de múltiples errores
            var message = result.Message;
            message.Should().Contain("10 caracteres");
            message.Should().Contain("resumen");
            message.Should().Contain("Designación");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConDataAccessRetornandoNull_DebeRetornarError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Valid news text here",
                IsInjury = false
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((AddNFLPlayerNewsResponseDTO?)null);

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Error al agregar noticia");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConSqlException_DebeRetornarErrorConMensajeSQL()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Valid news text here",
                IsInjury = false
            };

            var sqlException = CreateSqlException();

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(sqlException);

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConSqlException_DebeLoguearError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Valid news text here",
                IsInjury = false
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(CreateSqlException());

            // Act
            await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SQL error")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear errores SQL"
            );
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConExcepcionGenerica_DebeRetornarErrorGenerico()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Valid news text here",
                IsInjury = false
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Error inesperado");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConExcepcionGenerica_DebeLoguearError()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Valid news text here",
                IsInjury = false
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        #endregion

        #region DeletePlayerNewsAsync Tests

        [Fact]
        public async Task DeletePlayerNewsAsync_ConNewsIdValido_DebeRetornarSuccess()
        {
            // Arrange
            long newsId = 123;
            int actorUserId = 1;

            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "Noticia eliminada exitosamente.",
                RevertedDesignation = "Q"
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            var result = await _service.DeletePlayerNewsAsync(
                newsId,
                actorUserId,
                sourceIp: "192.168.1.1",
                userAgent: "Test Agent"
            );

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Noticia eliminada exitosamente.");
            result.Data.Should().NotBeNull();

            var responseData = result.Data as DeleteNFLPlayerNewsResponseDTO;
            responseData.Should().NotBeNull();
            responseData!.RevertedDesignation.Should().Be("Q");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConNewsIdValido_DebeInvocarDataAccessConParametrosCorrectos()
        {
            // Arrange
            long newsId = 456;
            int actorUserId = 5;
            string sourceIp = "10.0.0.1";
            string userAgent = "Mozilla/5.0";

            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "OK",
                RevertedDesignation = null
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            await _service.DeletePlayerNewsAsync(newsId, actorUserId, sourceIp, userAgent);

            // Assert
            _mockDataAccess.Verify(
                da => da.DeletePlayerNewsAsync(
                    newsId,
                    actorUserId,
                    sourceIp,
                    userAgent),
                Times.Once,
                "Debe invocar DataAccess exactamente una vez con los parámetros correctos"
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConNewsIdValido_DebeLoguearInformacion()
        {
            // Arrange
            long newsId = 789;
            int actorUserId = 1;

            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "OK",
                RevertedDesignation = "Q"
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            await _service.DeletePlayerNewsAsync(newsId, actorUserId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted news 789")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear la eliminación exitosa"
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConRevertedDesignationNull_DebeLoguearNull()
        {
            // Arrange
            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "OK",
                RevertedDesignation = null
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            await _service.DeletePlayerNewsAsync(1, 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NULL")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear NULL cuando no hay designación revertida"
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConNewsIdCero_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            long newsId = 0;  // Inválido

            // Act
            var result = await _service.DeletePlayerNewsAsync(newsId, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("ID de noticia inválido");

            // No debe llamar a DataAccess si falla validación
            _mockDataAccess.Verify(
                da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never,
                "No debe invocar DataAccess con NewsID inválido"
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConNewsIdNegativo_DebeRetornarErrorDeValidacion()
        {
            // Arrange
            long newsId = -5;  // Inválido

            // Act
            var result = await _service.DeletePlayerNewsAsync(newsId, actorUserId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("ID de noticia inválido");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConDataAccessRetornandoNull_DebeRetornarError()
        {
            // Arrange
            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((DeleteNFLPlayerNewsResponseDTO?)null);

            // Act
            var result = await _service.DeletePlayerNewsAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Error al eliminar noticia");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConSqlException_DebeRetornarErrorConMensajeSQL()
        {
            // Arrange
            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(CreateSqlException());

            // Act
            var result = await _service.DeletePlayerNewsAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConSqlException_DebeLoguearError()
        {
            // Arrange
            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(CreateSqlException());

            // Act
            await _service.DeletePlayerNewsAsync(123, 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SQL error al eliminar noticia 123")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConExcepcionGenerica_DebeRetornarErrorGenerico()
        {
            // Arrange
            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _service.DeletePlayerNewsAsync(1, 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Error inesperado");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConExcepcionGenerica_DebeLoguearError()
        {
            // Arrange
            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _service.DeletePlayerNewsAsync(456, 1);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error al eliminar noticia 456")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Theory]
        [InlineData(1L, 1, "Q")]
        [InlineData(999L, 5, "O")]
        [InlineData(12345L, 10, null)]
        public async Task DeletePlayerNewsAsync_ConDiferentesEscenarios_DebeManejarCorrectamente(
            long newsId, int actorUserId, string? revertedDesignation)
        {
            // Arrange
            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "OK",
                RevertedDesignation = revertedDesignation
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            var result = await _service.DeletePlayerNewsAsync(newsId, actorUserId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var responseData = result.Data as DeleteNFLPlayerNewsResponseDTO;
            responseData!.RevertedDesignation.Should().Be(revertedDesignation);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task AddPlayerNewsAsync_FlujosCompleto_DesdeValidacionHastaRespuesta()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes lanzó 5 touchdowns en el juego de hoy.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            var dataAccessResponse = new AddNFLPlayerNewsResponseDTO
            {
                NewsID = 999,
                Message = "Noticia agregada exitosamente."
            };

            _mockDataAccess
                .Setup(da => da.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            var result = await _service.AddPlayerNewsAsync(dto, actorUserId: 1);

            // Assert
            result.Success.Should().BeTrue();

            var responseData = result.Data as AddNFLPlayerNewsResponseDTO;
            responseData!.NewsID.Should().Be(999);

            _mockDataAccess.Verify(
                da => da.AddPlayerNewsAsync(dto, 1, null, null),
                Times.Once
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("999")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_FlujoCompleto_DesdeValidacionHastaRespuesta()
        {
            // Arrange
            long newsId = 888;
            int actorUserId = 2;

            var dataAccessResponse = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "Noticia eliminada exitosamente.",
                RevertedDesignation = "D"
            };

            _mockDataAccess
                .Setup(da => da.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(dataAccessResponse);

            // Act
            var result = await _service.DeletePlayerNewsAsync(newsId, actorUserId);

            // Assert
            result.Success.Should().BeTrue();

            var responseData = result.Data as DeleteNFLPlayerNewsResponseDTO;
            responseData!.RevertedDesignation.Should().Be("D");

            _mockDataAccess.Verify(
                da => da.DeletePlayerNewsAsync(newsId, actorUserId, null, null),
                Times.Once
            );

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("888") && v.ToString()!.Contains("D")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        #endregion
    }
}