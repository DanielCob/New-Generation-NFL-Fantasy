using Microsoft.Data.SqlClient;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces
{
    /// <summary>
    /// Contrato para el helper de base de datos (SQL Server).
    /// Debe coincidir EXACTAMENTE con las firmas implementadas en <see cref="DatabaseHelper"/>.
    /// </summary>
    public interface IDatabaseHelper
    {
        /// <summary>Cadena de conexión activa (solo lectura).</summary>
        string ConnectionString { get; }

        // ---------------------------
        // Stored Procedures
        // ---------------------------

        /// <summary>
        /// Ejecuta un SP que retorna UNA sola fila.
        /// </summary>
        Task<T?> ExecuteStoredProcedureAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper) where T : class;

        /// <summary>
        /// Ejecuta un SP que retorna MÚLTIPLES filas.
        /// </summary>
        Task<List<T>> ExecuteStoredProcedureListAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper);

        /// <summary>
        /// Ejecuta un SP con parámetros OUTPUT.
        /// </summary>
        Task<(bool success, string? errorMessage, Dictionary<string, object?> outputValues)>
            ExecuteStoredProcedureWithOutputAsync(
                string procedureName,
                SqlParameter[] parameters);

        /// <summary>
        /// Ejecuta un SP que retorna múltiples result sets.
        /// </summary>
        Task<List<List<T>>> ExecuteStoredProcedureMultipleResultSetsAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T>[] mappers);

        /// <summary>
        /// Ejecuta un SP sin result sets (INSERT/UPDATE/DELETE).
        /// </summary>
        Task<int> ExecuteStoredProcedureNonQueryAsync(
            string procedureName,
            SqlParameter[]? parameters);

        /// <summary>
        /// Ejecuta un SP y expone el SqlDataReader para manejo personalizado.
        /// </summary>
        Task ExecuteStoredProcedureWithCustomReaderAsync(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, Task> readerAction);

        /// <summary>
        /// Ejecuta un SP que retorna un mensaje en la columna "Message".
        /// </summary>
        Task<string> ExecuteStoredProcedureForMessageAsync(
            string procedureName,
            SqlParameter[]? parameters);

        // ---------------------------
        // Views / Queries
        // ---------------------------

        /// <summary>
        /// Ejecuta un SELECT contra una VIEW o tabla.
        /// </summary>
        Task<List<T>> ExecuteViewAsync<T>(
            string viewOrTableName,
            Func<SqlDataReader, T> mapper,
            string? whereClause = null,
            string? orderBy = null,
            int? top = null);

        /// <summary>
        /// Ejecuta una query SQL parametrizada (SELECT).
        /// </summary>
        Task<List<T>> ExecuteRawQueryAsync<T>(
            string sqlQuery,
            Func<SqlDataReader, T> mapper,
            SqlParameter[]? parameters = null);

        /// <summary>
        /// Verifica existencia de registros con una condición.
        /// </summary>
        Task<bool> ExistsAsync(string tableName, string whereClause);
    }
}
