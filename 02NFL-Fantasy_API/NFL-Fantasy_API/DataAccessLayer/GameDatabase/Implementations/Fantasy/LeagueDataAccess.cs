using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.Fantasy
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de ligas.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/Views.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class LeagueDataAccess
    {
        private readonly IDatabaseHelper _db;

        public LeagueDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Create League

        /// <summary>
        /// Crea una nueva liga de fantasy.
        /// SP: app.sp_CreateLeague
        /// </summary>
        public async Task<CreateLeagueResponseDTO?> CreateLeagueAsync(
            CreateLeagueDTO dto,
            int creatorUserId,
            string? sourceIp,
            string? userAgent)
        {

            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@CreatorUserID", creatorUserId),
                SqlParameterExtensions.CreateParameter("@Name", dto.Name),
                SqlParameterExtensions.CreateParameter("@Description", dto.Description),
                SqlParameterExtensions.CreateParameter("@TeamSlots", dto.TeamSlots),
                SqlParameterExtensions.CreateParameter("@LeaguePassword", dto.LeaguePassword),
                SqlParameterExtensions.CreateParameter("@InitialTeamName", dto.InitialTeamName),
                SqlParameterExtensions.CreateParameter("@PlayoffTeams", dto.PlayoffTeams),
                SqlParameterExtensions.CreateParameter("@AllowDecimals", dto.AllowDecimals),
                SqlParameterExtensions.CreateParameter("@PositionFormatID", dto.PositionFormatID),
                SqlParameterExtensions.CreateParameter("@ScoringSchemaID", dto.ScoringSchemaID),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_CreateLeague",
                parameters,
                reader => new CreateLeagueResponseDTO
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    Name = reader.GetSafeString("Name"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    Status = reader.GetSafeByte("Status"),
                    PlayoffTeams = reader.GetSafeByte("PlayoffTeams"),
                    AllowDecimals = reader.GetSafeBool("AllowDecimals"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    Message = "Liga creada exitosamente."
                }
            );
        }

        #endregion

        #region Edit League Config

        /// <summary>
        /// Edita la configuración de una liga.
        /// SP: app.sp_EditLeagueConfig
        /// </summary>
        public async Task<string> EditLeagueConfigAsync(
            int leagueId,
            EditLeagueConfigDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId),
                SqlParameterExtensions.CreateParameter("@Name", dto.Name),
                SqlParameterExtensions.CreateParameter("@Description", dto.Description),
                SqlParameterExtensions.CreateParameter("@TeamSlots", dto.TeamSlots),
                SqlParameterExtensions.CreateParameter("@PositionFormatID", dto.PositionFormatID),
                SqlParameterExtensions.CreateParameter("@ScoringSchemaID", dto.ScoringSchemaID),
                SqlParameterExtensions.CreateParameter("@PlayoffTeams", dto.PlayoffTeams),
                SqlParameterExtensions.CreateParameter("@AllowDecimals", dto.AllowDecimals),
                SqlParameterExtensions.CreateParameter("@TradeDeadlineEnabled", dto.TradeDeadlineEnabled),
                SqlParameterExtensions.CreateParameter("@TradeDeadlineDate", dto.TradeDeadlineDate),
                SqlParameterExtensions.CreateParameter("@MaxRosterChangesPerTeam", dto.MaxRosterChangesPerTeam),
                SqlParameterExtensions.CreateParameter("@MaxFreeAgentAddsPerTeam", dto.MaxFreeAgentAddsPerTeam),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_EditLeagueConfig",
                parameters
            );
        }

        #endregion

        #region Set League Status

        /// <summary>
        /// Cambia el estado de una liga.
        /// SP: app.sp_SetLeagueStatus
        /// </summary>
        public async Task SetLeagueStatusAsync(
            int leagueId,
            SetLeagueStatusDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId),
                SqlParameterExtensions.CreateParameter("@NewStatus", dto.NewStatus),
                SqlParameterExtensions.CreateParameter("@Reason", dto.Reason),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            await _db.ExecuteStoredProcedureNonQueryAsync(
                "app.sp_SetLeagueStatus",
                parameters
            );
        }

        #endregion

        #region Get League Summary

        /// <summary>
        /// Obtiene resumen completo de una liga.
        /// SP: app.sp_GetLeagueSummary (retorna 2 result sets)
        /// </summary>
        public async Task<LeagueSummaryDTO?> GetLeagueSummaryAsync(int leagueId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId)
            };

            LeagueSummaryDTO? summary = null;

            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetLeagueSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Result Set 1: Datos de la liga
            if (await reader.ReadAsync())
            {
                summary = new LeagueSummaryDTO
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    Name = reader.GetSafeString("Name"),
                    Description = reader.GetSafeNullableString("Description"),
                    Status = reader.GetSafeByte("Status"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    PlayoffTeams = reader.GetSafeByte("PlayoffTeams"),
                    AllowDecimals = reader.GetSafeBool("AllowDecimals"),
                    TradeDeadlineEnabled = reader.GetSafeBool("TradeDeadlineEnabled"),
                    TradeDeadlineDate = reader.GetSafeNullableDateTime("TradeDeadlineDate"),
                    MaxRosterChangesPerTeam = reader.GetSafeNullableInt32("MaxRosterChangesPerTeam"),
                    MaxFreeAgentAddsPerTeam = reader.GetSafeNullableInt32("MaxFreeAgentAddsPerTeam"),
                    PositionFormatID = reader.GetSafeInt32("PositionFormatID"),
                    PositionFormatName = reader.GetSafeString("PositionFormatName"),
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    ScoringSchemaName = reader.GetSafeString("ScoringSchemaName"),
                    ScoringVersion = reader.GetSafeInt32("Version"),
                    SeasonID = reader.GetSafeInt32("SeasonID"),
                    SeasonLabel = reader.GetSafeString("SeasonLabel"),
                    Year = reader.GetSafeInt32("Year"),
                    StartDate = reader.GetSafeDateTime("StartDate"),
                    EndDate = reader.GetSafeDateTime("EndDate"),
                    CreatedByUserID = reader.GetSafeInt32("CreatedByUserID"),
                    CreatedByName = reader.GetSafeString("CreatedByName"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt")
                };
            }

            // Result Set 2: Equipos de la liga
            if (summary != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    summary.Teams.Add(new LeagueTeamDTO
                    {
                        TeamID = reader.GetSafeInt32("TeamID"),
                        TeamName = reader.GetSafeString("TeamName"),
                        OwnerUserID = reader.GetSafeInt32("OwnerUserID"),
                        OwnerName = reader.GetSafeString("OwnerName"),
                        CreatedAt = reader.GetSafeDateTime("CreatedAt")
                    });
                }
            }

            return summary;
        }

        #endregion

        #region Search and Join

        /// <summary>
        /// Busca ligas disponibles para unirse.
        /// SP: app.sp_SearchLeagues
        /// </summary>
        public async Task<List<SearchLeaguesResultDTO>> SearchLeaguesAsync(SearchLeaguesRequestDTO request)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@SearchTerm", request.SearchTerm),
                SqlParameterExtensions.CreateParameter("@SeasonID", request.SeasonID),
                SqlParameterExtensions.CreateParameter("@MinSlots", request.MinSlots),
                SqlParameterExtensions.CreateParameter("@MaxSlots", request.MaxSlots),
                SqlParameterExtensions.CreateParameter("@PageNumber", request.PageNumber),
                SqlParameterExtensions.CreateParameter("@PageSize", request.PageSize)
            };

            return await _db.ExecuteStoredProcedureListAsync(
                "app.sp_SearchLeagues",
                parameters,
                reader => new SearchLeaguesResultDTO
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    Name = reader.GetSafeString("Name"),
                    Description = reader.GetSafeString("Description"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    PlayoffTeams = reader.GetSafeByte("PlayoffTeams"),
                    AllowDecimals = reader.GetSafeBool("AllowDecimals"),
                    SeasonLabel = reader.GetSafeString("SeasonLabel"),
                    SeasonYear = reader.GetSafeInt32("SeasonYear"),
                    CreatedByName = reader.GetSafeString("CreatedByName"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    TotalRecords = reader.GetSafeInt32("TotalRecords"),
                    CurrentPage = reader.GetSafeInt32("CurrentPage"),
                    PageSize = reader.GetSafeInt32("PageSize"),
                    TotalPages = reader.GetSafeInt32("TotalPages")
                }
            );
        }

        /// <summary>
        /// Une a un usuario a una liga existente.
        /// SP: app.sp_JoinLeague
        /// </summary>
        public async Task<JoinLeagueResultDTO?> JoinLeagueAsync(
            int userId,
            JoinLeagueRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@UserID", userId),
                SqlParameterExtensions.CreateParameter("@LeagueID", request.LeagueID),
                SqlParameterExtensions.CreateParameter("@LeaguePassword", request.LeaguePassword),
                SqlParameterExtensions.CreateParameter("@TeamName", request.TeamName),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_JoinLeague",
                parameters,
                reader => new JoinLeagueResultDTO
                {
                    TeamID = reader.GetSafeInt32("TeamID"),
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    LeagueName = reader.GetSafeString("LeagueName"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        /// <summary>
        /// Valida si una contraseña de liga es correcta.
        /// SP: app.sp_ValidateLeaguePassword
        /// </summary>
        public async Task<ValidateLeaguePasswordResultDTO?> ValidateLeaguePasswordAsync(
            ValidateLeaguePasswordRequestDTO request)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@LeagueID", request.LeagueID),
                SqlParameterExtensions.CreateParameter("@LeaguePassword", request.LeaguePassword)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_ValidateLeaguePassword",
                parameters,
                reader => new ValidateLeaguePasswordResultDTO
                {
                    IsValid = reader.GetSafeBool("IsValid"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Remueve un equipo de la liga.
        /// SP: app.sp_RemoveTeamFromLeague
        /// </summary>
        public async Task<RemoveTeamResultDTO?> RemoveTeamFromLeagueAsync(
            int actorUserId,
            int leagueId,
            RemoveTeamRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId),
                SqlParameterExtensions.CreateParameter("@TeamID", request.TeamID),
                SqlParameterExtensions.CreateParameter("@Reason", request.Reason),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_RemoveTeamFromLeague",
                parameters,
                reader => new RemoveTeamResultDTO
                {
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        /// <summary>
        /// Permite a un usuario salir voluntariamente de una liga.
        /// SP: app.sp_LeaveLeague
        /// </summary>
        public async Task<string> LeaveLeagueAsync(
            int userId,
            int leagueId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@UserID", userId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_LeaveLeague",
                parameters
            );
        }

        /// <summary>
        /// Transfiere el rol de comisionado principal a otro miembro.
        /// SP: app.sp_TransferCommissioner
        /// </summary>
        public async Task<TransferCommissionerResultDTO?> TransferCommissionerAsync(
            int actorUserId,
            int leagueId,
            TransferCommissionerRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId),
                SqlParameterExtensions.CreateParameter("@NewCommissionerID", request.NewCommissionerID),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_TransferCommissioner",
                parameters,
                reader => new TransferCommissionerResultDTO
                {
                    Message = reader.GetSafeString("Message"),
                    NewCommissionerID = reader.GetSafeInt32("NewCommissionerID"),
                    NewCommissionerName = reader.GetSafeString("NewCommissionerName")
                }
            );
        }

        #endregion

        #region User Roles

        /// <summary>
        /// Obtiene todos los roles efectivos de un usuario en una liga.
        /// SP: app.sp_GetUserRolesInLeague
        /// </summary>
        public async Task<GetUserRolesInLeagueResponseDTO?> GetUserRolesInLeagueAsync(
            int userId,
            int leagueId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@UserID", userId),
                SqlParameterExtensions.CreateParameter("@LeagueID", leagueId)
            };

            GetUserRolesInLeagueResponseDTO? result = null;

            await _db.ExecuteStoredProcedureWithCustomReaderAsync(
                "app.sp_GetUserRolesInLeague",
                parameters,
                async (reader) =>
                {
                    result = new GetUserRolesInLeagueResponseDTO();

                    // Primer result set: roles individuales
                    while (await reader.ReadAsync())
                    {
                        result.Roles.Add(new UserLeagueRoleDTO
                        {
                            RoleName = reader.GetSafeString("RoleName"),
                            IsExplicit = reader.GetSafeBool("IsExplicit"),
                            IsDerived = reader.GetSafeBool("IsDerived"),
                            JoinedAt = reader.GetSafeDateTime("JoinedAt")
                        });
                    }

                    // Segundo result set: resumen
                    if (await reader.NextResultAsync() && await reader.ReadAsync())
                    {
                        result.Summary = new UserLeagueRoleSummaryDTO
                        {
                            PrimaryRole = reader.GetSafeString("PrimaryRole"),
                            AllRoles = reader.GetSafeString("AllRoles")
                        };
                    }
                }
            );

            return result;
        }

        #endregion

        #region View Queries

        /// <summary>
        /// Obtiene el directorio de ligas desde VIEW.
        /// VIEW: vw_LeagueDirectory
        /// </summary>
        public async Task<List<LeagueDirectoryVM>> GetLeagueDirectoryAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_LeagueDirectory",
                reader => new LeagueDirectoryVM
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    SeasonLabel = reader.GetSafeString("SeasonLabel"),
                    Name = reader.GetSafeString("Name"),
                    Status = reader.GetSafeByte("Status"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    CreatedByUserID = reader.GetSafeInt32("CreatedByUserID"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                orderBy: "CreatedAt DESC"
            );
        }

        /// <summary>
        /// Obtiene miembros de una liga desde VIEW.
        /// VIEW: vw_LeagueMembers
        /// </summary>
        public async Task<List<LeagueMemberVM>> GetLeagueMembersAsync(int leagueId)
        {
            return await _db.ExecuteViewAsync(
                "vw_LeagueMembers",
                reader => new LeagueMemberVM
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    UserID = reader.GetSafeInt32("UserID"),
                    LeagueRoleCode = reader.GetSafeString("LeagueRoleCode"),
                    UserAlias = reader.GetSafeNullableString("UserAlias"),
                    SystemRoleDisplay = reader.GetSafeString("SystemRoleDisplay"),
                    ProfileImageUrl = reader.GetSafeNullableString("ProfileImageUrl"),
                    JoinedAt = reader.GetSafeDateTime("JoinedAt"),
                    LeftAt = reader.GetSafeNullableDateTime("LeftAt"),
                    UserName = reader.GetSafeString("UserName"),
                    UserEmail = reader.GetSafeString("UserEmail")
                },
                whereClause: $"LeagueID = {leagueId}",
                orderBy: "JoinedAt"
            );
        }

        /// <summary>
        /// Obtiene equipos de una liga desde VIEW.
        /// VIEW: vw_LeagueTeams
        /// </summary>
        public async Task<List<LeagueTeamVM>> GetLeagueTeamsAsync(int leagueId)
        {
            return await _db.ExecuteViewAsync(
                "vw_LeagueTeams",
                reader => new LeagueTeamVM
                {
                    TeamID = reader.GetSafeInt32("TeamID"),
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    OwnerUserID = reader.GetSafeInt32("OwnerUserID"),
                    OwnerName = reader.GetSafeString("OwnerName"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    RosterCount = reader.GetSafeInt32("RosterCount"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    OwnerProfileImage = reader.GetSafeNullableString("OwnerProfileImage"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                whereClause: $"LeagueID = {leagueId}",
                orderBy: "CreatedAt"
            );
        }

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (versión ligera).
        /// VIEW: vw_LeagueSummary
        /// </summary>
        public async Task<LeagueSummaryVM?> GetLeagueSummaryViewAsync(int leagueId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_LeagueSummary",
                reader => new LeagueSummaryVM
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    Name = reader.GetSafeString("Name"),
                    Description = reader.GetSafeNullableString("Description"),
                    Status = reader.GetSafeByte("Status"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    PlayoffTeams = reader.GetSafeByte("PlayoffTeams"),
                    AllowDecimals = reader.GetSafeBool("AllowDecimals"),
                    TradeDeadlineEnabled = reader.GetSafeBool("TradeDeadlineEnabled"),
                    TradeDeadlineDate = reader.GetSafeNullableDateTime("TradeDeadlineDate"),
                    MaxRosterChangesPerTeam = reader.GetSafeNullableInt32("MaxRosterChangesPerTeam"),
                    MaxFreeAgentAddsPerTeam = reader.GetSafeNullableInt32("MaxFreeAgentAddsPerTeam"),
                    PositionFormatID = reader.GetSafeInt32("PositionFormatID"),
                    PositionFormatName = reader.GetSafeString("PositionFormatName"),
                    ScoringSchemaID = reader.GetSafeInt32("ScoringSchemaID"),
                    ScoringSchemaName = reader.GetSafeString("ScoringSchemaName"),
                    ScoringVersion = reader.GetSafeInt32("ScoringVersion"),
                    SeasonID = reader.GetSafeInt32("SeasonID"),
                    SeasonLabel = reader.GetSafeString("SeasonLabel"),
                    Year = reader.GetSafeInt32("Year"),
                    StartDate = reader.GetSafeDateTime("StartDate"),
                    EndDate = reader.GetSafeDateTime("EndDate"),
                    CreatedByUserID = reader.GetSafeInt32("CreatedByUserID"),
                    CreatedByName = reader.GetSafeString("CreatedByName"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt")
                },
                whereClause: $"LeagueID = {leagueId}"
            );

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Obtiene ligas donde el usuario es comisionado desde VIEW.
        /// VIEW: vw_UserCommissionedLeagues
        /// </summary>
        public async Task<List<UserCommissionedLeagueVM>> GetUserCommissionedLeaguesAsync(int userId)
        {
            return await _db.ExecuteViewAsync(
                "vw_UserCommissionedLeagues",
                reader => new UserCommissionedLeagueVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeagueName = reader.GetSafeString("LeagueName"),
                    Status = reader.GetSafeByte("Status"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    RoleCode = reader.GetSafeString("RoleCode"),
                    JoinedAt = reader.GetSafeDateTime("JoinedAt"),
                    LeagueCreatedAt = reader.GetSafeDateTime("LeagueCreatedAt")
                },
                whereClause: $"UserID = {userId}",
                orderBy: "LeagueCreatedAt DESC"
            );
        }

        /// <summary>
        /// Obtiene equipos del usuario en todas sus ligas desde VIEW.
        /// VIEW: vw_UserTeams
        /// </summary>
        public async Task<List<UserTeamVM>> GetUserTeamsAsync(int userId)
        {
            return await _db.ExecuteViewAsync(
                "vw_UserTeams",
                reader => new UserTeamVM
                {
                    UserID = reader.GetSafeInt32("UserID"),
                    TeamID = reader.GetSafeInt32("TeamID"),
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeagueName = reader.GetSafeString("LeagueName"),
                    TeamName = reader.GetSafeString("TeamName"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    RosterCount = reader.GetSafeInt32("RosterCount"),
                    TeamCreatedAt = reader.GetSafeDateTime("TeamCreatedAt"),
                    LeagueStatus = reader.GetSafeByte("LeagueStatus")
                },
                whereClause: $"UserID = {userId}",
                orderBy: "TeamCreatedAt DESC"
            );
        }

        #endregion

        #region League Views

        /// <summary>
        /// Obtiene todas las ligas del sistema.
        /// VIEW: vw_LeagueDirectory
        /// </summary>
        public async Task<List<LeagueDirectoryVM>> GetAllLeaguesAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_LeagueDirectory",
                reader => new LeagueDirectoryVM
                {
                    LeagueID = reader.GetSafeInt32("LeagueID"),
                    LeaguePublicID = reader.GetSafeInt32("LeaguePublicID"),
                    SeasonLabel = reader.GetSafeString("SeasonLabel"),
                    Name = reader.GetSafeString("Name"),
                    Status = reader.GetSafeByte("Status"),
                    TeamSlots = reader.GetSafeByte("TeamSlots"),
                    TeamsCount = reader.GetSafeInt32("TeamsCount"),
                    AvailableSlots = reader.GetSafeInt32("AvailableSlots"),
                    CreatedByUserID = reader.GetSafeInt32("CreatedByUserID"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt")
                },
                orderBy: "CreatedAt DESC"
            );
        }

        #endregion
    }
}