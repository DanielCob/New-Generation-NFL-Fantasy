using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.NflDetails;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using System.Security.Claims;

namespace NFL_Fantasy_API.Tests.Controllers
{
    /// <summary>
    /// Tests unitarios para NFLPlayerController
    /// Cobertura: Endpoints HTTP para noticias de jugadores
    /// 
    /// ESTRATEGIA DE TESTING:
    /// - Mock de INFLPlayerService para simular lógica de negocio
    /// - Mock de HttpContext para simular contexto HTTP
    /// - Validación de códigos de respuesta HTTP
    /// - Verificación de extracción de datos del contexto (UserId, IP, UserAgent)
    /// - Validación de logging
    /// - Validación de respuestas ApiResponseDTO
    /// </summary>
    public class NFLPlayerControllerTests
    {
        private readonly Mock<INFLPlayerService> _mockService;
        private readonly Mock<ILogger<NFLPlayerController>> _mockLogger;
        private readonly NFLPlayerController _controller;

        public NFLPlayerControllerTests()
        {
            _mockService = new Mock<INFLPlayerService>();
            _mockLogger = new Mock<ILogger<NFLPlayerController>>();

            _controller = new NFLPlayerController(_mockService.Object, _mockLogger.Object);

            // Configurar HttpContext mock
            SetupHttpContext();
        }

        #region Helper Methods

        /// <summary>
        /// Configura un HttpContext mock con datos de prueba
        /// </summary>
        private void SetupHttpContext(int userId = 1, string ip = "192.168.1.1", string userAgent = "Test Agent")
        {
            var httpContext = new DefaultHttpContext();

            // Configurar Claims (UserId)
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            httpContext.User = claimsPrincipal;

            // Configurar IP
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);

            // Configurar UserAgent
            httpContext.Request.Headers["User-Agent"] = userAgent;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #endregion

        #region AddPlayerNews Tests

        [Fact]
        public async Task AddPlayerNews_ConDatosValidos_DebeRetornarCreatedAtAction()
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

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "Noticia creada exitosamente.",
                new AddNFLPlayerNewsResponseDTO
                {
                    NewsID = 123,
                    Message = "Noticia creada exitosamente."
                }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.AddPlayerNews(dto);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<CreatedAtActionResult>();

            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.StatusCode.Should().Be(201);
            createdResult.Value.Should().Be(serviceResponse);
        }

        [Fact]
        public async Task AddPlayerNews_ConDatosValidos_DebeInvocarServiceConParametrosCorrectos()
        {
            // Arrange
            SetupHttpContext(userId: 5, ip: "10.0.0.1", userAgent: "Mozilla/5.0");

            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes se lesionó el tobillo.",
                IsInjury = true,
                InjurySummary = "Tobillo derecho",
                Designation = "Q"
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockService.Verify(
                s => s.AddPlayerNewsAsync(
                    It.Is<AddNFLPlayerNewsDTO>(d =>
                        d.NFLPlayerID == 100 &&
                        d.NewsText == "Mahomes se lesionó el tobillo." &&
                        d.IsInjury == true &&
                        d.InjurySummary == "Tobillo derecho" &&
                        d.Designation == "Q"),
                    5,  // UserId del HttpContext
                    "10.0.0.1",  // IP del HttpContext
                    "Mozilla/5.0"),  // UserAgent del HttpContext
                Times.Once,
                "Debe invocar el service con los parámetros correctos extraídos del HttpContext"
            );
        }

        [Fact]
        public async Task AddPlayerNews_ConExito_DebeLoguearInformacion()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news",
                IsInjury = true,
                InjurySummary = "Test",
                Designation = "Q"
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 456, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("added news") &&
                        v.ToString()!.Contains("100") &&
                        v.ToString()!.Contains("IsInjury=True") &&
                        v.ToString()!.Contains("Designation=Q")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear información sobre la noticia creada"
            );
        }

        [Fact]
        public async Task AddPlayerNews_ConExito_DebeRetornarActionNameCorrecto()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news text here",
                IsInjury = false
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 789, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.AddPlayerNews(dto);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be("GetPlayerNewsById");
            createdResult.RouteValues.Should().ContainKey("newsId");
            createdResult.RouteValues!["newsId"].Should().Be(789);
        }

        [Fact]
        public async Task AddPlayerNews_ConServiceRetornandoNull_DebeRetornarBadRequest()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news",
                IsInjury = false
            };

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((ApiResponseDTO?)null);

            // Act
            var result = await _controller.AddPlayerNews(dto);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponseDTO;
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("No se pudo agregar la noticia");
        }

        [Fact]
        public async Task AddPlayerNews_ConServiceRetornandoError_DebeRetornarBadRequest()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news",
                IsInjury = false
            };

            var serviceResponse = ApiResponseDTO.ErrorResponse("Error de validación: texto muy corto");

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.AddPlayerNews(dto);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be(serviceResponse);
        }

        [Fact]
        public async Task AddPlayerNews_ConNoticiaRegular_NoDebeLoguearDesignation()
        {
            // Arrange
            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Mahomes tuvo una práctica normal.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("IsInjury=False")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Theory]
        [InlineData(1, "192.168.1.1", "Chrome")]
        [InlineData(5, "10.0.0.1", "Firefox")]
        [InlineData(10, "172.16.0.1", "Safari")]
        public async Task AddPlayerNews_ConDiferentesHttpContexts_DebeExtraerDatosCorrectamente(
            int userId, string ip, string userAgent)
        {
            // Arrange
            SetupHttpContext(userId, ip, userAgent);

            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news text here",
                IsInjury = false
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockService.Verify(
                s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    userId,
                    ip,
                    userAgent),
                Times.Once
            );
        }

        #endregion

        #region DeletePlayerNews Tests

        [Fact]
        public async Task DeletePlayerNews_ConNewsIdValido_DebeRetornarOk()
        {
            // Arrange
            long newsId = 123;

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "Noticia eliminada exitosamente.",
                new DeleteNFLPlayerNewsResponseDTO
                {
                    Message = "Noticia eliminada exitosamente.",
                    RevertedDesignation = "Q"
                }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeletePlayerNews(newsId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            okResult.Value.Should().Be(serviceResponse);
        }

        [Fact]
        public async Task DeletePlayerNews_ConNewsIdValido_DebeInvocarServiceConParametrosCorrectos()
        {
            // Arrange
            SetupHttpContext(userId: 7, ip: "10.10.10.10", userAgent: "Edge");

            long newsId = 456;

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO { Message = "OK", RevertedDesignation = null }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.DeletePlayerNews(newsId);

            // Assert
            _mockService.Verify(
                s => s.DeletePlayerNewsAsync(
                    456,  // NewsId
                    7,  // UserId del HttpContext
                    "10.10.10.10",  // IP del HttpContext
                    "Edge"),  // UserAgent del HttpContext
                Times.Once,
                "Debe invocar el service con los parámetros correctos"
            );
        }

        [Fact]
        public async Task DeletePlayerNews_ConExito_DebeLoguearInformacion()
        {
            // Arrange
            long newsId = 789;

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO
                {
                    Message = "OK",
                    RevertedDesignation = "D"
                }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.DeletePlayerNews(newsId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("deleted news 789") &&
                        v.ToString()!.Contains("reverted designation to: D")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear información sobre la noticia eliminada"
            );
        }

        [Fact]
        public async Task DeletePlayerNews_ConRevertedDesignationNull_DebeLoguearNull()
        {
            // Arrange
            long newsId = 999;

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO
                {
                    Message = "OK",
                    RevertedDesignation = null
                }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.DeletePlayerNews(newsId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("deleted news 999") &&
                        v.ToString()!.Contains("reverted designation to: NULL")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "Debe loguear NULL cuando no hay designación revertida"
            );
        }

        [Fact]
        public async Task DeletePlayerNews_ConServiceRetornandoNull_DebeRetornarBadRequest()
        {
            // Arrange
            long newsId = 123;

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((ApiResponseDTO?)null);

            // Act
            var result = await _controller.DeletePlayerNews(newsId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponseDTO;
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("No se pudo eliminar la noticia");
        }

        [Fact]
        public async Task DeletePlayerNews_ConServiceRetornandoError_DebeRetornarBadRequest()
        {
            // Arrange
            long newsId = 123;

            var serviceResponse = ApiResponseDTO.ErrorResponse("Noticia no existe o ya fue eliminada.");

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeletePlayerNews(newsId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();

            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be(serviceResponse);
        }

        [Theory]
        [InlineData(1L, "Q")]
        [InlineData(999L, "O")]
        [InlineData(12345L, null)]
        public async Task DeletePlayerNews_ConDiferentesRevertedDesignations_DebeManejarCorrectamente(
            long newsId, string? revertedDesignation)
        {
            // Arrange
            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO
                {
                    Message = "OK",
                    RevertedDesignation = revertedDesignation
                }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeletePlayerNews(newsId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            var response = okResult!.Value as ApiResponseDTO;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();

            var responseData = response.Data as DeleteNFLPlayerNewsResponseDTO;
            responseData!.RevertedDesignation.Should().Be(revertedDesignation);
        }

        [Theory]
        [InlineData(1L, 1, "192.168.1.1", "Chrome")]
        [InlineData(999L, 5, "10.0.0.1", "Firefox")]
        [InlineData(12345L, 10, "172.16.0.1", "Safari")]
        public async Task DeletePlayerNews_ConDiferentesHttpContexts_DebeExtraerDatosCorrectamente(
            long newsId, int userId, string ip, string userAgent)
        {
            // Arrange
            SetupHttpContext(userId, ip, userAgent);

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO { Message = "OK", RevertedDesignation = null }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.DeletePlayerNews(newsId);

            // Assert
            _mockService.Verify(
                s => s.DeletePlayerNewsAsync(
                    newsId,
                    userId,
                    ip,
                    userAgent),
                Times.Once
            );
        }

        #endregion

        #region HttpContext Extension Tests

        [Fact]
        public async Task AddPlayerNews_DebeExtraerUserIdCorrectamenteDelHttpContext()
        {
            // Arrange
            SetupHttpContext(userId: 999);

            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news text here",
                IsInjury = false
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockService.Verify(
                s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    999,  // UserId extraído del HttpContext
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task DeletePlayerNews_DebeExtraerClientIpCorrectamenteDelHttpContext()
        {
            // Arrange
            SetupHttpContext(ip: "203.0.113.42");

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new DeleteNFLPlayerNewsResponseDTO { Message = "OK", RevertedDesignation = null }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.DeletePlayerNews(1);

            // Assert
            _mockService.Verify(
                s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    "203.0.113.42",  // IP extraída del HttpContext
                    It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task AddPlayerNews_DebeExtraerUserAgentCorrectamenteDelHttpContext()
        {
            // Arrange
            SetupHttpContext(userAgent: "Custom-Agent/1.0");

            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 100,
                NewsText = "Test news text here",
                IsInjury = false
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "OK",
                new AddNFLPlayerNewsResponseDTO { NewsID = 1, Message = "OK" }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            await _controller.AddPlayerNews(dto);

            // Assert
            _mockService.Verify(
                s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    "Custom-Agent/1.0"),  // UserAgent extraído del HttpContext
                Times.Once
            );
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task AddPlayerNews_FlujoCompleto_DesdeHttpRequestHastaRespuesta()
        {
            // Arrange
            SetupHttpContext(userId: 3, ip: "192.168.100.50", userAgent: "IntegrationTest/1.0");

            var dto = new AddNFLPlayerNewsDTO
            {
                NFLPlayerID = 200,
                NewsText = "Travis Kelce anotó 3 touchdowns en el último juego.",
                IsInjury = false,
                InjurySummary = null,
                Designation = null
            };

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "Noticia creada exitosamente.",
                new AddNFLPlayerNewsResponseDTO
                {
                    NewsID = 555,
                    Message = "Noticia creada exitosamente."
                }
            );

            _mockService
                .Setup(s => s.AddPlayerNewsAsync(
                    It.IsAny<AddNFLPlayerNewsDTO>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.AddPlayerNews(dto);

            // Assert
            // 1. Verificar respuesta HTTP
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be("GetPlayerNewsById");
            createdResult.RouteValues!["newsId"].Should().Be(555);

            // 2. Verificar invocación del service
            _mockService.Verify(
                s => s.AddPlayerNewsAsync(dto, 3, "192.168.100.50", "IntegrationTest/1.0"),
                Times.Once
            );

            // 3. Verificar logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("User 3 added news") &&
                        v.ToString()!.Contains("200")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );

            // 4. Verificar respuesta
            var response = createdResult.Value as ApiResponseDTO;
            response!.Success.Should().BeTrue();
            response.Message.Should().Be("Noticia creada exitosamente.");
        }

        [Fact]
        public async Task DeletePlayerNews_FlujoCompleto_DesdeHttpRequestHastaRespuesta()
        {
            // Arrange
            SetupHttpContext(userId: 6, ip: "172.20.10.5", userAgent: "IntegrationTest/2.0");

            long newsId = 777;

            var serviceResponse = ApiResponseDTO.SuccessResponse(
                "Noticia eliminada exitosamente.",
                new DeleteNFLPlayerNewsResponseDTO
                {
                    Message = "Noticia eliminada exitosamente.",
                    RevertedDesignation = "IR"
                }
            );

            _mockService
                .Setup(s => s.DeletePlayerNewsAsync(
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(serviceResponse);

            // Act
            var result = await _controller.DeletePlayerNews(newsId);

            // Assert
            // 1. Verificar respuesta HTTP
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);

            // 2. Verificar invocación del service
            _mockService.Verify(
                s => s.DeletePlayerNewsAsync(777, 6, "172.20.10.5", "IntegrationTest/2.0"),
                Times.Once
            );

            // 3. Verificar logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("User 6 deleted news 777") &&
                        v.ToString()!.Contains("IR")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );

            // 4. Verificar respuesta
            var response = okResult.Value as ApiResponseDTO;
            response!.Success.Should().BeTrue();
            response.Message.Should().Be("Noticia eliminada exitosamente.");

            var responseData = response.Data as DeleteNFLPlayerNewsResponseDTO;
            responseData!.RevertedDesignation.Should().Be("IR");
        }

        #endregion
    }
}