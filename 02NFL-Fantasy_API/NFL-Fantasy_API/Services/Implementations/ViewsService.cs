using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de vistas/reportes administrativos
    /// Consolida acceso a vistas complejas para reportería
    /// Endpoints bajo /api/views/* (ADMIN-only por middleware)
    /// </summary>
    public class ViewsService : IViewsService
    {
        private readonly DatabaseHelper _db;

        public ViewsService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region League Views

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (sin equipos)
        /// VIEW: vw_LeagueSummary
        /// Alternativa ligera a sp_GetLeagueSummary
        /// </summary>
        public async Task<LeagueSummaryVM?> GetLeagueSummaryViewAsync(int leagueId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<LeagueSummaryVM>(
                    "vw_LeagueSummary",
                    reader => new LeagueSummaryVM
                    {
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeNullableString(reader, "Description"),
                        Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                        TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                        TeamsCount = DatabaseHelper.GetSafeInt32(reader, "TeamsCount"),
                        AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                        PlayoffTeams = DatabaseHelper.GetSafeByte(reader, "PlayoffTeams"),
                        AllowDecimals = DatabaseHelper.GetSafeBool(reader, "AllowDecimals"),
                        TradeDeadlineEnabled = DatabaseHelper.GetSafeBool(reader, "TradeDeadlineEnabled"),
                        TradeDeadlineDate = DatabaseHelper.GetSafeNullableDateTime(reader, "TradeDeadlineDate"),
                        MaxRosterChangesPerTeam = DatabaseHelper.GetSafeNullableInt32(reader, "MaxRosterChangesPerTeam"),
                        MaxFreeAgentAddsPerTeam = DatabaseHelper.GetSafeNullableInt32(reader, "MaxFreeAgentAddsPerTeam"),
                        PositionFormatID = DatabaseHelper.GetSafeInt32(reader, "PositionFormatID"),
                        PositionFormatName = DatabaseHelper.GetSafeString(reader, "PositionFormatName"),
                        ScoringSchemaID = DatabaseHelper.GetSafeInt32(reader, "ScoringSchemaID"),
                        ScoringSchemaName = DatabaseHelper.GetSafeString(reader, "ScoringSchemaName"),
                        ScoringVersion = DatabaseHelper.GetSafeInt32(reader, "ScoringVersion"),
                        SeasonID = DatabaseHelper.GetSafeInt32(reader, "SeasonID"),
                        SeasonLabel = DatabaseHelper.GetSafeString(reader, "SeasonLabel"),
                        Year = DatabaseHelper.GetSafeInt32(reader, "Year"),
                        StartDate = DatabaseHelper.GetSafeDateTime(reader, "StartDate"),
                        EndDate = DatabaseHelper.GetSafeDateTime(reader, "EndDate"),
                        CreatedByUserID = DatabaseHelper.GetSafeInt32(reader, "CreatedByUserID"),
                        CreatedByName = DatabaseHelper.GetSafeString(reader, "CreatedByName"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                    },
                    whereClause: $"LeagueID = {leagueId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene todas las ligas del sistema
        /// VIEW: vw_LeagueDirectory
        /// Para dashboards administrativos
        /// </summary>
        public async Task<List<LeagueDirectoryVM>> GetAllLeaguesAsync()
        {
            try
            {
                return await _db.ExecuteViewAsync<LeagueDirectoryVM>(
                    "vw_LeagueDirectory",
                    reader => new LeagueDirectoryVM
                    {
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        SeasonLabel = DatabaseHelper.GetSafeString(reader, "SeasonLabel"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                        TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                        TeamsCount = DatabaseHelper.GetSafeInt32(reader, "TeamsCount"),
                        AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                        CreatedByUserID = DatabaseHelper.GetSafeInt32(reader, "CreatedByUserID"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    orderBy: "CreatedAt DESC"
                );
            }
            catch
            {
                return new List<LeagueDirectoryVM>();
            }
        }

        #endregion

        #region User Views

        /// <summary>
        /// Obtiene todos los usuarios activos (AccountStatus=1)
        /// VIEW: vw_UserProfileBasic con filtro
        /// </summary>
        public async Task<List<UserProfileBasicVM>> GetActiveUsersAsync()
        {
            try
            {
                return await _db.ExecuteViewAsync<UserProfileBasicVM>(
                    "vw_UserProfileBasic",
                    reader => new UserProfileBasicVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        Email = DatabaseHelper.GetSafeString(reader, "Email"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias"),
                        LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode"),
                        ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                        ProfileImageWidth = DatabaseHelper.GetSafeInt16(reader, "ProfileImageWidth") == 0
                            ? null : DatabaseHelper.GetSafeInt16(reader, "ProfileImageWidth"),
                        ProfileImageHeight = DatabaseHelper.GetSafeInt16(reader, "ProfileImageHeight") == 0
                            ? null : DatabaseHelper.GetSafeInt16(reader, "ProfileImageHeight"),
                        ProfileImageBytes = DatabaseHelper.GetSafeInt32(reader, "ProfileImageBytes") == 0
                            ? null : DatabaseHelper.GetSafeInt32(reader, "ProfileImageBytes"),
                        AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        SystemRoleCode = DatabaseHelper.GetSafeString(reader, "SystemRoleCode")
                    },
                    whereClause: "AccountStatus = 1",
                    orderBy: "CreatedAt DESC"
                );
            }
            catch
            {
                return new List<UserProfileBasicVM>();
            }
        }

        #endregion

        #region System Statistics

        /// <summary>
        /// Obtiene estadísticas generales del sistema
        /// Combina múltiples queries para dashboard administrativo
        /// </summary>
        public async Task<object> GetSystemStatsAsync()
        {
            try
            {
                // Total de usuarios
                var totalUsers = await _db.ExecuteViewAsync<int>(
                    "auth.UserAccount",
                    reader => 1 // contador
                );

                // Usuarios activos
                var activeUsers = await _db.ExecuteViewAsync<int>(
                    "auth.UserAccount",
                    reader => 1,
                    whereClause: "AccountStatus = 1"
                );

                // Total de ligas
                var totalLeagues = await _db.ExecuteViewAsync<int>(
                    "league.League",
                    reader => 1
                );

                // Ligas activas
                var activeLeagues = await _db.ExecuteViewAsync<int>(
                    "league.League",
                    reader => 1,
                    whereClause: "Status = 1"
                );

                // Ligas en Pre-Draft
                var preDraftLeagues = await _db.ExecuteViewAsync<int>(
                    "league.League",
                    reader => 1,
                    whereClause: "Status = 0"
                );

                // Total de equipos
                var totalTeams = await _db.ExecuteViewAsync<int>(
                    "league.Team",
                    reader => 1
                );

                // Sesiones activas
                var activeSessions = await _db.ExecuteViewAsync<int>(
                    "vw_UserActiveSessions",
                    reader => 1
                );

                return new
                {
                    Users = new
                    {
                        Total = totalUsers.Count,
                        Active = activeUsers.Count,
                        Inactive = totalUsers.Count - activeUsers.Count
                    },
                    Leagues = new
                    {
                        Total = totalLeagues.Count,
                        Active = activeLeagues.Count,
                        PreDraft = preDraftLeagues.Count,
                        InactiveOrClosed = totalLeagues.Count - activeLeagues.Count - preDraftLeagues.Count
                    },
                    Teams = new
                    {
                        Total = totalTeams.Count
                    },
                    Sessions = new
                    {
                        ActiveNow = activeSessions.Count
                    },
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = $"Error al obtener estadísticas: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        #endregion

        #region System Roles

        /// <summary>
        /// Obtiene lista de roles del sistema disponibles
        /// VIEW: vw_SystemRoles
        /// </summary>
        public async Task<List<SystemRoleVM>> GetSystemRolesAsync()
        {
            return await _db.ExecuteViewAsync<SystemRoleVM>(
                "vw_SystemRoles",
                reader => new SystemRoleVM
                {
                    RoleCode = DatabaseHelper.GetSafeString(reader, "RoleCode"),
                    Display = DatabaseHelper.GetSafeString(reader, "Display"),
                    Description = DatabaseHelper.GetSafeNullableString(reader, "Description")
                },
                orderBy: "RoleCode"
            );
        }

        /// <summary>
        /// Obtiene lista completa de usuarios con sus roles y estadísticas
        /// VIEW: vw_UsersWithRoles
        /// </summary>
        public async Task<List<UserWithFullRoleVM>> GetUsersWithRolesAsync()
        {
            return await _db.ExecuteViewAsync<UserWithFullRoleVM>(
                "vw_UsersWithRoles",
                reader => new UserWithFullRoleVM
                {
                    UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                    Email = DatabaseHelper.GetSafeString(reader, "Email"),
                    Name = DatabaseHelper.GetSafeString(reader, "Name"),
                    Alias = DatabaseHelper.GetSafeNullableString(reader, "Alias"),
                    SystemRoleCode = DatabaseHelper.GetSafeString(reader, "SystemRoleCode"),
                    SystemRoleDisplay = DatabaseHelper.GetSafeString(reader, "SystemRoleDisplay"),
                    SystemRoleDescription = DatabaseHelper.GetSafeNullableString(reader, "SystemRoleDescription"),
                    LanguageCode = DatabaseHelper.GetSafeString(reader, "LanguageCode"),
                    ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                    AccountStatus = DatabaseHelper.GetSafeByte(reader, "AccountStatus"),
                    AccountStatusDisplay = DatabaseHelper.GetSafeString(reader, "AccountStatusDisplay"),
                    FailedLoginCount = DatabaseHelper.GetSafeInt32(reader, "FailedLoginCount"),
                    LockedUntil = DatabaseHelper.GetSafeNullableDateTime(reader, "LockedUntil"),
                    CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                    UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                    TeamsCount = DatabaseHelper.GetSafeInt32(reader, "TeamsCount"),
                    CommissionedLeaguesCount = DatabaseHelper.GetSafeInt32(reader, "CommissionedLeaguesCount")
                },
                orderBy: "Name"
            );
        }

        #endregion
    }
}