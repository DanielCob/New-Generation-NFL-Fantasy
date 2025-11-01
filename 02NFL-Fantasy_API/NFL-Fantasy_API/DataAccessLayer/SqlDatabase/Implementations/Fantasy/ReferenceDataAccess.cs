using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.Models.ViewModels.NflDetails;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de datos de referencia.
    /// Responsabilidad: Ejecución de queries a VIEWs del sistema.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class ReferenceDataAccess
    {
        private readonly IDatabaseHelper _db;

        public ReferenceDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Position Formats

        /// <summary>
        /// Lista todos los formatos de posiciones.
        /// VIEW: vw_PositionFormats
        /// </summary>
        public async Task<List<PositionFormatVM>> ListPositionFormatsAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_PositionFormats",
                reader => new PositionFormatVM
                {
                    PositionFormatID = reader.GetSafeInt32("PositionFormatID"),
                    Name = reader.GetSafeString("Name"),
                    Description = reader.GetSafeNullableString("Description"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                orderBy: "PositionFormatID"
            );
        }

        /// <summary>
        /// Obtiene los slots de un formato específico.
        /// VIEW: vw_PositionFormatSlots
        /// </summary>
        public async Task<List<PositionFormatSlotVM>> GetPositionFormatSlotsAsync(int positionFormatId)
        {
            return await _db.ExecuteViewAsync(
                "vw_PositionFormatSlots",
                reader => new PositionFormatSlotVM
                {
                    PositionFormatID = reader.GetSafeInt32("PositionFormatID"),
                    FormatName = reader.GetSafeString("FormatName"),
                    PositionCode = reader.GetSafeString("PositionCode"),
                    SlotCount = reader.GetSafeByte("SlotCount")
                },
                whereClause: $"PositionFormatID = {positionFormatId}"
            );
        }

        /// <summary>
        /// Obtiene un formato específico por ID.
        /// VIEW: vw_PositionFormats con WHERE
        /// </summary>
        public async Task<PositionFormatVM?> GetPositionFormatByIdAsync(int positionFormatId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_PositionFormats",
                reader => new PositionFormatVM
                {
                    PositionFormatID = reader.GetSafeInt32("PositionFormatID"),
                    Name = reader.GetSafeString("Name"),
                    Description = reader.GetSafeNullableString("Description"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                whereClause: $"PositionFormatID = {positionFormatId}"
            );

            return results.FirstOrDefault();
        }

        #endregion
    }
}