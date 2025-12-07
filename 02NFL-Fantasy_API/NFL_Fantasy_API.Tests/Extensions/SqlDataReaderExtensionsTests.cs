using System.Data;
using System.Data.Common;
using FluentAssertions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using Xunit;

namespace NFL_Fantasy_API.Tests.Extensions
{
    /// <summary>
    /// Tests unitarios para SqlDataReaderExtensions
    /// Cobertura: Lectura segura de datos desde SQL Server
    /// 
    /// NOTA: Estos tests usan DataTable + SqlDataReader para simular 
    /// resultados de SQL sin necesidad de conexión real a BD
    /// </summary>
    public class SqlDataReaderExtensionsTests : IDisposable
    {
        private DataTable? _testTable;
        private DbDataReader? _reader;

        public void Dispose()
        {
            _reader?.Close();
            _reader?.Dispose();
            _testTable?.Dispose();
        }

        #region Helper Methods

        /// <summary>
        /// Crea un SqlDataReader de prueba con datos específicos
        /// </summary>
        private DbDataReader CreateTestReader(Action<DataTable> setupAction)
        {
            _testTable = new DataTable();
            setupAction(_testTable);

            var dataReader = _testTable.CreateDataReader();
            return dataReader;
        }

        #endregion

        #region GetSafeInt32 Tests

        [Fact]
        public void GetSafeInt32_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(12345);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeInt32("UserID");

            // Assert
            result.Should().Be(12345);
        }

        [Fact]
        public void GetSafeInt32_ConValorNulo_DebeRetornarCero()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeInt32("UserID");

            // Assert
            result.Should().Be(0, "valores NULL deben retornar 0");
        }

        [Fact]
        public void GetSafeInt32_ConColumnaInexistente_DebeLanzarExcepcion()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(123);
            });
            _reader.Read();

            // Act & Assert
            var action = () => _reader.GetSafeInt32("NonExistentColumn");

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*does not exist*");
        }

        #endregion

        #region GetSafeNullableInt32 Tests

        [Fact]
        public void GetSafeNullableInt32_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(12345);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableInt32("UserID");

            // Assert
            result.Should().Be(12345);
        }

        [Fact]
        public void GetSafeNullableInt32_ConValorNulo_DebeRetornarNull()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableInt32("UserID");

            // Assert
            result.Should().BeNull("valores NULL deben retornar null en métodos Nullable");
        }

        #endregion

        #region GetSafeInt64 Tests

        [Fact]
        public void GetSafeInt64_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("NewsID", typeof(long));
                table.Rows.Add(9876543210L);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeInt64("NewsID");

            // Assert
            result.Should().Be(9876543210L);
        }

        [Fact]
        public void GetSafeInt64_ConValorNulo_DebeRetornarCero()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("NewsID", typeof(long));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeInt64("NewsID");

            // Assert
            result.Should().Be(0L);
        }

        #endregion

        #region GetSafeByte Tests

        [Fact]
        public void GetSafeByte_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("DesignationID", typeof(byte));
                table.Rows.Add((byte)5);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeByte("DesignationID");

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public void GetSafeByte_ConValorNulo_DebeRetornarCero()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("DesignationID", typeof(byte));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeByte("DesignationID");

            // Assert
            result.Should().Be((byte)0);
        }

        #endregion

        #region GetSafeString Tests

        [Fact]
        public void GetSafeString_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("Name", typeof(string));
                table.Rows.Add("Patrick Mahomes");
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeString("Name");

            // Assert
            result.Should().Be("Patrick Mahomes");
        }

        [Fact]
        public void GetSafeString_ConValorNulo_DebeRetornarStringVacio()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("Name", typeof(string));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeString("Name");

            // Assert
            result.Should().Be(string.Empty, "valores NULL deben retornar string.Empty");
        }

        [Fact]
        public void GetSafeString_ConColumnaInexistente_DebeRetornarStringVacio()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("Name", typeof(string));
                table.Rows.Add("Test");
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeString("NonExistentColumn");

            // Assert
            result.Should().Be(string.Empty, "columnas inexistentes deben retornar string.Empty");
        }

        #endregion

        #region GetSafeNullableString Tests

        [Fact]
        public void GetSafeNullableString_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("InjurySummary", typeof(string));
                table.Rows.Add("Tobillo derecho");
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableString("InjurySummary");

            // Assert
            result.Should().Be("Tobillo derecho");
        }

        [Fact]
        public void GetSafeNullableString_ConValorNulo_DebeRetornarNull()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("InjurySummary", typeof(string));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableString("InjurySummary");

            // Assert
            result.Should().BeNull("valores NULL deben retornar null en métodos Nullable");
        }

        #endregion

        #region GetSafeBool Tests

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void GetSafeBool_ConValoresValidos_DebeRetornarValor(bool input, bool expected)
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("IsInjury", typeof(bool));
                table.Rows.Add(input);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeBool("IsInjury");

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetSafeBool_ConValorNulo_DebeRetornarFalse()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("IsInjury", typeof(bool));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeBool("IsInjury");

            // Assert
            result.Should().BeFalse("valores NULL deben retornar false");
        }

        #endregion

        #region GetSafeDateTime Tests

        [Fact]
        public void GetSafeDateTime_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            var testDate = new DateTime(2024, 12, 6, 10, 30, 0);
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("CreatedAt", typeof(DateTime));
                table.Rows.Add(testDate);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeDateTime("CreatedAt");

            // Assert
            result.Should().Be(testDate);
        }

        [Fact]
        public void GetSafeDateTime_ConValorNulo_DebeRetornarDateTimeMinValue()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("CreatedAt", typeof(DateTime));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeDateTime("CreatedAt");

            // Assert
            result.Should().Be(DateTime.MinValue);
        }

        #endregion

        #region GetSafeNullableDateTime Tests

        [Fact]
        public void GetSafeNullableDateTime_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            var testDate = new DateTime(2024, 12, 6, 10, 30, 0);
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UpdatedAt", typeof(DateTime));
                table.Rows.Add(testDate);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableDateTime("UpdatedAt");

            // Assert
            result.Should().Be(testDate);
        }

        [Fact]
        public void GetSafeNullableDateTime_ConValorNulo_DebeRetornarNull()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UpdatedAt", typeof(DateTime));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeNullableDateTime("UpdatedAt");

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetSafeDecimal Tests

        [Fact]
        public void GetSafeDecimal_ConValorValido_DebeRetornarValor()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("Price", typeof(decimal));
                table.Rows.Add(99.99m);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeDecimal("Price");

            // Assert
            result.Should().Be(99.99m);
        }

        [Fact]
        public void GetSafeDecimal_ConValorNulo_DebeRetornarCero()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("Price", typeof(decimal));
                table.Rows.Add(DBNull.Value);
            });
            _reader.Read();

            // Act
            var result = _reader.GetSafeDecimal("Price");

            // Assert
            result.Should().Be(0m);
        }

        #endregion

        #region HasColumn Tests

        [Fact]
        public void HasColumn_ConColumnaExistente_DebeRetornarTrue()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Columns.Add("Name", typeof(string));
                table.Rows.Add(1, "Test");
            });
            _reader.Read();

            // Act
            var result = _reader.HasColumn("UserID");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasColumn_ConColumnaInexistente_DebeRetornarFalse()
        {
            // Arrange
            _reader = CreateTestReader(table =>
            {
                table.Columns.Add("UserID", typeof(int));
                table.Rows.Add(1);
            });
            _reader.Read();

            // Act
            var result = _reader.HasColumn("NonExistentColumn");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ToDbNull Tests

        [Fact]
        public void ToDbNull_ConValorNoNulo_DebeRetornarMismoValor()
        {
            // Arrange
            object value = "Test String";

            // Act
            var result = value.ToDbNull();

            // Assert
            result.Should().Be("Test String");
        }

        [Fact]
        public void ToDbNull_ConValorNulo_DebeRetornarDBNull()
        {
            // Arrange
            object? value = null;

            // Act
            var result = value.ToDbNull();

            // Assert
            result.Should().Be(DBNull.Value);
        }

        [Fact]
        public void ToDbNull_ConStringNulo_DebeRetornarDBNull()
        {
            // Arrange
            string? value = null;

            // Act
            var result = value.ToDbNull();

            // Assert
            result.Should().Be(DBNull.Value);
        }

        [Fact]
        public void ToDbNull_ConIntNullable_DebeRetornarDBNull()
        {
            // Arrange
            int? value = null;

            // Act
            var result = value.ToDbNull();

            // Assert
            result.Should().Be(DBNull.Value);
        }

        #endregion
    }
}