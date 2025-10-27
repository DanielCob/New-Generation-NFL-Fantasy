using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de equipos fantasy
    /// Feature 3.1: Creación y administración de equipos fantasy
    /// </summary>
    public class TeamService : ITeamService
    {
        private readonly DatabaseHelper _db;

        public TeamService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Update Team Branding

        public async Task<ApiResponseDTO> UpdateTeamBrandingAsync(int teamId, UpdateTeamBrandingDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validación de imagen si viene
                if (dto.TeamImageBytes.HasValue)
                {
                    if (!dto.TeamImageWidth.HasValue || !dto.TeamImageHeight.HasValue)
                    {
                        return ApiResponseDTO.ErrorResponse("Si proporciona tamaño de imagen, debe incluir ancho y alto.");
                    }
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@TeamID", teamId),
                    new SqlParameter("@TeamName", DatabaseHelper.DbNullIfNull(dto.TeamName)),
                    new SqlParameter("@TeamImageUrl", DatabaseHelper.DbNullIfNull(dto.TeamImageUrl)),
                    new SqlParameter("@TeamImageWidth", DatabaseHelper.DbNullIfNull(dto.TeamImageWidth)),
                    new SqlParameter("@TeamImageHeight", DatabaseHelper.DbNullIfNull(dto.TeamImageHeight)),
                    new SqlParameter("@TeamImageBytes", DatabaseHelper.DbNullIfNull(dto.TeamImageBytes)),
                    new SqlParameter("@ThumbnailUrl", DatabaseHelper.DbNullIfNull(dto.ThumbnailUrl)),
                    new SqlParameter("@ThumbnailWidth", DatabaseHelper.DbNullIfNull(dto.ThumbnailWidth)),
                    new SqlParameter("@ThumbnailHeight", DatabaseHelper.DbNullIfNull(dto.ThumbnailHeight)),
                    new SqlParameter("@ThumbnailBytes", DatabaseHelper.DbNullIfNull(dto.ThumbnailBytes)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_UpdateTeamBranding",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al actualizar branding: {ex.Message}");
            }
        }

        #endregion

        #region Get My Team

        public async Task<MyTeamResponseDTO?> GetMyTeamAsync(int teamId, int actorUserId, string? filterPosition = null, string? searchPlayer = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@TeamID", teamId),
                    new SqlParameter("@FilterPosition", DatabaseHelper.DbNullIfNull(filterPosition)),
                    new SqlParameter("@SearchPlayer", DatabaseHelper.DbNullIfNull(searchPlayer))
                };

                MyTeamResponseDTO? myTeam = null;

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_GetMyTeam", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // RS1: Información del equipo
                    if (await reader.ReadAsync())
                    {
                        myTeam = new MyTeamResponseDTO
                        {
                            TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                            TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                            ManagerName = DatabaseHelper.GetSafeString(reader, "ManagerName"),
                            TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                            ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                            IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                            LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                            LeagueStatus = DatabaseHelper.GetSafeByte(reader, "LeagueStatus"),
                            CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                            UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                        };
                    }

                    // RS2: Roster de jugadores
                    if (myTeam != null && await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            myTeam.Roster.Add(new RosterPlayerDTO
                            {
                                RosterID = DatabaseHelper.GetSafeInt32(reader, "RosterID"),
                                PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                                FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                                LastName = DatabaseHelper.GetSafeString(reader, "LastName"),
                                FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                                Position = DatabaseHelper.GetSafeString(reader, "Position"),
                                NFLTeamName = DatabaseHelper.GetSafeNullableString(reader, "NFLTeamName"),
                                InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus"),
                                PhotoUrl = DatabaseHelper.GetSafeNullableString(reader, "PhotoUrl"),
                                AcquisitionType = DatabaseHelper.GetSafeString(reader, "AcquisitionType"),
                                AcquisitionDate = DatabaseHelper.GetSafeDateTime(reader, "AcquisitionDate"),
                                IsOnRoster = DatabaseHelper.GetSafeBool(reader, "IsOnRoster")
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
                                AcquisitionType = DatabaseHelper.GetSafeString(reader, "AcquisitionType"),
                                PlayerCount = DatabaseHelper.GetSafeInt32(reader, "PlayerCount"),
                                TotalPlayers = DatabaseHelper.GetSafeInt32(reader, "TotalPlayers"),
                                Percentage = DatabaseHelper.GetSafeDecimal(reader, "Percentage")
                            });
                        }
                    }
                }

                return myTeam;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Get Team Roster Distribution

        public async Task<List<RosterDistributionItemDTO>> GetTeamRosterDistributionAsync(int teamId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@TeamID", teamId)
                };

                return await _db.ExecuteStoredProcedureListAsync<RosterDistributionItemDTO>(
                    "app.sp_GetTeamRosterDistribution",
                    parameters,
                    reader => new RosterDistributionItemDTO
                    {
                        AcquisitionType = DatabaseHelper.GetSafeString(reader, "AcquisitionType"),
                        PlayerCount = DatabaseHelper.GetSafeInt32(reader, "PlayerCount"),
                        TotalPlayers = DatabaseHelper.GetSafeInt32(reader, "TotalPlayers"),
                        Percentage = DatabaseHelper.GetSafeDecimal(reader, "Percentage")
                    }
                );
            }
            catch
            {
                return new List<RosterDistributionItemDTO>();
            }
        }

        #endregion

        #region Add / Remove Player from Roster

        public async Task<ApiResponseDTO> AddPlayerToRosterAsync(int teamId, AddPlayerToRosterDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@TeamID", teamId),
                    new SqlParameter("@PlayerID", dto.PlayerID),
                    new SqlParameter("@AcquisitionType", dto.AcquisitionType),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var result = await _db.ExecuteStoredProcedureAsync<AddPlayerToRosterResponseDTO>(
                    "app.sp_AddPlayerToRoster",
                    parameters,
                    reader => new AddPlayerToRosterResponseDTO
                    {
                        RosterID = DatabaseHelper.GetSafeInt32(reader, "RosterID"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                if (result != null)
                {
                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al agregar jugador al roster.");
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al agregar jugador: {ex.Message}");
            }
        }

        public async Task<ApiResponseDTO> RemovePlayerFromRosterAsync(int rosterId, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@RosterID", rosterId),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_RemovePlayerFromRoster",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al remover jugador: {ex.Message}");
            }
        }

        #endregion

        #region Get Fantasy Team Details from VIEW

        public async Task<FantasyTeamDetailsVM?> GetFantasyTeamDetailsAsync(int teamId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<FantasyTeamDetailsVM>(
                    "vw_FantasyTeamDetails",
                    reader => new FantasyTeamDetailsVM
                    {
                        TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                        LeagueStatus = DatabaseHelper.GetSafeByte(reader, "LeagueStatus"),
                        OwnerUserID = DatabaseHelper.GetSafeInt32(reader, "OwnerUserID"),
                        ManagerName = DatabaseHelper.GetSafeString(reader, "ManagerName"),
                        ManagerEmail = DatabaseHelper.GetSafeString(reader, "ManagerEmail"),
                        ManagerAlias = DatabaseHelper.GetSafeNullableString(reader, "ManagerAlias"),
                        TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                        TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                        ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        RosterCount = DatabaseHelper.GetSafeInt32(reader, "RosterCount"),
                        DraftedCount = DatabaseHelper.GetSafeInt32(reader, "DraftedCount"),
                        TradedCount = DatabaseHelper.GetSafeInt32(reader, "TradedCount"),
                        FreeAgentCount = DatabaseHelper.GetSafeInt32(reader, "FreeAgentCount"),
                        ManagerProfileImage = DatabaseHelper.GetSafeNullableString(reader, "ManagerProfileImage"),
                        WaiverCount = DatabaseHelper.GetSafeInt32(reader, "WaiverCount")
                    },
                    whereClause: $"TeamID = {teamId}"
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
