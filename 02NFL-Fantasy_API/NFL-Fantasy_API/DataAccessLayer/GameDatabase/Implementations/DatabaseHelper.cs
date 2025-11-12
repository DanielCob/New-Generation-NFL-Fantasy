using System.Data;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations
{
    /// <summary>
    /// Punto único de acceso a la base de datos SQL Server.
    /// 
    /// RESPONSABILIDADES:
    /// - Gestionar la cadena de conexión
    /// - Ejecutar Stored Procedures con diferentes patrones de retorno
    /// - Consultar Views y tablas de forma segura
    /// - Proporcionar métodos helper para escenarios comunes
    /// 
    /// PATRONES DE USO:
    /// 1. SP que retorna UNA fila → ExecuteStoredProcedureAsync
    /// 2. SP que retorna MÚLTIPLES filas → ExecuteStoredProcedureListAsync
    /// 3. SP con parámetros OUTPUT → ExecuteStoredProcedureWithOutputAsync
    /// 4. SP que retorna MÚLTIPLES result sets → ExecuteStoredProcedureMultipleResultSetsAsync
    /// 5. SP sin result sets (INSERT/UPDATE/DELETE) → ExecuteStoredProcedureNonQueryAsync
    /// 6. Consultas a Views → ExecuteViewAsync
    /// 7. Queries custom (usar con precaución) → ExecuteRawQueryAsync
    /// 
    /// SEGURIDAD:
    /// - TODOS los métodos usan parametrización para prevenir SQL Injection
    /// - Views y queries raw deben ser validados externamente
    /// - Timeout configurado con un valor FUERTE
    /// </summary>
    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly string _connectionString;

        // Única fuente de verdad para timeouts (hardcoded)
        private const int DefaultCommandTimeoutSeconds = 60;

        /// <summary>
        /// Cadena de conexión a SQL Server (solo lectura)
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// Constructor que obtiene la cadena de conexión desde appsettings.json
        /// </summary>
        /// <param name="configuration">IConfiguration de ASP.NET Core</param>
        /// <exception cref="InvalidOperationException">Si no existe 'DefaultConnection'</exception>
        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        #region Stored Procedure Execution Methods

        /// <summary>
        /// Ejecuta un Stored Procedure que retorna UNA SOLA fila.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a mapear</typeparam>
        /// <param name="procedureName">Nombre del SP (ej: "sp_GetUserByID")</param>
        /// <param name="parameters">Parámetros del SP (puede ser null si no requiere)</param>
        /// <param name="mapper">Función que convierte SqlDataReader a T</param>
        /// <returns>Objeto T si se encontró un registro, null si no hay resultados</returns>
        /// <remarks>
        /// USO TÍPICO: Obtener un usuario por ID, obtener configuración, etc.
        /// EJEMPLO:
        /// var user = await _dbHelper.ExecuteStoredProcedureAsync(
        ///     "sp_GetUserByID",
        ///     new[] { new SqlParameter("@UserID", userId) },
        ///     reader => new User {
        ///         UserID = reader.GetSafeInt32("UserID"),
        ///         Email = reader.GetSafeString("Email")
        ///     }
        /// );
        /// </remarks>
        public async Task<T?> ExecuteStoredProcedureAsync<T>(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, T> mapper) where T : class
        {
            using var connection = new SqlConnection(_connectionString);
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

            // Solo leer la primera fila
            if (await reader.ReadAsync())
            {
                return mapper(reader);
            }

            return null;
        }

        /// <summary>
        /// Ejecuta un Stored Procedure que retorna MÚLTIPLES filas.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a mapear</typeparam>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP</param>
        /// <param name="mapper">Función que convierte SqlDataReader a T</param>
        /// <returns>Lista de objetos T (vacía si no hay resultados)</returns>
        /// <remarks>
        /// USO TÍPICO: Obtener lista de usuarios, logs, transacciones, etc.
        /// EJEMPLO:
        /// var users = await _dbHelper.ExecuteStoredProcedureListAsync(
        ///     "sp_GetAllUsers",
        ///     null,
        ///     reader => new User {
        ///         UserID = reader.GetSafeInt32("UserID"),
        ///         Email = reader.GetSafeString("Email")
        ///     }
        /// );
        /// </remarks>
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
                CommandTimeout = DefaultCommandTimeoutSeconds
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Leer todas las filas
            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        /// <summary>
        /// Ejecuta un Stored Procedure con parámetros OUTPUT.
        /// </summary>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP (incluir OUTPUT params con Direction = Output)</param>
        /// <returns>
        /// Tupla con:
        /// - success: True si no hubo excepciones
        /// - errorMessage: Mensaje de error si hubo excepción
        /// - outputValues: Diccionario con valores de parámetros OUTPUT
        /// </returns>
        /// <remarks>
        /// USO TÍPICO: sp_Login, sp_Register (SPs que retornan códigos de error/éxito)
        /// EJEMPLO:
        /// var loginParam = new SqlParameter("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output };
        /// var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
        /// 
        /// var (success, error, outputs) = await _dbHelper.ExecuteStoredProcedureWithOutputAsync(
        ///     "sp_Login",
        ///     new[] {
        ///         new SqlParameter("@Email", email),
        ///         new SqlParameter("@PasswordHash", hash),
        ///         loginParam,
        ///         errorParam
        ///     }
        /// );
        /// 
        /// bool isValid = (bool)outputs["@IsValid"];
        /// string? message = outputs["@ErrorMessage"] as string;
        /// </remarks>
        public async Task<(bool success, string? errorMessage, Dictionary<string, object?> outputValues)>
            ExecuteStoredProcedureWithOutputAsync(
                string procedureName,
                SqlParameter[] parameters)
        {
            using var connection = new SqlConnection(_connectionString);
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

                // Extraer valores OUTPUT
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
                // SQL Server lanzó un error (ej: THROW en el SP)
                return (false, ex.Message, new Dictionary<string, object?>());
            }
            catch (Exception ex)
            {
                // Error inesperado (conexión, timeout, etc.)
                return (false, $"Error inesperado: {ex.Message}", new Dictionary<string, object?>());
            }
        }

        /// <summary>
        /// Ejecuta un Stored Procedure que retorna MÚLTIPLES result sets.
        /// </summary>
        /// <typeparam name="T">Tipo base para los result sets</typeparam>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP</param>
        /// <param name="mappers">Array de funciones mapper (uno por cada result set esperado)</param>
        /// <returns>Lista de listas, donde cada lista interna representa un result set</returns>
        /// <remarks>
        /// USO TÍPICO: SP que retorna datos relacionados en múltiples tablas
        /// EJEMPLO: sp_GetUserProfile retorna 3 result sets:
        /// 1. Datos del usuario
        /// 2. Sus equipos
        /// 3. Sus transacciones
        /// 
        /// var results = await _dbHelper.ExecuteStoredProcedureMultipleResultSetsAsync(
        ///     "sp_GetUserProfile",
        ///     new[] { new SqlParameter("@UserID", userId) },
        ///     new Func<SqlDataReader, object>[] {
        ///         reader => MapUser(reader),      // Result set 1
        ///         reader => MapTeam(reader),      // Result set 2
        ///         reader => MapTransaction(reader) // Result set 3
        ///     }
        /// );
        /// 
        /// var users = results[0].Cast<User>().ToList();
        /// var teams = results[1].Cast<Team>().ToList();
        /// var transactions = results[2].Cast<Transaction>().ToList();
        /// </remarks>
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

                // Aplicar el mapper correspondiente si existe
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
            while (await reader.NextResultAsync()); // Avanzar al siguiente result set

            return allResults;
        }

        /// <summary>
        /// Ejecuta un Stored Procedure que NO retorna result sets.
        /// </summary>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP</param>
        /// <returns>Número de filas afectadas</returns>
        /// <remarks>
        /// USO TÍPICO: INSERT, UPDATE, DELETE sin necesidad de SELECT
        /// EJEMPLO:
        /// var affectedRows = await _dbHelper.ExecuteStoredProcedureNonQueryAsync(
        ///     "sp_DeleteUser",
        ///     new[] { new SqlParameter("@UserID", userId) }
        /// );
        /// 
        /// if (affectedRows > 0) { ... }
        /// </remarks>
        public async Task<int> ExecuteStoredProcedureNonQueryAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            using var connection = new SqlConnection(_connectionString);
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

        #region View Query Methods

        /// <summary>
        /// Ejecuta una consulta SELECT contra una VIEW o tabla.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a mapear</typeparam>
        /// <param name="viewOrTableName">Nombre de la view o tabla (ej: "vw_ActiveUsers")</param>
        /// <param name="mapper">Función que convierte SqlDataReader a T</param>
        /// <param name="whereClause">Cláusula WHERE sin la palabra "WHERE" (ej: "UserID = 123")</param>
        /// <param name="orderBy">Cláusula ORDER BY sin las palabras "ORDER BY" (ej: "CreatedAt DESC")</param>
        /// <param name="top">Número máximo de registros a retornar</param>
        /// <returns>Lista de objetos T</returns>
        /// <remarks>
        /// ⚠️ SEGURIDAD: whereClause y orderBy NO están parametrizados.
        /// Solo usar con valores seguros/validados o constantes.
        /// Para filtros dinámicos del usuario, usar un SP con parámetros.
        /// 
        /// EJEMPLO:
        /// var users = await _dbHelper.ExecuteViewAsync(
        ///     "vw_ActiveUsers",
        ///     reader => new User { ... },
        ///     whereClause: "IsActive = 1 AND Country = 'CR'",
        ///     orderBy: "CreatedAt DESC",
        ///     top: 100
        /// );
        /// </remarks>
        public async Task<List<T>> ExecuteViewAsync<T>(
            string viewOrTableName,
            Func<SqlDataReader, T> mapper,
            string? whereClause = null,
            string? orderBy = null,
            int? top = null)
        {
            var results = new List<T>();

            // Construir query
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

        /// <summary>
        /// Ejecuta una query SQL personalizada (usar con EXTREMO cuidado).
        /// </summary>
        /// <typeparam name="T">Tipo del objeto a mapear</typeparam>
        /// <param name="sqlQuery">Query SQL completo</param>
        /// <param name="mapper">Función que convierte SqlDataReader a T</param>
        /// <param name="parameters">Parámetros de la query (SIEMPRE usar parámetros, nunca concatenar valores)</param>
        /// <returns>Lista de objetos T</returns>
        /// <remarks>
        /// ⚠️ USAR SOLO EN CASOS MUY ESPECÍFICOS donde Views y SPs no son suficientes.
        /// SIEMPRE usar parámetros para prevenir SQL Injection.
        /// 
        /// EJEMPLO CORRECTO:
        /// var users = await _dbHelper.ExecuteRawQueryAsync(
        ///     "SELECT * FROM Users WHERE Email = @Email AND IsActive = @IsActive",
        ///     reader => new User { ... },
        ///     new[] {
        ///         new SqlParameter("@Email", email),
        ///         new SqlParameter("@IsActive", true)
        ///     }
        /// );
        /// 
        /// ❌ EJEMPLO INCORRECTO (SQL Injection):
        /// var query = $"SELECT * FROM Users WHERE Email = '{email}'"; // ¡NUNCA HACER ESTO!
        /// </remarks>
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

        #region Helper Methods for Complex Scenarios

        /// <summary>
        /// Ejecuta un SP y retorna solo el mensaje de resultado.
        /// </summary>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP</param>
        /// <returns>Mensaje desde la columna "Message" o mensaje por defecto</returns>
        /// <remarks>
        /// USO TÍPICO: SPs de modificación que retornan un mensaje de confirmación
        /// EJEMPLO:
        /// var message = await _dbHelper.ExecuteStoredProcedureForMessageAsync(
        ///     "sp_UpdateUserProfile",
        ///     new[] { new SqlParameter("@UserID", userId) }
        /// );
        /// // Retorna: "Perfil actualizado exitosamente" o mensaje similar del SP
        /// </remarks>
        public async Task<string> ExecuteStoredProcedureForMessageAsync(
            string procedureName,
            SqlParameter[]? parameters)
        {
            var result = await ExecuteStoredProcedureAsync(
                procedureName,
                parameters,
                reader => reader.GetSafeString("Message")
            );

            return result ?? "Operación completada.";
        }

        /// <summary>
        /// Verifica si existe al menos un registro que cumpla la condición.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla o view</param>
        /// <param name="whereClause">Condición WHERE sin la palabra "WHERE"</param>
        /// <returns>True si existe al menos un registro, false en caso contrario</returns>
        /// <remarks>
        /// ⚠️ whereClause NO está parametrizado, solo usar con valores seguros.
        /// EJEMPLO:
        /// bool exists = await _dbHelper.ExistsAsync("Users", "Email = 'test@example.com'");
        /// </remarks>
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

        /// <summary>
        /// Ejecuta un SP y proporciona acceso directo al SqlDataReader.
        /// Útil para escenarios complejos con múltiples result sets de tipos diferentes.
        /// </summary>
        /// <param name="procedureName">Nombre del SP</param>
        /// <param name="parameters">Parámetros del SP</param>
        /// <param name="readerAction">Acción async que procesa el SqlDataReader</param>
        /// <remarks>
        /// USO TÍPICO: Cuando necesitas control total sobre el reader
        /// EJEMPLO:
        /// await _dbHelper.ExecuteStoredProcedureWithCustomReaderAsync(
        ///     "sp_GetComplexData",
        ///     null,
        ///     async reader => {
        ///         // Primer result set
        ///         var users = new List<User>();
        ///         while (await reader.ReadAsync())
        ///         {
        ///             users.Add(MapUser(reader));
        ///         }
        ///         
        ///         // Segundo result set
        ///         await reader.NextResultAsync();
        ///         var teams = new List<Team>();
        ///         while (await reader.ReadAsync())
        ///         {
        ///             teams.Add(MapTeam(reader));
        ///         }
        ///         
        ///         // Procesar datos...
        ///     }
        /// );
        /// </remarks>
        public async Task ExecuteStoredProcedureWithCustomReaderAsync(
            string procedureName,
            SqlParameter[]? parameters,
            Func<SqlDataReader, Task> readerAction)
        {
            using var connection = new SqlConnection(_connectionString);
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