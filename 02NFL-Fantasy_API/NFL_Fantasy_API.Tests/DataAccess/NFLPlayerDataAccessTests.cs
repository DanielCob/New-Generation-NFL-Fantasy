using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Moq;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;
using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.Tests.DataAccess
{
    /// <summary>
    /// Tests unitarios para NFLPlayerDataAccess
    /// Cobertura: Métodos de acceso a datos para noticias de jugadores
    /// 
    /// ESTRATEGIA DE TESTING:
    /// - Mock de IDatabaseHelper para simular ejecución de SPs
    /// - Validación de parámetros SQL construidos
    /// - Verificación de mapeo de resultados
    /// </summary>
    public class NFLPlayerDataAccessTests
    {
        private readonly Mock<IDatabaseHelper> _mockDbHelper;
        private readonly NFLPlayerDataAccess _dataAccess;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public NFLPlayerDataAccessTests()
        {
            _mockDbHelper = new Mock<IDatabaseHelper>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Inyectar el mock de IDatabaseHelper
            _dataAccess = new NFLPlayerDataAccess(_mockConfiguration.Object, _mockDbHelper.Object);
        }

        #region AddPlayerNewsAsync Tests

        [Fact]
        public async Task AddPlayerNewsAsync_ConDatosValidos_DebeEjecutarSPCorrectamente()
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

            var expectedResult = new AddNFLPlayerNewsResponseDTO
            {
                NewsID = 1,
                Message = "Noticia agregada exitosamente."
            };

            // Configurar el mock para retornar el resultado esperado
            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    "app.sp_AddNFLPlayerNews",
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _dataAccess.AddPlayerNewsAsync(
                dto,
                actorUserId: 1,
                sourceIp: "192.168.1.1",
                userAgent: "Test Agent"
            );

            // Assert
            result.Should().NotBeNull();
            result!.NewsID.Should().Be(1);
            result.Message.Should().Be("Noticia agregada exitosamente.");

            // Verificar que se llamó al SP correcto
            _mockDbHelper.Verify(
                db => db.ExecuteStoredProcedureAsync(
                    "app.sp_AddNFLPlayerNews",
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()),
                Times.Once,
                "Debe ejecutar el SP sp_AddNFLPlayerNews exactamente una vez"
            );
        }

        [Fact]
        public async Task AddPlayerNewsAsync_DebeEnviarParametrosCorrectos()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó el tobillo en práctica.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = "Q"
            };

            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" });

            // Act
            await _dataAccess.AddPlayerNewsAsync(
                dto,
                actorUserId: 1,
                sourceIp: "192.168.1.1",
                userAgent: "Test Agent"
            );

            // Assert
            capturedParameters.Should().NotBeNull();
            capturedParameters.Should().HaveCount(8, "Debe enviar 8 parámetros al SP");

            // Verificar cada parámetro
            var actorUserIdParam = capturedParameters!.First(p => p.ParameterName == "@ActorUserID");
            actorUserIdParam.Value.Should().Be(1);

            var nflPlayerIdParam = capturedParameters.First(p => p.ParameterName == "@NFLPlayerID");
            nflPlayerIdParam.Value.Should().Be(100);

            var newsTextParam = capturedParameters.First(p => p.ParameterName == "@NewsText");
            newsTextParam.Value.Should().Be("Mahomes se lesionó el tobillo en práctica.");

            var isInjuryParam = capturedParameters.First(p => p.ParameterName == "@IsInjury");
            isInjuryParam.Value.Should().Be(true);

            var injurySummaryParam = capturedParameters.First(p => p.ParameterName == "@InjurySummary");
            injurySummaryParam.Value.Should().Be("Tobillo derecho");

            var designationParam = capturedParameters.First(p => p.ParameterName == "@Designation");
            designationParam.Value.Should().Be("Q");

            var sourceIpParam = capturedParameters.First(p => p.ParameterName == "@SourceIp");
            sourceIpParam.Value.Should().Be("192.168.1.1");

            var userAgentParam = capturedParameters.First(p => p.ParameterName == "@UserAgent");
            userAgentParam.Value.Should().Be("Test Agent");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConNoticiaRegular_DebeEnviarNullsEnCamposOpcionales()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes tuvo una excelente práctica.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" });

            // Act
            await _dataAccess.AddPlayerNewsAsync(dto, 1, null, null);

            // Assert
            capturedParameters.Should().NotBeNull();

            var injurySummaryParam = capturedParameters!.First(p => p.ParameterName == "@InjurySummary");
            injurySummaryParam.Value.Should().Be(DBNull.Value, "InjurySummary debe ser DBNull cuando es null");

            var designationParam = capturedParameters.First(p => p.ParameterName == "@Designation");
            designationParam.Value.Should().Be(DBNull.Value, "Designation debe ser DBNull cuando es null");

            var sourceIpParam = capturedParameters.First(p => p.ParameterName == "@SourceIp");
            sourceIpParam.Value.Should().Be(DBNull.Value, "SourceIp debe ser DBNull cuando es null");

            var userAgentParam = capturedParameters.First(p => p.ParameterName == "@UserAgent");
            userAgentParam.Value.Should().Be(DBNull.Value, "UserAgent debe ser DBNull cuando es null");
        }

        [Fact]
        public async Task AddPlayerNewsAsync_ConResultadoNull_DebeRetornarNull()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news",
                IsInjury = false
            };

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync((AddNFLPlayerNewsResponseDTO?)null);

            // Act
            var result = await _dataAccess.AddPlayerNewsAsync(dto, 1, null, null);

            // Assert
            result.Should().BeNull("Debe retornar null si el SP no retorna resultados");
        }

        [Theory]
        [InlineData(1, 100, "News text aquí", true, "Tobillo", "Q")]
        [InlineData(2, 200, "Otra noticia de prueba", false, null, null)]
        [InlineData(5, 500, "Noticia muy larga que cumple con los requisitos mínimos", true, "Rodilla izquierda", "IR")]
        public async Task AddPlayerNewsAsync_ConDiferentesCombinaciones_DebeMapearCorrectamente(
            int actorUserId, int playerId, string newsText, bool isInjury, string? injurySummary, string? designation)
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = playerId,
                NewsText = newsText,
                IsInjury = isInjury,
                InjurySummary = injurySummary,
                Designation = designation
            };

            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" });

            // Act
            await _dataAccess.AddPlayerNewsAsync(dto, actorUserId, "192.168.1.1", "Test");

            // Assert
            capturedParameters.Should().NotBeNull();

            var actorParam = capturedParameters!.First(p => p.ParameterName == "@ActorUserID");
            actorParam.Value.Should().Be(actorUserId);

            var playerParam = capturedParameters.First(p => p.ParameterName == "@NFLPlayerID");
            playerParam.Value.Should().Be(playerId);

            var textParam = capturedParameters.First(p => p.ParameterName == "@NewsText");
            textParam.Value.Should().Be(newsText);

            var injuryParam = capturedParameters.First(p => p.ParameterName == "@IsInjury");
            injuryParam.Value.Should().Be(isInjury);
        }

        #endregion

        #region DeletePlayerNewsAsync Tests

        [Fact]
        public async Task DeletePlayerNewsAsync_ConDatosValidos_DebeEjecutarSPCorrectamente()
        {
            // Arrange
            long newsId = 123;
            int actorUserId = 1;

            var expectedResult = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "Noticia eliminada exitosamente.",
                RevertedDesignation = "Q"
            };

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    "app.sp_DeleteNFLPlayerNews",
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _dataAccess.DeletePlayerNewsAsync(
                newsId,
                actorUserId,
                sourceIp: "192.168.1.1",
                userAgent: "Test Agent"
            );

            // Assert
            result.Should().NotBeNull();
            result!.Message.Should().Be("Noticia eliminada exitosamente.");
            result.RevertedDesignation.Should().Be("Q");

            _mockDbHelper.Verify(
                db => db.ExecuteStoredProcedureAsync(
                    "app.sp_DeleteNFLPlayerNews",
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_DebeEnviarParametrosCorrectos()
        {
            // Arrange
            long newsId = 456;
            int actorUserId = 2;
            string sourceIp = "10.0.0.5";
            string userAgent = "Mozilla/5.0";

            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new DeleteNFLPlayerNewsResponseDTO { Message = "OK", RevertedDesignation = null });

            // Act
            await _dataAccess.DeletePlayerNewsAsync(newsId, actorUserId, sourceIp, userAgent);

            // Assert
            capturedParameters.Should().NotBeNull();
            capturedParameters.Should().HaveCount(4, "Debe enviar 4 parámetros al SP");

            var actorParam = capturedParameters!.First(p => p.ParameterName == "@ActorUserID");
            actorParam.Value.Should().Be(actorUserId);

            var newsIdParam = capturedParameters.First(p => p.ParameterName == "@NewsID");
            newsIdParam.Value.Should().Be(newsId);

            var ipParam = capturedParameters.First(p => p.ParameterName == "@SourceIp");
            ipParam.Value.Should().Be(sourceIp);

            var agentParam = capturedParameters.First(p => p.ParameterName == "@UserAgent");
            agentParam.Value.Should().Be(userAgent);
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConSourceIpNull_DebeEnviarDBNull()
        {
            // Arrange
            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new DeleteNFLPlayerNewsResponseDTO { Message = "OK" });

            // Act
            await _dataAccess.DeletePlayerNewsAsync(1, 1, sourceIp: null, userAgent: null);

            // Assert
            capturedParameters.Should().NotBeNull();

            var ipParam = capturedParameters!.First(p => p.ParameterName == "@SourceIp");
            ipParam.Value.Should().Be(DBNull.Value);

            var agentParam = capturedParameters.First(p => p.ParameterName == "@UserAgent");
            agentParam.Value.Should().Be(DBNull.Value);
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConRevertedDesignationNull_DebeRetornarNull()
        {
            // Arrange
            var expectedResult = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "Noticia eliminada exitosamente.",
                RevertedDesignation = null // No había designación previa
            };

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _dataAccess.DeletePlayerNewsAsync(1, 1, null, null);

            // Assert
            result.Should().NotBeNull();
            result!.RevertedDesignation.Should().BeNull("Debe ser null si no había designación previa");
        }

        [Theory]
        [InlineData("Q")]
        [InlineData("O")]
        [InlineData("IR")]
        [InlineData("D")]
        [InlineData(null)]
        public async Task DeletePlayerNewsAsync_ConDiferentesRevertedDesignations_DebeMapearCorrectamente(string? revertedDesignation)
        {
            // Arrange
            var expectedResult = new DeleteNFLPlayerNewsResponseDTO
            {
                Message = "Noticia eliminada exitosamente.",
                RevertedDesignation = revertedDesignation
            };

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _dataAccess.DeletePlayerNewsAsync(1, 1, null, null);

            // Assert
            result.Should().NotBeNull();
            result!.RevertedDesignation.Should().Be(revertedDesignation);
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_ConResultadoNull_DebeRetornarNull()
        {
            // Arrange
            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .ReturnsAsync((DeleteNFLPlayerNewsResponseDTO?)null);

            // Act
            var result = await _dataAccess.DeletePlayerNewsAsync(1, 1, null, null);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(1L, 1)]
        [InlineData(999L, 5)]
        [InlineData(123456789L, 10)]
        public async Task DeletePlayerNewsAsync_ConDiferentesNewsIds_DebeEnviarCorrectamente(long newsId, int actorUserId)
        {
            // Arrange
            SqlParameter[]? capturedParameters = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedParameters = parameters)
                .ReturnsAsync(new DeleteNFLPlayerNewsResponseDTO { Message = "OK" });

            // Act
            await _dataAccess.DeletePlayerNewsAsync(newsId, actorUserId, null, null);

            // Assert
            capturedParameters.Should().NotBeNull();

            var newsIdParam = capturedParameters!.First(p => p.ParameterName == "@NewsID");
            newsIdParam.Value.Should().Be(newsId);

            var actorParam = capturedParameters.First(p => p.ParameterName == "@ActorUserID");
            actorParam.Value.Should().Be(actorUserId);
        }

        #endregion

        #region Validación de Nombres de Stored Procedures

        [Fact]
        public async Task AddPlayerNewsAsync_DebeUsarNombreDeSPCorrecto()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news text here",
                IsInjury = false
            };

            string? capturedSpName = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>(
                    (spName, parameters, mapper) => capturedSpName = spName)
                .ReturnsAsync(new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" });

            // Act
            await _dataAccess.AddPlayerNewsAsync(dto, 1, null, null);

            // Assert
            capturedSpName.Should().Be("app.sp_AddNFLPlayerNews",
                "Debe usar el nombre correcto del stored procedure");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_DebeUsarNombreDeSPCorrecto()
        {
            // Arrange
            string? capturedSpName = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>(
                    (spName, parameters, mapper) => capturedSpName = spName)
                .ReturnsAsync(new DeleteNFLPlayerNewsResponseDTO { Message = "OK" });

            // Act
            await _dataAccess.DeletePlayerNewsAsync(1, 1, null, null);

            // Assert
            capturedSpName.Should().Be("app.sp_DeleteNFLPlayerNews",
                "Debe usar el nombre correcto del stored procedure");
        }

        #endregion

        #region Tests de Integración con Mapper

        [Fact]
        public async Task AddPlayerNewsAsync_MapperFunction_DebeMapearCorrectamenteDesdeReader()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test",
                IsInjury = false
            };

            Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>? capturedMapper = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, AddNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedMapper = mapper)
                .ReturnsAsync(new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" });

            // Act
            await _dataAccess.AddPlayerNewsAsync(dto, 1, null, null);

            // Assert
            capturedMapper.Should().NotBeNull("Debe proporcionar una función mapper");
        }

        [Fact]
        public async Task DeletePlayerNewsAsync_MapperFunction_DebeMapearCorrectamenteDesdeReader()
        {
            // Arrange
            Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>? capturedMapper = null;

            _mockDbHelper
                .Setup(db => db.ExecuteStoredProcedureAsync(
                    It.IsAny<string>(),
                    It.IsAny<SqlParameter[]>(),
                    It.IsAny<Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>()))
                .Callback<string, SqlParameter[], Func<SqlDataReader, DeleteNFLPlayerNewsResponseDTO>>(
                    (sp, parameters, mapper) => capturedMapper = mapper)
                .ReturnsAsync(new DeleteNFLPlayerNewsResponseDTO { Message = "OK" });

            // Act
            await _dataAccess.DeletePlayerNewsAsync(1, 1, null, null);

            // Assert
            capturedMapper.Should().NotBeNull("Debe proporcionar una función mapper");
        }

        #endregion
    }
}
