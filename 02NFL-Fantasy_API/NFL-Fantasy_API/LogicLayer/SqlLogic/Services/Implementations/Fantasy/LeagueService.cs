using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.SharedSystems.Validators;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Fantasy
{
    /// <summary>
    /// Implementación del servicio de gestión de ligas.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a LeagueDataAccess).
    /// NO ejecuta validaciones directamente (delegado a LeagueValidator).
    /// </summary>
    public class LeagueService : ILeagueService
    {
        private readonly LeagueDataAccess _dataAccess;
        private readonly ILogger<LeagueService> _logger;

        public LeagueService(
            LeagueDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<LeagueService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Create League

        /// <summary>
        /// Crea una nueva liga de fantasy.
        /// SP: app.sp_CreateLeague
        /// Feature 1.2 - Crear liga
        /// </summary>
        public async Task<ApiResponseDTO> CreateLeagueAsync(
            CreateLeagueDTO dto,
            int creatorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a LeagueValidator
                if (!LeagueValidator.IsValidTeamSlots(dto.TeamSlots))
                {
                    return ApiResponseDTO.ErrorResponse(
                        "TeamSlots debe ser uno de: 4, 6, 8, 10, 12, 14, 16, 18, 20."
                    );
                }

                if (!LeagueValidator.IsValidPlayoffTeams(dto.PlayoffTeams))
                {
                    return ApiResponseDTO.ErrorResponse("PlayoffTeams debe ser 4 o 6.");
                }

                var passwordErrors = LeagueValidator.ValidateLeaguePasswordComplexity(dto.LeaguePassword);
                if (passwordErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", passwordErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.CreateLeagueAsync(dto, creatorUserId, sourceIp, userAgent);

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {UserID} created league: {LeagueName} (ID: {LeagueID}) from {IP}",
                        creatorUserId,
                        dto.Name,
                        result.LeagueID,
                        sourceIp
                    );

                    return ApiResponseDTO.SuccessResponse("Liga creada exitosamente.", result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear la liga.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al crear liga: User={UserID}, LeagueName={LeagueName}",
                    creatorUserId,
                    dto.Name
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear liga: User={UserID}, LeagueName={LeagueName}",
                    creatorUserId,
                    dto.Name
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado al crear liga: {ex.Message}");
            }
        }

        #endregion

        #region Edit League Config

        /// <summary>
        /// Edita la configuración de una liga.
        /// SP: app.sp_EditLeagueConfig
        /// Feature 1.2 - Editar configuración de liga
        /// </summary>
        public async Task<ApiResponseDTO> EditLeagueConfigAsync(
            int leagueId,
            EditLeagueConfigDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a LeagueValidator
                if (dto.TeamSlots.HasValue && !LeagueValidator.IsValidTeamSlots(dto.TeamSlots.Value))
                {
                    return ApiResponseDTO.ErrorResponse(
                        "TeamSlots debe ser uno de: 4, 6, 8, 10, 12, 14, 16, 18, 20."
                    );
                }

                if (dto.PlayoffTeams.HasValue && !LeagueValidator.IsValidPlayoffTeams(dto.PlayoffTeams.Value))
                {
                    return ApiResponseDTO.ErrorResponse("PlayoffTeams debe ser 4 o 6.");
                }

                var rosterLimitErrors = LeagueValidator.ValidateRosterLimits(
                    dto.MaxRosterChangesPerTeam,
                    dto.MaxFreeAgentAddsPerTeam
                );

                if (rosterLimitErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", rosterLimitErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.EditLeagueConfigAsync(
                    leagueId,
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {UserID} edited config for league {LeagueID} from {IP}",
                    actorUserId,
                    leagueId,
                    sourceIp
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al editar configuración: Actor={ActorUserID}, League={LeagueID}",
                    actorUserId,
                    leagueId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al editar configuración de liga: League={LeagueID}",
                    leagueId
                );
                return ApiResponseDTO.ErrorResponse($"Error al editar configuración de liga: {ex.Message}");
            }
        }

        #endregion

        #region Set League Status

        /// <summary>
        /// Cambia el estado de una liga.
        /// SP: app.sp_SetLeagueStatus
        /// Feature 1.2 - Administrar estado de liga
        /// </summary>
        public async Task<ApiResponseDTO> SetLeagueStatusAsync(
            int leagueId,
            SetLeagueStatusDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Estado válido
                if (dto.NewStatus > 3)
                {
                    return ApiResponseDTO.ErrorResponse(
                        "Estado inválido. Debe ser: 0=PreDraft, 1=Active, 2=Inactive, 3=Closed."
                    );
                }

                // EJECUCIÓN: Delegada a DataAccess
                await _dataAccess.SetLeagueStatusAsync(leagueId, dto, actorUserId, sourceIp, userAgent);

                string statusName = dto.NewStatus switch
                {
                    0 => "Pre-Draft",
                    1 => "Active",
                    2 => "Inactive",
                    3 => "Closed",
                    _ => "Unknown"
                };

                _logger.LogInformation(
                    "User {UserID} changed status of league {LeagueID} to {NewStatus} from {IP}",
                    actorUserId,
                    leagueId,
                    dto.NewStatus,
                    sourceIp
                );

                return ApiResponseDTO.SuccessResponse($"Estado de liga cambiado a {statusName}.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al cambiar estado: Actor={ActorUserID}, League={LeagueID}",
                    actorUserId,
                    leagueId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado de liga: League={LeagueID}",
                    leagueId
                );
                return ApiResponseDTO.ErrorResponse($"Error al cambiar estado de liga: {ex.Message}");
            }
        }

        #endregion

        #region Get League Summary

        /// <summary>
        /// Obtiene resumen completo de una liga.
        /// SP: app.sp_GetLeagueSummary (2 result sets)
        /// Feature 1.2 - Ver liga
        /// </summary>
        public async Task<LeagueSummaryDTO?> GetLeagueSummaryAsync(int leagueId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetLeagueSummaryAsync(leagueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de liga {LeagueID}", leagueId);
                throw;
            }
        }

        #endregion

        #region Search and Join

        /// <summary>
        /// Busca ligas disponibles para unirse.
        /// SP: app.sp_SearchLeagues
        /// </summary>
        public async Task<List<SearchLeaguesResultDTO>> SearchLeaguesAsync(SearchLeaguesRequestDTO request)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.SearchLeaguesAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar ligas");
                throw;
            }
        }

        /// <summary>
        /// Une a un usuario a una liga existente.
        /// SP: app.sp_JoinLeague
        /// </summary>
        public async Task<JoinLeagueResultDTO> JoinLeagueAsync(
            int userId,
            JoinLeagueRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.JoinLeagueAsync(userId, request, sourceIp, userAgent);

                if (result == null)
                {
                    throw new InvalidOperationException(
                        "No se recibió respuesta del procedimiento almacenado sp_JoinLeague."
                    );
                }

                _logger.LogInformation(
                    "User {UserID} joined league {LeagueID} from {IP}",
                    userId,
                    request.LeagueID,
                    sourceIp
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al unirse a liga: User={UserID}, League={LeagueID}",
                    userId,
                    request.LeagueID
                );
                throw;
            }
        }

        /// <summary>
        /// Valida si una contraseña de liga es correcta.
        /// SP: app.sp_ValidateLeaguePassword
        /// </summary>
        public async Task<ValidateLeaguePasswordResultDTO> ValidateLeaguePasswordAsync(
            ValidateLeaguePasswordRequestDTO request)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.ValidateLeaguePasswordAsync(request);

                return result ?? new ValidateLeaguePasswordResultDTO
                {
                    IsValid = false,
                    Message = "Error al validar contraseña de la liga."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al validar contraseña de liga {LeagueID}",
                    request.LeagueID
                );
                return new ValidateLeaguePasswordResultDTO
                {
                    IsValid = false,
                    Message = "Error al validar contraseña de la liga."
                };
            }
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Remueve un equipo de la liga.
        /// SP: app.sp_RemoveTeamFromLeague
        /// </summary>
        public async Task<ApiResponseDTO> RemoveTeamFromLeagueAsync(
            int actorUserId,
            int leagueId,
            RemoveTeamRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.RemoveTeamFromLeagueAsync(
                    actorUserId,
                    leagueId,
                    request,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {UserID} removed team {TeamID} from league {LeagueID} from {IP}",
                    actorUserId,
                    request.TeamID,
                    leagueId,
                    sourceIp
                );

                return ApiResponseDTO.SuccessResponse(
                    result?.Message ?? "Equipo removido exitosamente.",
                    new { AvailableSlots = result?.AvailableSlots ?? 0 }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al remover equipo: Actor={ActorUserID}, Team={TeamID}, League={LeagueID}",
                    actorUserId,
                    request.TeamID,
                    leagueId
                );
                throw;
            }
        }

        /// <summary>
        /// Permite a un usuario salir voluntariamente de una liga.
        /// SP: app.sp_LeaveLeague
        /// </summary>
        public async Task<ApiResponseDTO> LeaveLeagueAsync(
            int userId,
            int leagueId,
            string? sourceIp,
            string? userAgent)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.LeaveLeagueAsync(userId, leagueId, sourceIp, userAgent);

                _logger.LogInformation(
                    "User {UserID} left league {LeagueID} from {IP}",
                    userId,
                    leagueId,
                    sourceIp
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al salir de liga: User={UserID}, League={LeagueID}",
                    userId,
                    leagueId
                );
                throw;
            }
        }

        /// <summary>
        /// Transfiere el rol de comisionado principal a otro miembro.
        /// SP: app.sp_TransferCommissioner
        /// </summary>
        public async Task<ApiResponseDTO> TransferCommissionerAsync(
            int actorUserId,
            int leagueId,
            TransferCommissionerRequestDTO request,
            string? sourceIp,
            string? userAgent)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.TransferCommissionerAsync(
                    actorUserId,
                    leagueId,
                    request,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserID} transferred commissioner role to User {NewCommissionerID} in league {LeagueID} from {IP}",
                    actorUserId,
                    request.NewCommissionerID,
                    leagueId,
                    sourceIp
                );

                return ApiResponseDTO.SuccessResponse(
                    result?.Message ?? "Comisionado transferido exitosamente.",
                    new
                    {
                        result?.NewCommissionerID,
                        result?.NewCommissionerName
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al transferir comisionado: Actor={ActorUserID}, NewCommissioner={NewCommissionerID}, League={LeagueID}",
                    actorUserId,
                    request.NewCommissionerID,
                    leagueId
                );
                throw;
            }
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
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserRolesInLeagueAsync(userId, leagueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener roles: User={UserID}, League={LeagueID}",
                    userId,
                    leagueId
                );
                throw;
            }
        }

        #endregion

        #region View Queries

        /// <summary>
        /// Obtiene el directorio de ligas.
        /// VIEW: vw_LeagueDirectory
        /// </summary>
        public async Task<List<LeagueDirectoryVM>> GetLeagueDirectoryAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetLeagueDirectoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener directorio de ligas");
                throw;
            }
        }

        /// <summary>
        /// Obtiene miembros de una liga.
        /// VIEW: vw_LeagueMembers
        /// </summary>
        public async Task<List<LeagueMemberVM>> GetLeagueMembersAsync(int leagueId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetLeagueMembersAsync(leagueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener miembros de liga {LeagueID}", leagueId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene equipos de una liga.
        /// VIEW: vw_LeagueTeams
        /// </summary>
        public async Task<List<LeagueTeamVM>> GetLeagueTeamsAsync(int leagueId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetLeagueTeamsAsync(leagueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos de liga {LeagueID}", leagueId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene resumen de liga desde VIEW (versión ligera).
        /// VIEW: vw_LeagueSummary
        /// </summary>
        public async Task<LeagueSummaryVM?> GetLeagueSummaryViewAsync(int leagueId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetLeagueSummaryViewAsync(leagueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen ligero de liga {LeagueID}", leagueId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene ligas donde el usuario es comisionado.
        /// VIEW: vw_UserCommissionedLeagues
        /// </summary>
        public async Task<List<UserCommissionedLeagueVM>> GetUserCommissionedLeaguesAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserCommissionedLeaguesAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ligas como comisionado de usuario {UserID}", userId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene equipos del usuario en todas sus ligas.
        /// VIEW: vw_UserTeams
        /// </summary>
        public async Task<List<UserTeamVM>> GetUserTeamsAsync(int userId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetUserTeamsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos de usuario {UserID}", userId);
                throw;
            }
        }

        #endregion
    }
}