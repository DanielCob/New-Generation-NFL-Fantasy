using Microsoft.Data.SqlClient;
using System.Data;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de temporadas NFL.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/queries.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class SeasonDataAccess
    {
        private readonly IDatabaseHelper _db;

        public SeasonDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Get Current Season

        /// <summary>
        /// Obtiene la temporada actual.
        /// VIEW: vw_CurrentSeason
        /// </summary>
        public async Task<SeasonVM?> GetCurrentSeasonAsync()
        {
            var list = await _db.ExecuteViewAsync(
                "vw_CurrentSeason",
                MapSeason,
                top: 1
            );

            return list.FirstOrDefault();
        }

        #endregion

        #region Create Season

        /// <summary>
        /// Crea una nueva temporada.
        /// SP: app.sp_CreateSeason
        /// </summary>
        public async Task<SeasonVM?> CreateSeasonAsync(
            CreateSeasonRequestDTO dto,
            int actorUserId,
            string? ip,
            string? userAgent)
        {
            var parameters = new[]
            {
                new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = dto.Name },
                new SqlParameter("@WeekCount", SqlDbType.TinyInt) { Value = dto.WeekCount },
                new SqlParameter("@StartDate", SqlDbType.Date) { Value = dto.StartDate.Date },
                new SqlParameter("@EndDate", SqlDbType.Date) { Value = dto.EndDate.Date },
                new SqlParameter("@MarkAsCurrent", SqlDbType.Bit) { Value = dto.MarkAsCurrent },
                new SqlParameter("@ActorUserID", SqlDbType.Int) { Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45) { Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300) { Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_CreateSeason",
                parameters,
                MapSeason
            );
        }

        #endregion

        #region Update Season

        /// <summary>
        /// Actualiza una temporada existente.
        /// SP: app.sp_UpdateSeason
        /// </summary>
        public async Task<SeasonVM?> UpdateSeasonAsync(
            int seasonId,
            UpdateSeasonRequestDTO dto,
            int actorUserId,
            string? ip,
            string? userAgent)
        {
            var parameters = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId },
                new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = dto.Name },
                new SqlParameter("@WeekCount", SqlDbType.TinyInt) { Value = dto.WeekCount },
                new SqlParameter("@StartDate", SqlDbType.Date) { Value = dto.StartDate.Date },
                new SqlParameter("@EndDate", SqlDbType.Date) { Value = dto.EndDate.Date },
                new SqlParameter("@SetAsCurrent", SqlDbType.Bit) { Value = (object?)dto.SetAsCurrent ?? DBNull.Value },
                new SqlParameter("@ConfirmMakeCurrent", SqlDbType.Bit) { Value = dto.ConfirmMakeCurrent },
                new SqlParameter("@ActorUserID", SqlDbType.Int) { Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45) { Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300) { Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_UpdateSeason",
                parameters,
                MapSeason
            );
        }

        #endregion

        #region Deactivate Season

        /// <summary>
        /// Desactiva una temporada.
        /// SP: app.sp_DeactivateSeason
        /// </summary>
        public async Task<string> DeactivateSeasonAsync(
            int seasonId,
            bool confirm,
            int actorUserId,
            string? ip,
            string? userAgent)
        {
            var parameters = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId },
                new SqlParameter("@Confirm", SqlDbType.Bit) { Value = confirm },
                new SqlParameter("@ActorUserID", SqlDbType.Int) { Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45) { Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300) { Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_DeactivateSeason",
                parameters
            );
        }

        #endregion

        #region Delete Season

        /// <summary>
        /// Elimina una temporada.
        /// SP: app.sp_DeleteSeason
        /// </summary>
        public async Task<string> DeleteSeasonAsync(
            int seasonId,
            bool confirm,
            int actorUserId,
            string? ip,
            string? userAgent)
        {
            var parameters = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId },
                new SqlParameter("@Confirm", SqlDbType.Bit) { Value = confirm },
                new SqlParameter("@ActorUserID", SqlDbType.Int) { Value = actorUserId },
                new SqlParameter("@SourceIp", SqlDbType.NVarChar, 45) { Value = (object?)ip ?? DBNull.Value },
                new SqlParameter("@UserAgent", SqlDbType.NVarChar, 300) { Value = (object?)userAgent ?? DBNull.Value }
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_DeleteSeason",
                parameters
            );
        }

        #endregion

        #region Get Season Weeks

        /// <summary>
        /// Obtiene las semanas de una temporada.
        /// Query directa a league.SeasonWeek
        /// </summary>
        public async Task<List<SeasonWeekVM>> GetSeasonWeeksAsync(int seasonId)
        {
            var sql = @"
                SELECT SeasonID, WeekNumber, StartDate, EndDate
                FROM league.SeasonWeek
                WHERE SeasonID = @SeasonID
                ORDER BY WeekNumber";

            var parameters = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId }
            };

            return await _db.ExecuteRawQueryAsync(
                sql,
                reader => new SeasonWeekVM
                {
                    SeasonID = reader.GetSafeInt32("SeasonID"),
                    WeekNumber = reader.GetSafeByte("WeekNumber"),
                    StartDate = reader.GetSafeDateTime("StartDate"),
                    EndDate = reader.GetSafeDateTime("EndDate")
                },
                parameters
            );
        }

        #endregion

        #region Get Season by ID

        /// <summary>
        /// Obtiene una temporada por ID.
        /// Query directa a league.Season
        /// </summary>
        public async Task<SeasonVM?> GetSeasonByIdAsync(int seasonId)
        {
            var sql = @"
                SELECT SeasonID, Label, [Year], StartDate, EndDate, IsCurrent, CreatedAt
                FROM league.Season
                WHERE SeasonID = @SeasonID";

            var parameters = new[]
            {
                new SqlParameter("@SeasonID", SqlDbType.Int) { Value = seasonId }
            };

            var list = await _db.ExecuteRawQueryAsync(sql, MapSeason, parameters);
            return list.FirstOrDefault();
        }

        #endregion

        #region Mapper

        /// <summary>
        /// Mapper para SeasonVM desde SqlDataReader.
        /// </summary>
        private static SeasonVM MapSeason(SqlDataReader reader)
        {
            return new SeasonVM
            {
                SeasonID = reader.GetSafeInt32("SeasonID"),
                Label = reader.GetSafeString("Label"),
                Year = reader.GetSafeInt32("Year"),
                StartDate = reader.GetSafeDateTime("StartDate"),
                EndDate = reader.GetSafeDateTime("EndDate"),
                IsCurrent = reader.GetSafeBool("IsCurrent"),
                CreatedAt = reader.GetSafeDateTime("CreatedAt")
            };
        }

        #endregion
    }
}