using NFL_Fantasy_API.SharedSystems.Validators;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Models.DTOs.Fantasy;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.Fantasy;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Fantasy;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.Fantasy
{
    /// <summary>
    /// Implementación del servicio de gestión de equipos fantasy.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a TeamDataAccess).
    /// Feature 3.1: Creación y administración de equipos fantasy
    /// </summary>
    public class TeamService : ITeamService
    {
        private readonly TeamDataAccess _dataAccess;
        private readonly ILogger<TeamService> _logger;

        public TeamService(
            TeamDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<TeamService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Update Team Branding

        /// <summary>
        /// Actualiza el branding de un equipo fantasy.
        /// SP: app.sp_UpdateTeamBranding
        /// </summary>
        public async Task<ApiResponseDTO> UpdateTeamBrandingAsync(
            int teamId,
            UpdateTeamBrandingDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a TeamBrandingValidator
                var brandingErrors = TeamBrandingValidator.ValidateTeamBranding(dto);

                if (brandingErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", brandingErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.UpdateTeamBrandingAsync(
                    teamId,
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} updated branding for Team {TeamId}",
                    actorUserId,
                    teamId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al actualizar branding: Actor={ActorUserId}, Team={TeamId}",
                    actorUserId,
                    teamId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar branding: Team={TeamId}",
                    teamId
                );
                return ApiResponseDTO.ErrorResponse($"Error al actualizar branding: {ex.Message}");
            }
        }

        #endregion

        #region Get My Team

        /// <summary>
        /// Obtiene información completa del equipo con roster.
        /// SP: app.sp_GetMyTeam (3 result sets)
        /// </summary>
        public async Task<MyTeamResponseDTO?> GetMyTeamAsync(
            int teamId,
            int actorUserId,
            string? filterPosition = null,
            string? searchPlayer = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetMyTeamAsync(
                    teamId,
                    actorUserId,
                    filterPosition,
                    searchPlayer
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener equipo: Team={TeamId}, Actor={ActorUserId}",
                    teamId,
                    actorUserId
                );
                throw;
            }
        }

        #endregion

        #region Get Team Roster Distribution

        /// <summary>
        /// Obtiene distribución porcentual del roster.
        /// SP: app.sp_GetTeamRosterDistribution
        /// </summary>
        public async Task<List<RosterDistributionItemDTO>> GetTeamRosterDistributionAsync(int teamId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetTeamRosterDistributionAsync(teamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener distribución de roster: Team={TeamId}",
                    teamId
                );
                throw;
            }
        }

        #endregion

        #region Add / Remove Player from Roster

        /// <summary>
        /// Agrega un jugador al roster del equipo.
        /// SP: app.sp_AddPlayerToRoster
        /// </summary>
        public async Task<ApiResponseDTO> AddPlayerToRosterAsync(
            int teamId,
            AddPlayerToRosterDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.AddPlayerToRosterAsync(
                    teamId,
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} added Player {PlayerId} to Team {TeamId}",
                        actorUserId,
                        dto.PlayerID,
                        teamId
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al agregar jugador al roster.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al agregar jugador: Team={TeamId}, Player={PlayerId}",
                    teamId,
                    dto.PlayerID
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al agregar jugador: Team={TeamId}, Player={PlayerId}",
                    teamId,
                    dto.PlayerID
                );
                return ApiResponseDTO.ErrorResponse($"Error al agregar jugador: {ex.Message}");
            }
        }

        /// <summary>
        /// Remueve un jugador del roster.
        /// SP: app.sp_RemovePlayerFromRoster
        /// </summary>
        public async Task<ApiResponseDTO> RemovePlayerFromRosterAsync(
            int rosterId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.RemovePlayerFromRosterAsync(
                    rosterId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} removed Roster entry {RosterId}",
                    actorUserId,
                    rosterId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al remover jugador: Roster={RosterId}",
                    rosterId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al remover jugador: Roster={RosterId}",
                    rosterId
                );
                return ApiResponseDTO.ErrorResponse($"Error al remover jugador: {ex.Message}");
            }
        }

        #endregion

        #region Get Fantasy Team Details from VIEW

        /// <summary>
        /// Obtiene detalles de un equipo fantasy desde VIEW.
        /// VIEW: vw_FantasyTeamDetails
        /// </summary>
        public async Task<FantasyTeamDetailsVM?> GetFantasyTeamDetailsAsync(int teamId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetFantasyTeamDetailsAsync(teamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener detalles de equipo: Team={TeamId}",
                    teamId
                );
                throw;
            }
        }

        #endregion
    }
}