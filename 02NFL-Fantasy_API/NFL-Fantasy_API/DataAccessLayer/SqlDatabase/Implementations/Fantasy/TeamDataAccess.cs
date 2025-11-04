using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de equipos fantasy.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/Views.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class TeamDataAccess
    {
        private readonly IDatabaseHelper _db;

        public TeamDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Update Team Branding

        /// <summary>
        /// Actualiza el branding de un equipo.
        /// SP: app.sp_UpdateTeamBranding
        /// </summary>
        public async Task<string> UpdateTeamBrandingAsync(
            int teamId,
            UpdateTeamBrandingDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TeamID", teamId),
                SqlParameterExtensions.CreateParameter("@TeamName", dto.TeamName),
                SqlParameterExtensions.CreateParameter("@TeamImageUrl", dto.TeamImageUrl),
                SqlParameterExtensions.CreateParameter("@TeamImageWidth", dto.TeamImageWidth),
                SqlParameterExtensions.CreateParameter("@TeamImageHeight", dto.TeamImageHeight),
                SqlParameterExtensions.CreateParameter("@TeamImageBytes", dto.TeamImageBytes),
                SqlParameterExtensions.CreateParameter("@ThumbnailUrl", dto.ThumbnailUrl),
                SqlParameterExtensions.CreateParameter("@ThumbnailWidth", dto.ThumbnailWidth),
                SqlParameterExtensions.CreateParameter("@ThumbnailHeight", dto.ThumbnailHeight),
                SqlParameterExtensions.CreateParameter("@ThumbnailBytes", dto.ThumbnailBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_UpdateTeamBranding",
                parameters
            );
        }

        #endregion

        #region Get My Team

        /// <summary>
        /// Obtiene información completa del equipo con roster.
        /// SP: app.sp_GetMyTeam (retorna 3 result sets)
        /// RS1: Información del equipo
        /// RS2: Jugadores en roster
        /// RS3: Distribución de adquisición
        /// </summary>
        public async Task<MyTeamResponseDTO?> GetMyTeamAsync(
            int teamId,
            int actorUserId,
            string? filterPosition,
            string? searchPlayer)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TeamID", teamId),
                SqlParameterExtensions.CreateParameter("@FilterPosition", filterPosition),
                SqlParameterExtensions.CreateParameter("@SearchPlayer", searchPlayer)
            };

            MyTeamResponseDTO? myTeam = null;

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetMyTeam", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // RS1: Información del equipo
            if (await reader.ReadAsync())
            {
                myTeam = new MyTeamResponseDTO
                {
                    TeamID = reader.GetSafeInt32("TeamID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    ManagerName = reader.GetSafeString("ManagerName"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    LeagueName = reader.GetSafeString("LeagueName"),
                    LeagueStatus = reader.GetSafeByte("LeagueStatus"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt")
                };
            }

            // RS2: Roster de jugadores
            if (myTeam != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    myTeam.Roster.Add(new RosterPlayerDTO
                    {
                        RosterID = reader.GetSafeInt64("RosterID"),
                        PlayerID = reader.GetSafeInt32("PlayerID"),
                        FirstName = reader.GetSafeString("FirstName"),
                        LastName = reader.GetSafeString("LastName"),
                        FullName = reader.GetSafeString("FullName"),
                        Position = reader.GetSafeString("Position"),
                        NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                        InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                        PhotoUrl = reader.GetSafeNullableString("PhotoUrl"),
                        AcquisitionType = reader.GetSafeString("AcquisitionType"),
                        AcquisitionDate = reader.GetSafeDateTime("AcquisitionDate"),
                        IsOnRoster = reader.GetSafeBool("IsOnRoster")
                    });
                }
            }

            // RS3: Distribución
            if (myTeam != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    myTeam.Distribution.Add(new RosterDistributionItemDTO
                    {
                        AcquisitionType = reader.GetSafeString("AcquisitionType"),
                        PlayerCount = reader.GetSafeInt32("PlayerCount"),
                        TotalPlayers = reader.GetSafeInt32("TotalPlayers"),
                        Percentage = reader.GetSafeDecimal("Percentage")
                    });
                }
            }

            return myTeam;
        }

        #endregion

        #region Get Team Roster Distribution

        /// <summary>
        /// Obtiene distribución porcentual del roster.
        /// SP: app.sp_GetTeamRosterDistribution
        /// </summary>
        public async Task<List<RosterDistributionItemDTO>> GetTeamRosterDistributionAsync(int teamId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@TeamID", teamId)
            };

            return await _db.ExecuteStoredProcedureListAsync(
                "app.sp_GetTeamRosterDistribution",
                parameters,
                reader => new RosterDistributionItemDTO
                {
                    AcquisitionType = reader.GetSafeString("AcquisitionType"),
                    PlayerCount = reader.GetSafeInt32("PlayerCount"),
                    TotalPlayers = reader.GetSafeInt32("TotalPlayers"),
                    Percentage = reader.GetSafeDecimal("Percentage")
                }
            );
        }

        #endregion

        #region Add / Remove Player from Roster

        /// <summary>
        /// Agrega un jugador al roster.
        /// SP: app.sp_AddPlayerToRoster
        /// </summary>
        public async Task<AddPlayerToRosterResponseDTO?> AddPlayerToRosterAsync(
            int teamId,
            AddPlayerToRosterDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TeamID", teamId),
                SqlParameterExtensions.CreateParameter("@PlayerID", dto.PlayerID),
                SqlParameterExtensions.CreateParameter("@AcquisitionType", dto.AcquisitionType),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_AddPlayerToRoster",
                parameters,
                reader => new AddPlayerToRosterResponseDTO
                {
                    RosterID = reader.GetSafeInt64("RosterID"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        /// <summary>
        /// Remueve un jugador del roster.
        /// SP: app.sp_RemovePlayerFromRoster
        /// </summary>
        public async Task<string> RemovePlayerFromRosterAsync(
            int rosterId,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@RosterID", rosterId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_RemovePlayerFromRoster",
                parameters
            );
        }

        #endregion

        #region Get Fantasy Team Details from VIEW

        /// <summary>
        /// Obtiene detalles de un equipo fantasy desde VIEW.
        /// VIEW: vw_FantasyTeamDetails
        /// </summary>
        public async Task<FantasyTeamDetailsVM?> GetFantasyTeamDetailsAsync(int teamId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_FantasyTeamDetails",
                reader => new FantasyTeamDetailsVM
                {
                    TeamID = reader.GetSafeInt32("TeamID"),
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeagueName = reader.GetSafeString("LeagueName"),
                    LeagueStatus = reader.GetSafeByte("LeagueStatus"),
                    OwnerUserID = reader.GetSafeInt32("OwnerUserID"),
                    ManagerName = reader.GetSafeString("ManagerName"),
                    ManagerEmail = reader.GetSafeString("ManagerEmail"),
                    ManagerAlias = reader.GetSafeNullableString("ManagerAlias"),
                    TeamName = reader.GetSafeString("TeamName"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    RosterCount = reader.GetSafeInt32("RosterCount"),
                    DraftedCount = reader.GetSafeInt32("DraftedCount"),
                    TradedCount = reader.GetSafeInt32("TradedCount"),
                    FreeAgentCount = reader.GetSafeInt32("FreeAgentCount"),
                    ManagerProfileImage = reader.GetSafeNullableString("ManagerProfileImage"),
                    WaiverCount = reader.GetSafeInt32("WaiverCount")
                },
                whereClause: $"TeamID = {teamId}"
            );

            return results.FirstOrDefault();
        }

        #endregion
    }
}