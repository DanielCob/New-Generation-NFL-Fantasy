using System.Data;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations
{
    /// <summary>
    /// DatabaseHelper configurado como:
    /// - TODAS las lecturas y escrituras se hacen SIEMPRE contra {BaseDb}_sectorA
    /// - sectorB NUNCA es usado por la app en tiempo de ejecución
    /// - sectorB solo se mantiene mediante procesos de backup/restore externos
    /// 
    /// Mantiene:
    /// - Compatibilidad con IDatabaseHelper
    /// - Campo privado _connectionString para el reflection de tus DataAccess
    /// </summary>
    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHelper> _logger;
        private readonly string _baseConnectionString;
        private readonly string _originalDatabaseName;

        // IMPORTANTE: este es el que usan tus clases por reflection
        private readonly string _connectionString;

        private string _activeSector = "A"; // solo informativo / logging

        private const int DefaultCommandTimeoutSeconds = 60;

        /// <summary>
        /// Connection string que usa SIEMPRE sectorA.
        /// </summary>
        public string ConnectionString => _connectionString;

        public DatabaseHelper(IConfiguration configuration, ILogger<DatabaseHelper> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _baseConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var builder = new SqlConnectionStringBuilder(_baseConnectionString);
            _originalDatabaseName = builder.InitialCatalog;

            // TODA la app apunta solo a sectorA
            builder.InitialCatalog = $"{_originalDatabaseName}_sectorA";
            _connectionString = builder.ConnectionString;

            _logger.LogInformation(
                "DatabaseHelper inicializado. TODAS las operaciones (lectura/escritura) usan la BD: {Database}",
                $"{_originalDatabaseName}_sectorA");
        }

        public void SetActiveSector(string sector)
        {
            if (sector != "A" && sector != "B")
                throw new ArgumentException("Sector debe ser 'A' o 'B'", nameof(sector));

            // Solo guardamos para diagnóstico; NO cambia la conexión real.
            _activeSector = sector;
            _logger.LogWarning(
                "SetActiveSector('{Sector}') fue llamado, pero por diseño TODO sigue leyendo/escribiendo en sectorA.",
                sector);
        }

        public string GetActiveSector() => _activeSector;

        #region Stored Procedure Execution Methods

        public async Task<T?> ExecuteStoredProcedureAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper) where T : class
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
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

        public async Task<List<T>> ExecuteStoredProcedureListAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
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

        public async Task<(bool success, string? errorMessage, Dictionary<string, object?> outputValues)>
            ExecuteStoredProcedureWithOutputAsync(
                string procedureName,
                SqlParameter[] parameters)
        {
            // SIEMPRE contra sectorA
            return await ExecuteSingleWithOutputAsync(ConnectionString, procedureName, parameters);
        }

        private async Task<(bool success, string? errorMessage, Dictionary<string, object?> outputValues)>
            ExecuteSingleWithOutputAsync(string connectionString, string procedureName, SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            command.Parameters.AddRange(parameters);

            try
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                var outputValues = new Dictionary<string, object?>();
                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output ||
                        param.Direction == ParameterDirection.InputOutput)
                    {
                        outputValues[param.ParameterName] =
                            param.Value == DBNull.Value ? null : param.Value;
                    }
                }

                return (true, null, outputValues);
            }
            catch (SqlException ex)
            {
                return (false, ex.Message, new Dictionary<string, object?>());
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}", new Dictionary<string, object?>());
            }
        }

        public async Task<List<List<T>>> ExecuteStoredProcedureMultipleResultSetsAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T>[] mappers)
        {
            var allResults = new List<List<T>>();

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
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

        public async Task<int> ExecuteStoredProcedureNonQueryAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            // SIEMPRE contra sectorA
            return await ExecuteSingleNonQueryAsync(ConnectionString, procedureName, parameters);
        }

        private async Task<int> ExecuteSingleNonQueryAsync(
            string connectionString,
            string procedureName,
            SqlParameter[]? parameters)
        {
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region View / Raw Query Methods

        public async Task<List<T>> ExecuteViewAsync<T>(
            string viewOrTableName,
            Func<SqlDataReader, T> mapper,
            string? whereClause = null,
            string? orderBy = null,
            int? top = null)
        {
            var results = new List<T>();

            var query = $"SELECT {(top.HasValue ? $"TOP {top.Value}" : "")} * FROM {viewOrTableName}";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                query += $" WHERE {whereClause}";
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                query += $" ORDER BY {orderBy}";
            }

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        public async Task<List<T>> ExecuteRawQueryAsync<T>(
            string sqlQuery,
            Func<SqlDataReader, T> mapper,
            SqlParameter[]? parameters = null)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(sqlQuery, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = DefaultCommandTimeoutSeconds
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

        #region Helper Methods

        public async Task<string> ExecuteStoredProcedureForMessageAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            // SIEMPRE sectorA
            return await ExecuteStoredProcedureForMessageSingleAsync(
                ConnectionString,
                procedureName,
                parameters
            );
        }

        private async Task<string> ExecuteStoredProcedureForMessageSingleAsync(
            string connectionString,
            string procedureName,
            SqlParameter[]? parameters)
        {
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var ordinal = reader.GetOrdinal("Message");
                if (!reader.IsDBNull(ordinal))
                {
                    return reader.GetString(ordinal);
                }
            }

            return "Operación completada.";
        }

        public async Task<bool> ExistsAsync(string tableName, string whereClause)
        {
            var query = $"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName} WHERE {whereClause}) THEN 1 ELSE 0 END";

            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(query, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 30
            };

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) == 1;
        }

        public async Task ExecuteStoredProcedureWithCustomReaderAsync(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, Task> readerAction)
        {
            using var connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            await readerAction(reader);
        }

        #endregion
    }
}
