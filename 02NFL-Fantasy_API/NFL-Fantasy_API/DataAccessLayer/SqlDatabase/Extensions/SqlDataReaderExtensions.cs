using Microsoft.Data.SqlClient;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions
{
    /// <summary>
    /// Extensiones para SqlDataReader que facilitan lectura segura de datos desde SQL Server.
    /// 
    /// PROPÓSITO:
    /// - Evitar excepciones por columnas NULL o inexistentes
    /// - Centralizar manejo de errores en lectura de datos
    /// - Proveer valores por defecto consistentes
    /// - Simplificar código de mappers
    /// 
    /// USO:
    /// var userId = reader.GetSafeInt32("UserID");  // Retorna 0 si NULL o error
    /// var email = reader.GetSafeString("Email");   // Retorna "" si NULL o error
    /// 
    /// NOTAS:
    /// - Métodos "Safe" retornan valores por defecto (0, "", false, etc.)
    /// - Métodos "SafeNullable" retornan null cuando corresponde
    /// - Try-catch interno previene crashes por columnas faltantes
    /// </summary>
    public static class SqlDataReaderExtensions
    {
        #region Integer Types

        /// <summary>
        /// Lee un Int32 de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor entero o 0 si es NULL/no existe</returns>
        public static int GetSafeInt32(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Fallar explícitamente si columna no existe
                throw new InvalidOperationException(
                    $"Column '{columnName}' does not exist in the result set.", ex);
            }
            catch (InvalidCastException ex)
            {
                // Fallar si hay problema de conversión
                throw new InvalidOperationException(
                    $"Cannot convert column '{columnName}' to Int32.", ex);
            }
        }

        /// <summary>
        /// Lee un Int32 nullable de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor entero o null si es NULL/no existe</returns>
        public static int? GetSafeNullableInt32(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lee un SmallInt (Int16) de forma segura.
        /// Útil para columnas SMALLINT en SQL Server.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor short o 0 si es NULL/no existe</returns>
        public static short GetSafeInt16(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (short)0 : reader.GetInt16(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lee un SmallInt (Int16) nullable de forma segura.
        /// </summary>
        public static short? GetSafeNullableInt16(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt16(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lee un BigInt (Int64) de forma segura.
        /// Útil para IDs grandes o columnas BIGINT en SQL Server.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor long o 0 si es NULL/no existe</returns>
        public static long GetSafeInt64(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0L : reader.GetInt64(ordinal);
            }
            catch
            {
                return 0L;
            }
        }

        /// <summary>
        /// Lee un BigInt (Int64) nullable de forma segura.
        /// </summary>
        public static long? GetSafeNullableInt64(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lee un TinyInt (byte) de forma segura.
        /// Útil para flags, estados pequeños o columnas TINYINT en SQL Server.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor byte o 0 si es NULL/no existe</returns>
        public static byte GetSafeByte(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (byte)0 : reader.GetByte(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lee un TinyInt (byte) nullable de forma segura.
        /// </summary>
        public static byte? GetSafeNullableByte(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetByte(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region String Types

        /// <summary>
        /// Lee un String de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor string o string.Empty si es NULL/no existe</returns>
        /// <remarks>
        /// Retorna string.Empty en lugar de null para evitar NullReferenceException
        /// en código que asume strings no-null.
        /// </remarks>
        public static string GetSafeString(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Lee un String nullable de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor string o null si es NULL/no existe</returns>
        /// <remarks>
        /// Usar cuando se necesita distinguir entre NULL y string vacío.
        /// </remarks>
        public static string? GetSafeNullableString(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Boolean Types

        /// <summary>
        /// Lee un Boolean de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor bool o false si es NULL/no existe</returns>
        public static bool GetSafeBool(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lee un Boolean nullable de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor bool o null si es NULL/no existe</returns>
        public static bool? GetSafeNullableBool(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region DateTime Types

        /// <summary>
        /// Lee un DateTime de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor DateTime o DateTime.MinValue si es NULL/no existe</returns>
        /// <remarks>
        /// ADVERTENCIA: DateTime.MinValue (01/01/0001) puede no ser válido en SQL Server.
        /// Considerar usar GetSafeNullableDateTime si se necesita distinguir NULL.
        /// </remarks>
        public static DateTime GetSafeDateTime(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Lee un DateTime nullable de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor DateTime o null si es NULL/no existe</returns>
        /// <remarks>
        /// Preferir este método sobre GetSafeDateTime para columnas opcionales.
        /// </remarks>
        public static DateTime? GetSafeNullableDateTime(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Guid Types

        /// <summary>
        /// Lee un Guid (UNIQUEIDENTIFIER) de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor Guid o Guid.Empty si es NULL/no existe</returns>
        public static Guid GetSafeGuid(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? Guid.Empty : reader.GetGuid(ordinal);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Lee un Guid nullable de forma segura.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor Guid o null si es NULL/no existe</returns>
        public static Guid? GetSafeNullableGuid(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Decimal Types

        /// <summary>
        /// Lee un Decimal de forma segura.
        /// Útil para columnas DECIMAL, NUMERIC o MONEY en SQL Server.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a leer</param>
        /// <returns>Valor decimal o 0m si es NULL/no existe</returns>
        public static decimal GetSafeDecimal(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
            }
            catch
            {
                return 0m;
            }
        }

        /// <summary>
        /// Lee un Decimal nullable de forma segura.
        /// </summary>
        public static decimal? GetSafeNullableDecimal(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lee un Double de forma segura.
        /// Útil para columnas FLOAT en SQL Server.
        /// </summary>
        public static double GetSafeDouble(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0.0 : reader.GetDouble(ordinal);
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Lee un Double nullable de forma segura.
        /// </summary>
        public static double? GetSafeNullableDouble(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Convierte un valor a DBNull.Value si es null.
        /// Útil para preparar parámetros de Stored Procedures.
        /// </summary>
        /// <param name="value">Valor a convertir</param>
        /// <returns>El valor original o DBNull.Value si es null</returns>
        /// <example>
        /// new SqlParameter("@Email", email.ToDbNull())
        /// </example>
        public static object ToDbNull(this object? value)
        {
            return value ?? DBNull.Value;
        }

        /// <summary>
        /// Verifica si una columna existe en el SqlDataReader actual.
        /// Útil para validar schema antes de leer datos.
        /// </summary>
        /// <param name="reader">SqlDataReader activo</param>
        /// <param name="columnName">Nombre de la columna a verificar</param>
        /// <returns>True si la columna existe, false en caso contrario</returns>
        public static bool HasColumn(this SqlDataReader reader, string columnName)
        {
            try
            {
                reader.GetOrdinal(columnName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}