using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de equipos NFL
    /// Feature 10.1: Gestión de Equipos NFL (CRUD)
    /// </summary>
    public class NFLTeamService : INFLTeamService
    {
        private readonly DatabaseHelper _db;

        public NFLTeamService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Create NFL Team

        public async Task<ApiResponseDTO> CreateNFLTeamAsync(CreateNFLTeamDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
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
                    new SqlParameter("@TeamName", dto.TeamName),
                    new SqlParameter("@City", dto.City),
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

                var result = await _db.ExecuteStoredProcedureAsync<CreateNFLTeamResponseDTO>(
                    "app.sp_CreateNFLTeam",
                    parameters,
                    reader => new CreateNFLTeamResponseDTO
                    {
                        NFLTeamID = DatabaseHelper.GetSafeInt32(reader, "NFLTeamID"),
                        TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                        City = DatabaseHelper.GetSafeString(reader, "City"),
                        Message = DatabaseHelper.GetSafeString(reader, "Message")
                    }
                );

                if (result != null)
                {
                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear equipo NFL.");
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region List NFL Teams

        public async Task<ListNFLTeamsResponseDTO> ListNFLTeamsAsync(ListNFLTeamsRequestDTO request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@PageNumber", request.PageNumber),
                    new SqlParameter("@PageSize", request.PageSize),
                    new SqlParameter("@SearchTerm", DatabaseHelper.DbNullIfNull(request.SearchTerm)),
                    new SqlParameter("@FilterCity", DatabaseHelper.DbNullIfNull(request.FilterCity)),
                    new SqlParameter("@FilterIsActive", DatabaseHelper.DbNullIfNull(request.FilterIsActive))
                };

                var response = new ListNFLTeamsResponseDTO();

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_ListNFLTeams", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // Leer equipos
                    while (await reader.ReadAsync())
                    {
                        response.Teams.Add(new NFLTeamListItemDTO
                        {
                            NFLTeamID = DatabaseHelper.GetSafeInt32(reader, "NFLTeamID"),
                            TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                            City = DatabaseHelper.GetSafeString(reader, "City"),
                            TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                            ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                            IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                            CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                            UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                        });

                        // Leer metadatos de la primera fila
                        if (response.TotalRecords == 0)
                        {
                            response.TotalRecords = DatabaseHelper.GetSafeInt32(reader, "TotalRecords");
                            response.CurrentPage = DatabaseHelper.GetSafeInt32(reader, "CurrentPage");
                            response.PageSize = DatabaseHelper.GetSafeInt32(reader, "PageSize");
                            response.TotalPages = DatabaseHelper.GetSafeInt32(reader, "TotalPages");
                        }
                    }
                }

                return response;
            }
            catch
            {
                return new ListNFLTeamsResponseDTO();
            }
        }

        #endregion

        #region Get NFL Team Details

        public async Task<NFLTeamDetailsDTO?> GetNFLTeamDetailsAsync(int nflTeamId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@NFLTeamID", nflTeamId)
                };

                NFLTeamDetailsDTO? details = null;

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_GetNFLTeamDetails", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // RS1: Información del equipo
                    if (await reader.ReadAsync())
                    {
                        details = new NFLTeamDetailsDTO
                        {
                            NFLTeamID = DatabaseHelper.GetSafeInt32(reader, "NFLTeamID"),
                            TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                            City = DatabaseHelper.GetSafeString(reader, "City"),
                            TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                            TeamImageWidth = DatabaseHelper.GetSafeInt16(reader, "TeamImageWidth") == 0 ? null : DatabaseHelper.GetSafeInt16(reader, "TeamImageWidth"),
                            TeamImageHeight = DatabaseHelper.GetSafeInt16(reader, "TeamImageHeight") == 0 ? null : DatabaseHelper.GetSafeInt16(reader, "TeamImageHeight"),
                            TeamImageBytes = DatabaseHelper.GetSafeNullableInt32(reader, "TeamImageBytes"),
                            ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                            ThumbnailWidth = DatabaseHelper.GetSafeInt16(reader, "ThumbnailWidth") == 0 ? null : DatabaseHelper.GetSafeInt16(reader, "ThumbnailWidth"),
                            ThumbnailHeight = DatabaseHelper.GetSafeInt16(reader, "ThumbnailHeight") == 0 ? null : DatabaseHelper.GetSafeInt16(reader, "ThumbnailHeight"),
                            ThumbnailBytes = DatabaseHelper.GetSafeNullableInt32(reader, "ThumbnailBytes"),
                            IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                            CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                            CreatedByName = DatabaseHelper.GetSafeNullableString(reader, "CreatedByName"),
                            UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                            UpdatedByName = DatabaseHelper.GetSafeNullableString(reader, "UpdatedByName")
                        };
                    }

                    // RS2: Historial de cambios
                    if (details != null && await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            details.ChangeHistory.Add(new NFLTeamChangeDTO
                            {
                                ChangeID = DatabaseHelper.GetSafeInt64(reader, "ChangeID"),
                                FieldName = DatabaseHelper.GetSafeString(reader, "FieldName"),
                                OldValue = DatabaseHelper.GetSafeNullableString(reader, "OldValue"),
                                NewValue = DatabaseHelper.GetSafeNullableString(reader, "NewValue"),
                                ChangedAt = DatabaseHelper.GetSafeDateTime(reader, "ChangedAt"),
                                ChangedByName = DatabaseHelper.GetSafeString(reader, "ChangedByName")
                            });
                        }
                    }

                    // RS3: Jugadores activos
                    if (details != null && await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            details.ActivePlayers.Add(new PlayerBasicDTO
                            {
                                PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                                FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                                LastName = DatabaseHelper.GetSafeString(reader, "LastName"),
                                FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                                Position = DatabaseHelper.GetSafeString(reader, "Position"),
                                InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus")
                            });
                        }
                    }
                }

                return details;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Update NFL Team

        public async Task<ApiResponseDTO> UpdateNFLTeamAsync(int nflTeamId, UpdateNFLTeamDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@NFLTeamID", nflTeamId),
                    new SqlParameter("@TeamName", DatabaseHelper.DbNullIfNull(dto.TeamName)),
                    new SqlParameter("@City", DatabaseHelper.DbNullIfNull(dto.City)),
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
                    "app.sp_UpdateNFLTeam",
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
                return ApiResponseDTO.ErrorResponse($"Error al actualizar equipo NFL: {ex.Message}");
            }
        }

        #endregion

        #region Deactivate / Reactivate

        [Authorize(Policy = "AdminOnly")]
        public async Task<ApiResponseDTO> DeactivateNFLTeamAsync(int nflTeamId, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@NFLTeamID", nflTeamId),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_DeactivateNFLTeam",
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
                return ApiResponseDTO.ErrorResponse($"Error al desactivar equipo NFL: {ex.Message}");
            }
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<ApiResponseDTO> ReactivateNFLTeamAsync(int nflTeamId, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@NFLTeamID", nflTeamId),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_ReactivateNFLTeam",
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
                return ApiResponseDTO.ErrorResponse($"Error al reactivar equipo NFL: {ex.Message}");
            }
        }

        #endregion

        #region Get Active NFL Teams

        public async Task<List<NFLTeamBasicVM>> GetActiveNFLTeamsAsync()
        {
            try
            {
                return await _db.ExecuteViewAsync<NFLTeamBasicVM>(
                    "vw_ActiveNFLTeams",
                    reader => new NFLTeamBasicVM
                    {
                        NFLTeamID = DatabaseHelper.GetSafeInt32(reader, "NFLTeamID"),
                        TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                        City = DatabaseHelper.GetSafeString(reader, "City"),
                        TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                        ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl")
                    },
                    orderBy: "TeamName"
                );
            }
            catch
            {
                return new List<NFLTeamBasicVM>();
            }
        }

        #endregion
    }
}
