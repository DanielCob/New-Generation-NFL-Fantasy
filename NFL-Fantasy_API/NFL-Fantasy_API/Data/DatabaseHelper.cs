// Data/DatabaseHelper.cs - VERSIÓN CORREGIDA
using Microsoft.Data.SqlClient;
using System.Data;

namespace NFL_Fantasy_API.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<T?> ExecuteStoredProcedureAsync<T>(string procedureName, SqlParameter[]? parameters, Func<SqlDataReader, T> mapper)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }

            return default(T);
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedureListAsync<T>(string procedureName, SqlParameter[]? parameters, Func<SqlDataReader, T> mapper)
        {
            var results = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        public async Task<(bool Success, string Message, Dictionary<string, object?> OutputValues)> ExecuteStoredProcedureWithOutputAsync(
            string procedureName,
            SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();

            try
            {
                await command.ExecuteNonQueryAsync();

                var outputValues = new Dictionary<string, object?>();
                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.ReturnValue)
                    {
                        outputValues[param.ParameterName] = param.Value == DBNull.Value ? null : param.Value;
                    }
                }

                return (true, "Success", outputValues);
            }
            catch (SqlException ex)
            {
                return (false, ex.Message, new Dictionary<string, object?>());
            }
        }

        public async Task<IEnumerable<T>> ExecuteViewAsync<T>(string viewName, Func<SqlDataReader, T> mapper, string? whereClause = null)
        {
            var results = new List<T>();
            var query = $"SELECT * FROM {viewName}";

            if (!string.IsNullOrEmpty(whereClause))
                query += $" WHERE {whereClause}";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        // Helper methods for safe data reading
        public static int GetSafeInt32(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? 0 : reader.GetInt32(columnName);
        }

        public static string GetSafeString(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? string.Empty : reader.GetString(columnName);
        }

        public static string? GetSafeNullableString(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetString(columnName);
        }

        public static DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? DateTime.MinValue : reader.GetDateTime(columnName);
        }

        public static bool GetSafeBool(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? false : reader.GetBoolean(columnName);
        }

        // Para campos que SQL devuelve como int (1/0) pero necesitamos como bool
        public static bool GetSafeIntToBool(SqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? false : reader.GetInt32(columnName) == 1;
        }
    }
}