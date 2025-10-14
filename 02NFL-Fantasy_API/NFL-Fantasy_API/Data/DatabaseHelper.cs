using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace NFL_Fantasy_API.Data
{
    /// <summary>
    /// Único punto de acceso a la base de datos.
    /// Proporciona métodos para ejecutar SPs y consultar Views de forma segura.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        #region Safe Getters (helpers para leer SqlDataReader de forma segura)

        /// <summary>
        /// Lee un Int32 de forma segura, retorna 0 si es NULL o no existe
        /// </summary>
        public static int GetSafeInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lee un Int32? nullable de forma segura
        /// </summary>
        public static int? GetSafeNullableInt32(SqlDataReader reader, string columnName)
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
        /// Lee un String de forma segura, retorna string.Empty si es NULL o no existe
        /// </summary>
        public static string GetSafeString(SqlDataReader reader, string columnName)
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
        /// Lee un String? nullable de forma segura
        /// </summary>
        public static string? GetSafeNullableString(SqlDataReader reader, string columnName)
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

        /// <summary>
        /// Lee un Boolean de forma segura, retorna false si es NULL o no existe
        /// </summary>
        public static bool GetSafeBool(SqlDataReader reader, string columnName)
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
        /// Lee un Boolean? nullable de forma segura
        /// </summary>
        public static bool? GetSafeNullableBool(SqlDataReader reader, string columnName)
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

        /// <summary>
        /// Lee un DateTime de forma segura, retorna DateTime.MinValue si es NULL o no existe
        /// </summary>
        public static DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
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
        /// Lee un DateTime? nullable de forma segura
        /// </summary>
        public static DateTime? GetSafeNullableDateTime(SqlDataReader reader, string columnName)
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

        /// <summary>
        /// Lee un byte (TinyInt en SQL) de forma segura
        /// </summary>
        public static byte GetSafeByte(SqlDataReader reader, string columnName)
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
        /// Lee un Guid de forma segura
        /// </summary>
        public static Guid GetSafeGuid(SqlDataReader reader, string columnName)
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
        /// Lee un Guid? nullable de forma segura
        /// </summary>
        public static Guid? GetSafeNullableGuid(SqlDataReader reader, string columnName)
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

        /// <summary>
        /// Lee un SmallInt (Int16) de forma segura
        /// </summary>
        public static short GetSafeInt16(SqlDataReader reader, string columnName)
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
        /// Lee un Decimal de forma segura
        /// </summary>
        public static decimal GetSafeDecimal(SqlDataReader reader, string columnName)
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
        /// Convierte un valor a DBNull si es null (útil para parámetros opcionales)
        /// </summary>
        public static object DbNullIfNull(object? value)
        {
            return value ?? DBNull.Value;
        }

        /// <summary>
        /// Lee un Int64 (BigInt) de forma segura
        /// </summary>
        public static long GetSafeInt64(SqlDataReader reader, string columnName)
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
        /// Lee un Int64? nullable de forma segura
        /// </summary>
        public static long? GetSafeNullableInt64(SqlDataReader reader, string columnName)
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

        #endregion

        #region Stored Procedure Execution Methods

        /// <summary>
        /// Ejecuta un SP que retorna UNA fila y la mapea con el mapper proporcionado.
        /// Útil para SPs que retornan un resultado único.
        /// </summary>
        public async Task<T?> ExecuteStoredProcedureAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper) where T : class
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }

            return null;
        }

        /// <summary>
        /// Ejecuta un SP que retorna MÚLTIPLES filas y las mapea a una lista.
        /// </summary>
        public async Task<List<T>> ExecuteStoredProcedureListAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        /// <summary>
        /// Ejecuta un SP con parámetros OUTPUT.
        /// Retorna (success, errorMessage, outputValues).
        /// Útil para SPs como sp_Login que usan OUTPUT params.
        /// </summary>
        public async Task<(bool success, string? errorMessage, Dictionary<string, object?> outputValues)>
            ExecuteStoredProcedureWithOutputAsync(
                string procedureName,
                SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            command.Parameters.AddRange(parameters);

            try
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                // Leer valores OUTPUT
                var outputValues = new Dictionary<string, object?>();
                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output ||
                        param.Direction == ParameterDirection.InputOutput)
                    {
                        outputValues[param.ParameterName] = param.Value == DBNull.Value ? null : param.Value;
                    }
                }

                return (true, null, outputValues);
            }
            catch (SqlException ex)
            {
                // SQL lanzó un error (THROW en SP)
                return (false, ex.Message, new Dictionary<string, object?>());
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}", new Dictionary<string, object?>());
            }
        }

        /// <summary>
        /// Ejecuta un SP que retorna MÚLTIPLES result sets.
        /// Útil para SPs como sp_GetUserProfile que retornan 3 tablas diferentes.
        /// </summary>
        public async Task<List<List<T>>> ExecuteStoredProcedureMultipleResultSetsAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T>[] mappers)
        {
            var allResults = new List<List<T>>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            int mapperIndex = 0;
            do
            {
                var resultSet = new List<T>();
                if (mapperIndex < mappers.Length)
                {
                    var mapper = mappers[mapperIndex];
                    while (await reader.ReadAsync())
                    {
                        resultSet.Add(mapper(reader));
                    }
                }
                allResults.Add(resultSet);
                mapperIndex++;
            }
            while (await reader.NextResultAsync());

            return allResults;
        }

        /// <summary>
        /// Ejecuta un SP que NO retorna result sets (INSERT/UPDATE/DELETE sin SELECT).
        /// Retorna el número de filas afectadas.
        /// </summary>
        public async Task<int> ExecuteStoredProcedureNonQueryAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region View Query Methods

        /// <summary>
        /// Ejecuta una consulta SELECT contra una VIEW (o tabla).
        /// IMPORTANTE: whereClause y orderBy deben ser seguros (solo usar con valores de ruta validados).
        /// Para filtros dinámicos, usar un SP con parámetros.
        /// </summary>
        public async Task<List<T>> ExecuteViewAsync<T>(
            string viewOrTableName,
            Func<SqlDataReader, T> mapper,
            string? whereClause = null,
            string? orderBy = null,
            int? top = null)
        {
            var results = new List<T>();

            // Construir query básico
            var query = $"SELECT {(top.HasValue ? $"TOP {top.Value}" : "")} * FROM {viewOrTableName}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                query += $" WHERE {whereClause}";
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                query += $" ORDER BY {orderBy}";
            }

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 60
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        /// <summary>
        /// Ejecuta una query personalizada (usar con EXTREMO cuidado).
        /// Solo para casos muy específicos donde Views y SPs no son suficientes.
        /// </summary>
        public async Task<List<T>> ExecuteRawQueryAsync<T>(
            string sqlQuery,
            Func<SqlDataReader, T> mapper,
            SqlParameter[]? parameters = null)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 60
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        #endregion

        #region Helper Methods for Complex Scenarios

        /// <summary>
        /// Ejecuta un SP y retorna solo el mensaje de resultado (común en SPs de modificación)
        /// </summary>
        public async Task<string> ExecuteStoredProcedureForMessageAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            var result = await ExecuteStoredProcedureAsync<string>(
                procedureName,
                parameters,
                reader => GetSafeString(reader, "Message")
            );

            return result ?? "Operación completada.";
        }

        /// <summary>
        /// Verifica si existe al menos un registro que cumpla la condición
        /// </summary>
        public async Task<bool> ExistsAsync(string tableName, string whereClause)
        {
            var query = $"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName} WHERE {whereClause}) THEN 1 ELSE 0 END";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 30
            };

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }
        
        #endregion
    }
}