using FluentAssertions;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using System.Data;
using Xunit;

namespace NFL_Fantasy_API.Tests.Extensions
{
    /// <summary>
    /// Tests unitarios para SqlParameterExtensions
    /// Cobertura: Creación de SqlParameters de forma segura
    /// </summary>
    public class SqlParameterExtensionsTests
    {
        #region CreateParameter Tests

        [Fact]
        public void CreateParameter_ConValorEntero_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@UserID";
            var paramValue = 12345;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
            parameter.Direction.Should().Be(ParameterDirection.Input);
        }

        [Fact]
        public void CreateParameter_ConValorString_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@NewsText";
            var paramValue = "Mahomes tuvo una excelente práctica hoy";

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        [Fact]
        public void CreateParameter_ConValorBooleano_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@IsInjury";
            var paramValue = true;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        [Fact]
        public void CreateParameter_ConValorNull_DebeCrearParametroConDBNull()
        {
            // Arrange
            var paramName = "@InjurySummary";
            object? paramValue = null;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(DBNull.Value,
                "valores null deben convertirse a DBNull.Value");
        }

        [Fact]
        public void CreateParameter_ConStringNull_DebeCrearParametroConDBNull()
        {
            // Arrange
            var paramName = "@Designation";
            string? paramValue = null;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(DBNull.Value);
        }

        [Fact]
        public void CreateParameter_ConDateTime_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@CreatedAt";
            var paramValue = new DateTime(2024, 12, 6, 10, 30, 0);

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        [Fact]
        public void CreateParameter_ConDecimal_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@Amount";
            var paramValue = 99.99m;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        [Fact]
        public void CreateParameter_ConLong_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@NewsID";
            var paramValue = 9876543210L;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        [Fact]
        public void CreateParameter_ConByte_DebeCrearParametroCorrectamente()
        {
            // Arrange
            var paramName = "@DesignationID";
            var paramValue = (byte)5;

            // Act
            var parameter = SqlParameterExtensions.CreateParameter(paramName, paramValue);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.Value.Should().Be(paramValue);
        }

        #endregion

        #region CreateOutputParameter Tests

        [Fact]
        public void CreateOutputParameter_ConTipoInt_DebeCrearParametroOutputCorrectamente()
        {
            // Arrange
            var paramName = "@OutputID";
            var paramType = SqlDbType.Int;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.SqlDbType.Should().Be(paramType);
            parameter.Direction.Should().Be(ParameterDirection.Output,
                "debe ser un parámetro OUTPUT");
        }

        [Fact]
        public void CreateOutputParameter_ConTipoVarChar_DebeCrearParametroOutputCorrectamente()
        {
            // Arrange
            var paramName = "@OutputMessage";
            var paramType = SqlDbType.NVarChar;
            var size = 500;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType, size);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.SqlDbType.Should().Be(paramType);
            parameter.Direction.Should().Be(ParameterDirection.Output);
            parameter.Size.Should().Be(size, "debe establecer el tamaño especificado");
        }

        [Fact]
        public void CreateOutputParameter_ConTipoBigInt_DebeCrearParametroOutputCorrectamente()
        {
            // Arrange
            var paramName = "@OutputNewsID";
            var paramType = SqlDbType.BigInt;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.SqlDbType.Should().Be(paramType);
            parameter.Direction.Should().Be(ParameterDirection.Output);
        }

        [Fact]
        public void CreateOutputParameter_ConTipoBit_DebeCrearParametroOutputCorrectamente()
        {
            // Arrange
            var paramName = "@OutputSuccess";
            var paramType = SqlDbType.Bit;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType);

            // Assert
            parameter.Should().NotBeNull();
            parameter.ParameterName.Should().Be(paramName);
            parameter.SqlDbType.Should().Be(paramType);
            parameter.Direction.Should().Be(ParameterDirection.Output);
        }

        [Fact]
        public void CreateOutputParameter_SinSize_NoDebeEstablecerSize()
        {
            // Arrange
            var paramName = "@OutputID";
            var paramType = SqlDbType.Int;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType);

            // Assert
            parameter.Size.Should().Be(0, "no debe tener tamaño si no se especifica");
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(4000)]
        public void CreateOutputParameter_ConDiferentesSizes_DebeEstablecerSizeCorrectamente(int size)
        {
            // Arrange
            var paramName = "@OutputMessage";
            var paramType = SqlDbType.NVarChar;

            // Act
            var parameter = SqlParameterExtensions.CreateOutputParameter(paramName, paramType, size);

            // Assert
            parameter.Size.Should().Be(size);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CreateParameter_Array_DebeCrearMultiplesParametros()
        {
            // Arrange & Act
            var parameters = new[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", 1),
                SqlParameterExtensions.CreateParameter("@NFLPlayerID", 100),
                SqlParameterExtensions.CreateParameter("@NewsText", "Test news"),
                SqlParameterExtensions.CreateParameter("@IsInjury", true),
                SqlParameterExtensions.CreateParameter("@InjurySummary", (object?)null),
                SqlParameterExtensions.CreateParameter("@Designation", "Q")
            };

            // Assert
            parameters.Should().HaveCount(6);
            parameters[0].ParameterName.Should().Be("@ActorUserID");
            parameters[0].Value.Should().Be(1);

            parameters[4].ParameterName.Should().Be("@InjurySummary");
            parameters[4].Value.Should().Be(DBNull.Value);

            parameters[5].ParameterName.Should().Be("@Designation");
            parameters[5].Value.Should().Be("Q");
        }

        [Fact]
        public void CreateParameter_ConNombresVariados_DebeManejarTodosLosFormatos()
        {
            // Arrange & Act
            var param1 = SqlParameterExtensions.CreateParameter("@Param1", 1);
            var param2 = SqlParameterExtensions.CreateParameter("Param2", 2); // Sin @
            var param3 = SqlParameterExtensions.CreateParameter("@ParamWith_Underscore", 3);
            var param4 = SqlParameterExtensions.CreateParameter("@ParamWithNumber123", 4);

            // Assert
            param1.ParameterName.Should().Be("@Param1");
            param2.ParameterName.Should().Be("Param2");
            param3.ParameterName.Should().Be("@ParamWith_Underscore");
            param4.ParameterName.Should().Be("@ParamWithNumber123");
        }

        #endregion
    }
}