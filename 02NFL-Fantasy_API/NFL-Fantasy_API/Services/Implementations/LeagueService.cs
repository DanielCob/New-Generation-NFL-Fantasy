using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;
using System.Text.RegularExpressions;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de ligas
    /// Maneja creación, edición, cambio de estado y consultas de ligas
    /// </summary>
    public class LeagueService : ILeagueService
    {
        private readonly DatabaseHelper _db;
        private static readonly byte[] ValidTeamSlots = { 4, 6, 8, 10, 12, 14, 16, 18, 20 };

        public LeagueService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        #region Create League

        /// <summary>
        /// Crea una nueva liga de fantasy
        /// SP: app.sp_CreateLeague
        /// Feature 1.2 - Crear liga
        /// </summary>
        public async Task<ApiResponseDTO> CreateLeagueAsync(CreateLeagueDTO dto, int creatorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validaciones previas
                if (!IsValidTeamSlots(dto.TeamSlots))
                {
                    return ApiResponseDTO.ErrorResponse("TeamSlots debe ser uno de: 4, 6, 8, 10, 12, 14, 16, 18, 20.");
                }

                if (dto.PlayoffTeams != 4 && dto.PlayoffTeams != 6)
                {
                    return ApiResponseDTO.ErrorResponse("PlayoffTeams debe ser 4 o 6.");
                }

                // Validar complejidad de contraseña de liga
                var passwordErrors = ValidateLeaguePasswordComplexity(dto.LeaguePassword);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                // NUEVO: si no hay nombre de equipo (o es solo espacios), enviar NULL al SP
                var initialTeamNameValue = string.IsNullOrWhiteSpace(dto.InitialTeamName)
                    ? (object)DBNull.Value
                    : dto.InitialTeamName!.Trim();

                var parameters = new SqlParameter[]
                {
            new SqlParameter("@CreatorUserID", creatorUserId),
            new SqlParameter("@Name", dto.Name),
            new SqlParameter("@Description", DatabaseHelper.DbNullIfNull(dto.Description)),
            new SqlParameter("@TeamSlots", dto.TeamSlots),
            new SqlParameter("@LeaguePassword", dto.LeaguePassword),
            // Antes enviaba siempre un string no-vacío; ahora puede ir DBNull.Value
            new SqlParameter("@InitialTeamName", initialTeamNameValue),
            new SqlParameter("@PlayoffTeams", dto.PlayoffTeams),
            new SqlParameter("@AllowDecimals", dto.AllowDecimals),
            new SqlParameter("@PositionFormatID", DatabaseHelper.DbNullIfNull(dto.PositionFormatID)),
            new SqlParameter("@ScoringSchemaID", DatabaseHelper.DbNullIfNull(dto.ScoringSchemaID)),
            new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
            new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                // sp_CreateLeague retorna un result set con información de la liga creada
                var result = await _db.ExecuteStoredProcedureAsync<CreateLeagueResponseDTO>(
                    "app.sp_CreateLeague",
                    parameters,
                    reader => new CreateLeagueResponseDTO
                    {
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                        AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                        Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                        PlayoffTeams = DatabaseHelper.GetSafeByte(reader, "PlayoffTeams"),
                        AllowDecimals = DatabaseHelper.GetSafeBool(reader, "AllowDecimals"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                        Message = "Liga creada exitosamente."
                    }
                );

                if (result != null)
                {
                    return ApiResponseDTO.SuccessResponse("Liga creada exitosamente.", result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear la liga.");
            }
            catch (SqlException ex)
            {
                // Errores específicos del SP (temporada no existe, nombre duplicado, etc.)
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error inesperado al crear liga: {ex.Message}");
            }
        }

        #endregion

        #region Edit League Config

        /// <summary>
        /// Edita la configuración de una liga
        /// SP: app.sp_EditLeagueConfig
        /// Feature 1.2 - Editar configuración de liga
        /// </summary>
        public async Task<ApiResponseDTO> EditLeagueConfigAsync(int leagueId, EditLeagueConfigDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validaciones previas
                if (dto.TeamSlots.HasValue && !IsValidTeamSlots(dto.TeamSlots.Value))
                {
                    return ApiResponseDTO.ErrorResponse("TeamSlots debe ser uno de: 4, 6, 8, 10, 12, 14, 16, 18, 20.");
                }

                if (dto.PlayoffTeams.HasValue && dto.PlayoffTeams.Value != 4 && dto.PlayoffTeams.Value != 6)
                {
                    return ApiResponseDTO.ErrorResponse("PlayoffTeams debe ser 4 o 6.");
                }

                if (dto.MaxRosterChangesPerTeam.HasValue)
                {
                    if (dto.MaxRosterChangesPerTeam.Value < 1 || dto.MaxRosterChangesPerTeam.Value > 100)
                    {
                        return ApiResponseDTO.ErrorResponse("MaxRosterChangesPerTeam debe estar entre 1 y 100, o null para sin límite.");
                    }
                }

                if (dto.MaxFreeAgentAddsPerTeam.HasValue)
                {
                    if (dto.MaxFreeAgentAddsPerTeam.Value < 1 || dto.MaxFreeAgentAddsPerTeam.Value > 100)
                    {
                        return ApiResponseDTO.ErrorResponse("MaxFreeAgentAddsPerTeam debe estar entre 1 y 100, o null para sin límite.");
                    }
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@LeagueID", leagueId),
                    new SqlParameter("@Name", DatabaseHelper.DbNullIfNull(dto.Name)),
                    new SqlParameter("@Description", DatabaseHelper.DbNullIfNull(dto.Description)),
                    new SqlParameter("@TeamSlots", DatabaseHelper.DbNullIfNull(dto.TeamSlots)),
                    new SqlParameter("@PositionFormatID", DatabaseHelper.DbNullIfNull(dto.PositionFormatID)),
                    new SqlParameter("@ScoringSchemaID", DatabaseHelper.DbNullIfNull(dto.ScoringSchemaID)),
                    new SqlParameter("@PlayoffTeams", DatabaseHelper.DbNullIfNull(dto.PlayoffTeams)),
                    new SqlParameter("@AllowDecimals", DatabaseHelper.DbNullIfNull(dto.AllowDecimals)),
                    new SqlParameter("@TradeDeadlineEnabled", DatabaseHelper.DbNullIfNull(dto.TradeDeadlineEnabled)),
                    new SqlParameter("@TradeDeadlineDate", DatabaseHelper.DbNullIfNull(dto.TradeDeadlineDate)),
                    new SqlParameter("@MaxRosterChangesPerTeam", DatabaseHelper.DbNullIfNull(dto.MaxRosterChangesPerTeam)),
                    new SqlParameter("@MaxFreeAgentAddsPerTeam", DatabaseHelper.DbNullIfNull(dto.MaxFreeAgentAddsPerTeam)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                var message = await _db.ExecuteStoredProcedureForMessageAsync(
                    "app.sp_EditLeagueConfig",
                    parameters
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                // Errores específicos del SP (permisos, estado incorrecto, validaciones)
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al editar configuración de liga: {ex.Message}");
            }
        }

        #endregion

        #region Set League Status

        /// <summary>
        /// Cambia el estado de una liga
        /// SP: app.sp_SetLeagueStatus
        /// Feature 1.2 - Administrar estado de liga
        /// </summary>
        public async Task<ApiResponseDTO> SetLeagueStatusAsync(int leagueId, SetLeagueStatusDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null)
        {
            try
            {
                // Validar que el estado sea válido (0-3)
                if (dto.NewStatus > 3)
                {
                    return ApiResponseDTO.ErrorResponse("Estado inválido. Debe ser: 0=PreDraft, 1=Active, 2=Inactive, 3=Closed.");
                }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@ActorUserID", actorUserId),
                    new SqlParameter("@LeagueID", leagueId),
                    new SqlParameter("@NewStatus", dto.NewStatus),
                    new SqlParameter("@Reason", DatabaseHelper.DbNullIfNull(dto.Reason)),
                    new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
                    new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
                };

                await _db.ExecuteStoredProcedureNonQueryAsync(
                    "app.sp_SetLeagueStatus",
                    parameters
                );

                string statusName = dto.NewStatus switch
                {
                    0 => "Pre-Draft",
                    1 => "Active",
                    2 => "Inactive",
                    3 => "Closed",
                    _ => "Unknown"
                };

                return ApiResponseDTO.SuccessResponse($"Estado de liga cambiado a {statusName}.");
            }
            catch (SqlException ex)
            {
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponseDTO.ErrorResponse($"Error al cambiar estado de liga: {ex.Message}");
            }
        }

        #endregion

        #region Get League Summary

        /// <summary>
        /// Obtiene resumen completo de una liga
        /// SP: app.sp_GetLeagueSummary (retorna 2 result sets)
        /// Feature 1.2 - Ver liga
        /// </summary>
        public async Task<LeagueSummaryDTO?> GetLeagueSummaryAsync(int leagueId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@LeagueID", leagueId)
                };

                LeagueSummaryDTO? summary = null;

                using (var connection = new SqlConnection(_db.GetType().GetField("_connectionString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(_db) as string))
                {
                    using var command = new SqlCommand("app.sp_GetLeagueSummary", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    command.Parameters.AddRange(parameters);

                    await connection.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();

                    // Result Set 1: Datos de la liga
                    if (await reader.ReadAsync())
                    {
                        summary = new LeagueSummaryDTO
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
                            ScoringVersion = DatabaseHelper.GetSafeInt32(reader, "Version"),
                            SeasonID = DatabaseHelper.GetSafeInt32(reader, "SeasonID"),
                            SeasonLabel = DatabaseHelper.GetSafeString(reader, "SeasonLabel"),
                            Year = DatabaseHelper.GetSafeInt32(reader, "Year"),
                            StartDate = DatabaseHelper.GetSafeDateTime(reader, "StartDate"),
                            EndDate = DatabaseHelper.GetSafeDateTime(reader, "EndDate"),
                            CreatedByUserID = DatabaseHelper.GetSafeInt32(reader, "CreatedByUserID"),
                            CreatedByName = DatabaseHelper.GetSafeString(reader, "CreatedByName"),
                            CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                            UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt")
                        };
                    }

                    // Result Set 2: Equipos de la liga
                    if (summary != null && await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            summary.Teams.Add(new LeagueTeamDTO
                            {
                                TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                                TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                                OwnerUserID = DatabaseHelper.GetSafeInt32(reader, "OwnerUserID"),
                                OwnerName = DatabaseHelper.GetSafeString(reader, "OwnerName"),
                                CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                            });
                        }
                    }
                }

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting league summary: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region View-based Queries

        /// <summary>
        /// Obtiene el directorio de ligas
        /// VIEW: vw_LeagueDirectory
        /// </summary>
        public async Task<List<LeagueDirectoryVM>> GetLeagueDirectoryAsync()
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

        /// <summary>
        /// Obtiene miembros de una liga
        /// VIEW: vw_LeagueMembers
        /// </summary>
        public async Task<List<LeagueMemberVM>> GetLeagueMembersAsync(int leagueId)
        {
            try
            {
                return await _db.ExecuteViewAsync<LeagueMemberVM>(
                    "vw_LeagueMembers",
                    reader => new LeagueMemberVM
                    {
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        LeagueRoleCode = DatabaseHelper.GetSafeString(reader, "LeagueRoleCode"),
                        UserAlias = DatabaseHelper.GetSafeNullableString(reader, "UserAlias"),
                        SystemRoleDisplay = DatabaseHelper.GetSafeString(reader, "SystemRoleDisplay"),
                        ProfileImageUrl = DatabaseHelper.GetSafeNullableString(reader, "ProfileImageUrl"),
                        IsPrimaryCommissioner = DatabaseHelper.GetSafeBool(reader, "IsPrimaryCommissioner"),
                        JoinedAt = DatabaseHelper.GetSafeDateTime(reader, "JoinedAt"),
                        LeftAt = DatabaseHelper.GetSafeNullableDateTime(reader, "LeftAt"),
                        UserName = DatabaseHelper.GetSafeString(reader, "UserName"),
                        UserEmail = DatabaseHelper.GetSafeString(reader, "UserEmail")
                    },
                    whereClause: $"LeagueID = {leagueId}",
                    orderBy: "JoinedAt"
                );
            }
            catch
            {
                return new List<LeagueMemberVM>();
            }
        }

        /// <summary>
        /// Obtiene equipos de una liga
        /// VIEW: vw_LeagueTeams (ACTUALIZADA)
        /// </summary>
        public async Task<List<LeagueTeamVM>> GetLeagueTeamsAsync(int leagueId)
        {
            try
            {
                return await _db.ExecuteViewAsync<LeagueTeamVM>(
                    "vw_LeagueTeams",
                    reader => new LeagueTeamVM
                    {
                        TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                        OwnerUserID = DatabaseHelper.GetSafeInt32(reader, "OwnerUserID"),
                        OwnerName = DatabaseHelper.GetSafeString(reader, "OwnerName"),
                        TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                        ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        RosterCount = DatabaseHelper.GetSafeInt32(reader, "RosterCount"),
                        UpdatedAt = DatabaseHelper.GetSafeDateTime(reader, "UpdatedAt"),
                        OwnerProfileImage = DatabaseHelper.GetSafeNullableString(reader, "OwnerProfileImage"),
                        CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt")
                    },
                    whereClause: $"LeagueID = {leagueId}",
                    orderBy: "CreatedAt"
                );
            }
            catch
            {
                return new List<LeagueTeamVM>();
            }
        }

        /// <summary>
        /// Obtiene todos los roles efectivos de un usuario en una liga
        /// SP: app.sp_GetUserRolesInLeague
        /// Retorna roles (explícitos + derivados) y resumen
        /// </summary>
        public async Task<GetUserRolesInLeagueResponseDTO?> GetUserRolesInLeagueAsync(int userId, int leagueId)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
            new SqlParameter("@UserID", userId),
            new SqlParameter("@LeagueID", leagueId)
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
                                RoleName = DatabaseHelper.GetSafeString(reader, "RoleName"),
                                IsExplicit = DatabaseHelper.GetSafeBool(reader, "IsExplicit"),
                                IsDerived = DatabaseHelper.GetSafeBool(reader, "IsDerived"),
                                IsPrimaryCommissioner = DatabaseHelper.GetSafeBool(reader, "IsPrimaryCommissioner"),
                                JoinedAt = DatabaseHelper.GetSafeDateTime(reader, "JoinedAt")
                            });
                        }

                        // Segundo result set: resumen
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            result.Summary = new UserLeagueRoleSummaryDTO
                            {
                                PrimaryRole = DatabaseHelper.GetSafeString(reader, "PrimaryRole"),
                                AllRoles = DatabaseHelper.GetSafeString(reader, "AllRoles"),
                                IsPrimaryCommissioner = DatabaseHelper.GetSafeBool(reader, "IsPrimaryCommissioner")
                            };
                        }
                    }
                );

                return result;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Error en base de datos al obtener roles: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener roles de usuario en liga: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene ligas donde el usuario es comisionado
        /// VIEW: vw_UserCommissionedLeagues
        /// </summary>
        public async Task<List<UserCommissionedLeagueVM>> GetUserCommissionedLeaguesAsync(int userId)
        {
            try
            {
                return await _db.ExecuteViewAsync<UserCommissionedLeagueVM>(
                    "vw_UserCommissionedLeagues",
                    reader => new UserCommissionedLeagueVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                        Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                        TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                        AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                        RoleCode = DatabaseHelper.GetSafeString(reader, "RoleCode"),
                        IsPrimaryCommissioner = DatabaseHelper.GetSafeBool(reader, "IsPrimaryCommissioner"),
                        JoinedAt = DatabaseHelper.GetSafeDateTime(reader, "JoinedAt"),
                        LeagueCreatedAt = DatabaseHelper.GetSafeDateTime(reader, "LeagueCreatedAt")
                    },
                    whereClause: $"UserID = {userId}",
                    orderBy: "LeagueCreatedAt DESC"
                );
            }
            catch
            {
                return new List<UserCommissionedLeagueVM>();
            }
        }

        /// <summary>
        /// Obtiene equipos del usuario en todas sus ligas
        /// VIEW: vw_UserTeams (ACTUALIZADA)
        /// </summary>
        public async Task<List<UserTeamVM>> GetUserTeamsAsync(int userId)
        {
            try
            {
                return await _db.ExecuteViewAsync<UserTeamVM>(
                    "vw_UserTeams",
                    reader => new UserTeamVM
                    {
                        UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                        TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                        LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                        LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                        TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                        TeamImageUrl = DatabaseHelper.GetSafeNullableString(reader, "TeamImageUrl"),
                        ThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "ThumbnailUrl"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive"),
                        RosterCount = DatabaseHelper.GetSafeInt32(reader, "RosterCount"),
                        TeamCreatedAt = DatabaseHelper.GetSafeDateTime(reader, "TeamCreatedAt"),
                        LeagueStatus = DatabaseHelper.GetSafeByte(reader, "LeagueStatus")
                    },
                    whereClause: $"UserID = {userId}",
                    orderBy: "TeamCreatedAt DESC"
                );
            }
            catch
            {
                return new List<UserTeamVM>();
            }
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Valida que TeamSlots sea válido
        /// Valores: 4, 6, 8, 10, 12, 14, 16, 18, 20
        /// </summary>
        public bool IsValidTeamSlots(byte teamSlots)
        {
            return ValidTeamSlots.Contains(teamSlots);
        }

        /// <summary>
        /// Valida complejidad de contraseña de liga (misma política que usuarios)
        /// </summary>
        public List<string> ValidateLeaguePasswordComplexity(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("La contraseña de liga es obligatoria.");
                return errors;
            }

            if (password.Length < 8 || password.Length > 12)
            {
                errors.Add("La contraseña de liga debe tener entre 8 y 12 caracteres.");
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos una letra mayúscula.");
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos una letra minúscula.");
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("La contraseña de liga debe incluir al menos un dígito.");
            }

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
            {
                errors.Add("La contraseña de liga debe ser alfanumérica (solo letras y números).");
            }

            return errors;
        }

        #endregion

        // ============================================================================
        // IMPLEMENTACIÓN - Búsqueda y Unión
        // ============================================================================

        public async Task<List<SearchLeaguesResultDTO>> SearchLeaguesAsync(SearchLeaguesRequestDTO request)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@SearchTerm", DatabaseHelper.DbNullIfNull(request.SearchTerm)),
        new SqlParameter("@SeasonID", DatabaseHelper.DbNullIfNull(request.SeasonID)),
        new SqlParameter("@MinSlots", DatabaseHelper.DbNullIfNull(request.MinSlots)),
        new SqlParameter("@MaxSlots", DatabaseHelper.DbNullIfNull(request.MaxSlots)),
        new SqlParameter("@PageNumber", request.PageNumber),
        new SqlParameter("@PageSize", request.PageSize)
            };

            var results = new List<SearchLeaguesResultDTO>();
            var connectionString = _db.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("app.sp_SearchLeagues", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new SearchLeaguesResultDTO
                            {
                                LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                                Name = DatabaseHelper.GetSafeString(reader, "Name"),
                                Description = DatabaseHelper.GetSafeString(reader, "Description"),
                                TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                                TeamsCount = DatabaseHelper.GetSafeInt32(reader, "TeamsCount"),
                                AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                                PlayoffTeams = DatabaseHelper.GetSafeByte(reader, "PlayoffTeams"),
                                AllowDecimals = DatabaseHelper.GetSafeBool(reader, "AllowDecimals"),
                                SeasonLabel = DatabaseHelper.GetSafeString(reader, "SeasonLabel"),
                                SeasonYear = DatabaseHelper.GetSafeInt32(reader, "SeasonYear"),
                                CreatedByName = DatabaseHelper.GetSafeString(reader, "CreatedByName"),
                                CreatedAt = DatabaseHelper.GetSafeDateTime(reader, "CreatedAt"),
                                TotalRecords = DatabaseHelper.GetSafeInt32(reader, "TotalRecords"),
                                CurrentPage = DatabaseHelper.GetSafeInt32(reader, "CurrentPage"),
                                PageSize = DatabaseHelper.GetSafeInt32(reader, "PageSize"),
                                TotalPages = DatabaseHelper.GetSafeInt32(reader, "TotalPages")
                            });
                        }
                    }
                }
            }

            return results;
        }

        public async Task<JoinLeagueResultDTO> JoinLeagueAsync(
    int userId,
    JoinLeagueRequestDTO request,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@UserID", userId),
        new SqlParameter("@LeagueID", request.LeagueID),
        new SqlParameter("@LeaguePassword", request.LeaguePassword),
        new SqlParameter("@TeamName", request.TeamName),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            // Retorna un solo resultado, igual que CreateLeague
            var result = await _db.ExecuteStoredProcedureAsync<JoinLeagueResultDTO>(
                "app.sp_JoinLeague",
                parameters,
                reader => new JoinLeagueResultDTO
                {
                    TeamID = DatabaseHelper.GetSafeInt32(reader, "TeamID"),
                    LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                    TeamName = DatabaseHelper.GetSafeString(reader, "TeamName"),
                    LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                    AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                    Message = DatabaseHelper.GetSafeString(reader, "Message")
                }
            );

            if (result == null)
            {
                throw new InvalidOperationException("No se recibió respuesta del procedimiento almacenado sp_JoinLeague.");
            }

            return result;
        }

        public async Task<ValidateLeaguePasswordResultDTO> ValidateLeaguePasswordAsync(
    ValidateLeaguePasswordRequestDTO request)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@LeagueID", request.LeagueID),
        new SqlParameter("@LeaguePassword", request.LeaguePassword)
            };

            var result = await _db.ExecuteStoredProcedureAsync<ValidateLeaguePasswordResultDTO>(
                "app.sp_ValidateLeaguePassword",
                parameters,
                reader => new ValidateLeaguePasswordResultDTO
                {
                    IsValid = DatabaseHelper.GetSafeBool(reader, "IsValid"),
                    Message = DatabaseHelper.GetSafeString(reader, "Message")
                }
            );

            return result ?? new ValidateLeaguePasswordResultDTO
            {
                IsValid = false,
                Message = "Error al validar contraseña de la liga."
            };
        }

        // ============================================================================
        // IMPLEMENTACIÓN - Gestión de Miembros
        // ============================================================================

        public async Task<ApiResponseDTO> RemoveTeamFromLeagueAsync(
    int actorUserId,
    int leagueId,
    RemoveTeamRequestDTO request,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ActorUserID", actorUserId),
        new SqlParameter("@LeagueID", leagueId),
        new SqlParameter("@TeamID", request.TeamID),
        new SqlParameter("@Reason", DatabaseHelper.DbNullIfNull(request.Reason)),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            var result = await _db.ExecuteStoredProcedureAsync<RemoveTeamResultDTO>(
                "app.sp_RemoveTeamFromLeague",
                parameters,
                reader => new RemoveTeamResultDTO
                {
                    AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                    Message = DatabaseHelper.GetSafeString(reader, "Message")
                }
            );

            return ApiResponseDTO.SuccessResponse(
                result?.Message ?? "Equipo removido exitosamente.",
                new { AvailableSlots = result?.AvailableSlots ?? 0 }
            );
        }

        public async Task<ApiResponseDTO> LeaveLeagueAsync(
    int userId,
    int leagueId,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@UserID", userId),
        new SqlParameter("@LeagueID", leagueId),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            var result = await _db.ExecuteStoredProcedureAsync<LeaveLeagueResultDTO>(
                "app.sp_LeaveLeague",
                parameters,
                reader => new LeaveLeagueResultDTO
                {
                    Message = DatabaseHelper.GetSafeString(reader, "Message")
                }
            );

            return ApiResponseDTO.SuccessResponse(
                result?.Message ?? "Has salido exitosamente de la liga."
            );
        }

        public async Task<ApiResponseDTO> AssignCoCommissionerAsync(
    int actorUserId,
    int leagueId,
    AssignCoCommissionerRequestDTO request,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ActorUserID", actorUserId),
        new SqlParameter("@LeagueID", leagueId),
        new SqlParameter("@TargetUserID", request.TargetUserID),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            var result = await _db.ExecuteStoredProcedureAsync<AssignCoCommissionerResultDTO>(
                "app.sp_AssignCoCommissioner",
                parameters,
                reader => new AssignCoCommissionerResultDTO
                {
                    Message = DatabaseHelper.GetSafeString(reader, "Message"),
                    UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                    UserName = DatabaseHelper.GetSafeString(reader, "UserName"),
                    NewRole = DatabaseHelper.GetSafeString(reader, "NewRole")
                }
            );

            return ApiResponseDTO.SuccessResponse(
                result?.Message ?? "Co-comisionado asignado exitosamente.",
                new
                {
                    UserID = result?.UserID,
                    UserName = result?.UserName,
                    NewRole = result?.NewRole
                }
            );
        }

        public async Task<ApiResponseDTO> RemoveCoCommissionerAsync(
    int actorUserId,
    int leagueId,
    RemoveCoCommissionerRequestDTO request,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ActorUserID", actorUserId),
        new SqlParameter("@LeagueID", leagueId),
        new SqlParameter("@TargetUserID", request.TargetUserID),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            var result = await _db.ExecuteStoredProcedureAsync<RemoveCoCommissionerResultDTO>(
                "app.sp_RemoveCoCommissioner",
                parameters,
                reader => new RemoveCoCommissionerResultDTO
                {
                    Message = DatabaseHelper.GetSafeString(reader, "Message"),
                    UserID = DatabaseHelper.GetSafeInt32(reader, "UserID"),
                    UserName = DatabaseHelper.GetSafeString(reader, "UserName")
                }
            );

            return ApiResponseDTO.SuccessResponse(
                result?.Message ?? "Co-comisionado removido exitosamente.",
                new
                {
                    UserID = result?.UserID,
                    UserName = result?.UserName
                }
            );
        }

        public async Task<ApiResponseDTO> TransferCommissionerAsync(
    int actorUserId,
    int leagueId,
    TransferCommissionerRequestDTO request,
    string? sourceIp,
    string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ActorUserID", actorUserId),
        new SqlParameter("@LeagueID", leagueId),
        new SqlParameter("@NewCommissionerID", request.NewCommissionerID),
        new SqlParameter("@SourceIp", DatabaseHelper.DbNullIfNull(sourceIp)),
        new SqlParameter("@UserAgent", DatabaseHelper.DbNullIfNull(userAgent))
            };

            var result = await _db.ExecuteStoredProcedureAsync<TransferCommissionerResultDTO>(
                "app.sp_TransferCommissioner",
                parameters,
                reader => new TransferCommissionerResultDTO
                {
                    Message = DatabaseHelper.GetSafeString(reader, "Message"),
                    NewCommissionerID = DatabaseHelper.GetSafeInt32(reader, "NewCommissionerID"),
                    NewCommissionerName = DatabaseHelper.GetSafeString(reader, "NewCommissionerName")
                }
            );

            return ApiResponseDTO.SuccessResponse(
                result?.Message ?? "Comisionado transferido exitosamente.",
                new
                {
                    NewCommissionerID = result?.NewCommissionerID,
                    NewCommissionerName = result?.NewCommissionerName
                }
            );
        }

        public async Task<LeaguePasswordInfoDTO> GetLeaguePasswordInfoAsync(
    int actorUserId,
    int leagueId)
        {
            var parameters = new SqlParameter[]
            {
        new SqlParameter("@ActorUserID", actorUserId),
        new SqlParameter("@LeagueID", leagueId)
            };

            var result = await _db.ExecuteStoredProcedureAsync<LeaguePasswordInfoDTO>(
                "app.sp_GetLeaguePassword",
                parameters,
                reader => new LeaguePasswordInfoDTO
                {
                    LeagueID = DatabaseHelper.GetSafeInt32(reader, "LeagueID"),
                    LeagueName = DatabaseHelper.GetSafeString(reader, "LeagueName"),
                    Status = DatabaseHelper.GetSafeByte(reader, "Status"),
                    TeamSlots = DatabaseHelper.GetSafeByte(reader, "TeamSlots"),
                    TeamsCount = DatabaseHelper.GetSafeInt32(reader, "TeamsCount"),
                    AvailableSlots = DatabaseHelper.GetSafeInt32(reader, "AvailableSlots"),
                    Message = DatabaseHelper.GetSafeString(reader, "Message")
                }
            );

            if (result == null)
            {
                throw new InvalidOperationException("No se pudo obtener información de la liga.");
            }

            return result;
        }

        // ============================================================================
        // CLASES AUXILIARES PRIVADAS PARA MAPEO DE RESULTADOS
        // ============================================================================

        private class RemoveTeamResultDTO
        {
            public int AvailableSlots { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        private class LeaveLeagueResultDTO
        {
            public string Message { get; set; } = string.Empty;
        }

        private class AssignCoCommissionerResultDTO
        {
            public string Message { get; set; } = string.Empty;
            public int UserID { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string NewRole { get; set; } = string.Empty;
        }

        private class RemoveCoCommissionerResultDTO
        {
            public string Message { get; set; } = string.Empty;
            public int UserID { get; set; }
            public string UserName { get; set; } = string.Empty;
        }

        private class TransferCommissionerResultDTO
        {
            public string Message { get; set; } = string.Empty;
            public int NewCommissionerID { get; set; }
            public string NewCommissionerName { get; set; } = string.Empty;
        }
    }
}
