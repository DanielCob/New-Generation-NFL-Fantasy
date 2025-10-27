using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;
using System.Data;

namespace NFL_Fantasy_API.Services.Implementations
{
    public class SeasonService : ISeasonService
    {
        private readonly DatabaseHelper _db;

        public SeasonService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        public async Task<SeasonVM?> GetCurrentSeasonAsync()
        {
            var list = await _db.ExecuteViewAsync("vw_CurrentSeason", MapSeason, top: 1);
            return list.FirstOrDefault();
        }

        #region Create
        public async Task<SeasonVM?> CreateSeasonAsync(CreateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent)
        {
            var ps = new[]
            {
                new SqlParameter("@Name", SqlDbType.NVarChar, 100){ Value = dto.Name },
                new SqlParameter("@WeekCount", SqlDbType.TinyInt){ Value = dto.WeekCount },
                new SqlParameter("@StartDate", SqlDbType.Date){ Value = dto.StartDate.Date },
                new SqlParameter("@EndDate", SqlDbType.Date){ Value = dto.EndDate.Date },
                new SqlParameter("@MarkAsCurrent", SqlDbType.Bit){ Value = dto.MarkAsCurrent },
                new SqlParameter("@ActorUserID", SqlDbType.Int){ Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45){ Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300){ Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureAsync("app.sp_CreateSeason", ps, MapSeason);
        }
        #endregion

        #region Deactivate
        public async Task<string> DeactivateSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent)
        {
            var ps = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int){ Value = seasonId },
                new SqlParameter("@Confirm", SqlDbType.Bit){ Value = confirm },
                new SqlParameter("@ActorUserID", SqlDbType.Int){ Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45){ Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300){ Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureForMessageAsync("app.sp_DeactivateSeason", ps);
        }
        #endregion

        #region Delete
        public async Task<string> DeleteSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent)
        {
            var ps = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int){ Value = seasonId },
                new SqlParameter("@Confirm", SqlDbType.Bit){ Value = confirm },
                new SqlParameter("@ActorUserID", SqlDbType.Int){ Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45){ Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300){ Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureForMessageAsync("app.sp_DeleteSeason", ps);
        }
        #endregion

        #region Update
        public async Task<SeasonVM?> UpdateSeasonAsync(int seasonId, UpdateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent)
        {
            var ps = new List<SqlParameter>
            {
                new SqlParameter("@SeasonID", SqlDbType.Int){ Value = seasonId },
                new SqlParameter("@Name", SqlDbType.NVarChar, 100){ Value = dto.Name },
                new SqlParameter("@WeekCount", SqlDbType.TinyInt){ Value = dto.WeekCount },
                new SqlParameter("@StartDate", SqlDbType.Date){ Value = dto.StartDate.Date },
                new SqlParameter("@EndDate", SqlDbType.Date){ Value = dto.EndDate.Date },
                new SqlParameter("@SetAsCurrent", SqlDbType.Bit){ Value = (object?)dto.SetAsCurrent ?? DBNull.Value },
                new SqlParameter("@ConfirmMakeCurrent", SqlDbType.Bit){ Value = dto.ConfirmMakeCurrent },
                new SqlParameter("@ActorUserID", SqlDbType.Int){ Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45){ Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300){ Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureAsync("app.sp_UpdateSeason", ps.ToArray(), MapSeason);
        }
        #endregion

        #region Weeks + Get by ID
        public async Task<List<SeasonWeekVM>> GetSeasonWeeksAsync(int seasonId)
        {
            var sql = @"
                SELECT SeasonID, WeekNumber, StartDate, EndDate
                FROM league.SeasonWeek
                WHERE SeasonID = @SeasonID
                ORDER BY WeekNumber";

            var ps = new[] { new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId } };

            return await _db.ExecuteRawQueryAsync(sql, r => new SeasonWeekVM
            {
                SeasonID = DatabaseHelper.GetSafeInt32(r, "SeasonID"),
                WeekNumber = (byte)DatabaseHelper.GetSafeByte(r, "WeekNumber"),
                StartDate = DatabaseHelper.GetSafeDateTime(r, "StartDate"),
                EndDate = DatabaseHelper.GetSafeDateTime(r, "EndDate"),
            }, ps);
        }

        public async Task<SeasonVM?> GetSeasonByIdAsync(int seasonId)
        {
            var sql = @"
                SELECT SeasonID, Label, [Year], StartDate, EndDate, IsCurrent, CreatedAt
                FROM league.Season WHERE SeasonID = @SeasonID";

            var ps = new[] { new SqlParameter("@SeasonID", System.Data.SqlDbType.Int) { Value = seasonId } };

            var list = await _db.ExecuteRawQueryAsync(sql, MapSeason, ps);
            return list.FirstOrDefault();
        }
        #endregion

        #region Mapper
        private static SeasonVM MapSeason(SqlDataReader reader)
        {
            return new SeasonVM
            {
                SeasonID = DatabaseHelper.GetSafeInt32(reader, "SeasonID"),
                Label = DatabaseHelper.GetSafeString(reader, "Label"),
                Year = DatabaseHelper.GetSafeInt32(reader, "Year"),
                StartDate = DatabaseHelper.GetSafeDateTime(reader, "StartDate"),
                EndDate = DatabaseHelper.GetSafeDateTime(reader, "EndDate"),
                IsCurrent = DatabaseHelper.GetSafeBool(reader, "IsCurrent"),
                CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
            };
        }
        #endregion
    }
}
