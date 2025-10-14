using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de datos de referencia
    /// Maneja temporadas, formatos de posiciones y datos del sistema
    /// </summary>
    public class ReferenceService : IReferenceService
    {
        private readonly DatabaseHelper _db;

        public ReferenceService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Current Season

        /// <summary>
        /// Obtiene la temporada actual (IsCurrent=1)
        /// VIEW: vw_CurrentSeason
        /// Feature 1.2 - Crear liga (necesita temporada actual)
        /// </summary>
        public async Task<CurrentSeasonVM?> GetCurrentSeasonAsync()
        {
            try
            {
                var results = await _db.ExecuteViewAsync<CurrentSeasonVM>(
                    "vw_CurrentSeason",
                    reader => new CurrentSeasonVM
                    {
                        SeasonID = DatabaseHelper.GetSafeInt32(reader, "SeasonID"),
                        Label = DatabaseHelper.GetSafeString(reader, "Label"),
                        Year = DatabaseHelper.GetSafeInt32(reader, "Year"),
                        StartDate = DatabaseHelper.GetSafeDateTime(reader, "StartDate"),
                        EndDate = DatabaseHelper.GetSafeDateTime(reader, "EndDate"),
                        IsCurrent = DatabaseHelper.GetSafeBool(reader, "IsCurrent"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    }
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Position Formats

        /// <summary>
        /// Lista todos los formatos de posiciones
        /// VIEW: vw_PositionFormats
        /// </summary>
        public async Task<List<PositionFormatVM>> ListPositionFormatsAsync()
        {
            try
            {
                return await _db.ExecuteViewAsync<PositionFormatVM>(
                    "vw_PositionFormats",
                    reader => new PositionFormatVM
                    {
                        PositionFormatID = DatabaseHelper.GetSafeInt32(reader, "PositionFormatID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    orderBy: "PositionFormatID"
                );
            }
            catch
            {
                return new List<PositionFormatVM>();
            }
        }

        /// <summary>
        /// Obtiene los slots de un formato específico
        /// VIEW: vw_PositionFormatSlots
        /// </summary>
        public async Task<List<PositionFormatSlotVM>> GetPositionFormatSlotsAsync(int positionFormatId)
        {
            try
            {
                return await _db.ExecuteViewAsync<PositionFormatSlotVM>(
                    "vw_PositionFormatSlots",
                    reader => new PositionFormatSlotVM
                    {
                        PositionFormatID = DatabaseHelper.GetSafeInt32(reader, "PositionFormatID"),
                        FormatName = DatabaseHelper.GetSafeString(reader, "FormatName"),
                        PositionCode = DatabaseHelper.GetSafeString(reader, "PositionCode"),
                        SlotCount = DatabaseHelper.GetSafeByte(reader, "SlotCount")
                    },
                    whereClause: $"PositionFormatID = {positionFormatId}"
                );
            }
            catch
            {
                return new List<PositionFormatSlotVM>();
            }
        }

        /// <summary>
        /// Obtiene un formato específico por ID
        /// VIEW: vw_PositionFormats con WHERE
        /// </summary>
        public async Task<PositionFormatVM?> GetPositionFormatByIdAsync(int positionFormatId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<PositionFormatVM>(
                    "vw_PositionFormats",
                    reader => new PositionFormatVM
                    {
                        PositionFormatID = DatabaseHelper.GetSafeInt32(reader, "PositionFormatID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    whereClause: $"PositionFormatID = {positionFormatId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}