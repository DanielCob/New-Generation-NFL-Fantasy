using Microsoft.Data.SqlClient;
using System.Data;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions
{
    /// <summary>
    /// Extensiones para simplificar la creación de SqlParameters.
    /// </summary>
    public static class SqlParameterExtensions
    {
        /// <summary>
        /// Crea un SqlParameter con manejo automático de NULL.
        /// </summary>
        public static SqlParameter CreateParameter(string name, object? value)
        {
            return new SqlParameter(name, value.ToDbNull());
        }

        /// <summary>
        /// Crea un SqlParameter OUTPUT.
        /// </summary>
        public static SqlParameter CreateOutputParameter(string name, SqlDbType type, int? size = null)
        {
            var param = new SqlParameter(name, type) { Direction = ParameterDirection.Output };
            if (size.HasValue)
                param.Size = size.Value;
            return param;
        }
    }
}