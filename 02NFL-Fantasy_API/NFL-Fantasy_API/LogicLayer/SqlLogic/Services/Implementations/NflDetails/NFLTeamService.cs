using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.SharedSystems.Validators;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.NflDetails
{
    /// <summary>
    /// Implementación del servicio de gestión de equipos NFL.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a NFLTeamDataAccess).
    /// Feature 10.1: Gestión de Equipos NFL (CRUD)
    /// </summary>
    public class NFLTeamService : INFLTeamService
    {
        private readonly NFLTeamDataAccess _dataAccess;
        private readonly ILogger<NFLTeamService> _logger;

        public NFLTeamService(
            NFLTeamDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<NFLTeamService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Create NFL Team

        /// <summary>
        /// Crea un nuevo equipo NFL.
        /// SP: app.sp_CreateNFLTeam
        /// </summary>
        public async Task<ApiResponseDTO> CreateNFLTeamAsync(
            CreateNFLTeamDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Reutilizamos TeamBrandingValidator
                var imageErrors = TeamBrandingValidator.ValidateTeamImage(
                    dto.TeamImageWidth,
                    dto.TeamImageHeight,
                    dto.TeamImageBytes
                );

                var thumbErrors = TeamBrandingValidator.ValidateThumbnail(
                    dto.ThumbnailWidth,
                    dto.ThumbnailHeight,
                    dto.ThumbnailBytes
                );

                var allErrors = imageErrors.Concat(thumbErrors).ToList();

                if (allErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", allErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.CreateNFLTeamAsync(
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} created NFL Team {NFLTeamID} - {TeamName}",
                        actorUserId,
                        result.NFLTeamID,
                        result.TeamName
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear equipo NFL.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al crear equipo NFL: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear equipo NFL: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region List NFL Teams

        /// <summary>
        /// Lista equipos NFL con paginación y filtros.
        /// SP: app.sp_ListNFLTeams
        /// </summary>
        public async Task<ListNFLTeamsResponseDTO> ListNFLTeamsAsync(ListNFLTeamsRequestDTO request)
        {
            try
            {
                // VALIDACIÓN: Delegada a PaginationValidator
                var (adjustedPageNumber, adjustedPageSize, paginationErrors) =
                    PaginationValidator.ValidateAndAdjustPagination(
                        request.PageNumber,
                        request.PageSize
                    );

                if (paginationErrors.Any())
                {
                    _logger.LogWarning(
                        "Parámetros de paginación ajustados: {Errors}",
                        string.Join(", ", paginationErrors)
                    );

                    // Ajustar los valores en el request
                    request.PageNumber = adjustedPageNumber;
                    request.PageSize = adjustedPageSize;
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.ListNFLTeamsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar equipos NFL");
                throw;
            }
        }

        #endregion

        #region Get NFL Team Details

        /// <summary>
        /// Obtiene detalles completos de un equipo NFL.
        /// SP: app.sp_GetNFLTeamDetails (3 result sets)
        /// </summary>
        public async Task<NFLTeamDetailsDTO?> GetNFLTeamDetailsAsync(int nflTeamId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetNFLTeamDetailsAsync(nflTeamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener detalles de equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                throw;
            }
        }

        #endregion

        #region Update NFL Team

        /// <summary>
        /// Actualiza un equipo NFL existente.
        /// SP: app.sp_UpdateNFLTeam
        /// </summary>
        public async Task<ApiResponseDTO> UpdateNFLTeamAsync(
            int nflTeamId,
            UpdateNFLTeamDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Reutilizamos TeamBrandingValidator si hay imágenes
                var imageErrors = TeamBrandingValidator.ValidateTeamImage(
                    dto.TeamImageWidth,
                    dto.TeamImageHeight,
                    dto.TeamImageBytes
                );

                var thumbErrors = TeamBrandingValidator.ValidateThumbnail(
                    dto.ThumbnailWidth,
                    dto.ThumbnailHeight,
                    dto.ThumbnailBytes
                );

                var allErrors = imageErrors.Concat(thumbErrors).ToList();

                if (allErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", allErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.UpdateNFLTeamAsync(
                    nflTeamId,
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} updated NFL Team {NFLTeamId}",
                    actorUserId,
                    nflTeamId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al actualizar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse($"Error al actualizar equipo NFL: {ex.Message}");
            }
        }

        #endregion

        #region Deactivate / Reactivate

        /// <summary>
        /// Desactiva un equipo NFL.
        /// SP: app.sp_DeactivateNFLTeam
        /// Requiere autorización: AdminOnly (verificar en controller)
        /// </summary>
        public async Task<ApiResponseDTO> DeactivateNFLTeamAsync(
            int nflTeamId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.DeactivateNFLTeamAsync(
                    nflTeamId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} deactivated NFL Team {NFLTeamId}",
                    actorUserId,
                    nflTeamId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al desactivar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al desactivar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse($"Error al desactivar equipo NFL: {ex.Message}");
            }
        }

        /// <summary>
        /// Reactiva un equipo NFL desactivado.
        /// SP: app.sp_ReactivateNFLTeam
        /// Requiere autorización: AdminOnly (verificar en controller)
        /// </summary>
        public async Task<ApiResponseDTO> ReactivateNFLTeamAsync(
            int nflTeamId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.ReactivateNFLTeamAsync(
                    nflTeamId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} reactivated NFL Team {NFLTeamId}",
                    actorUserId,
                    nflTeamId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al reactivar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al reactivar equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                return ApiResponseDTO.ErrorResponse($"Error al reactivar equipo NFL: {ex.Message}");
            }
        }

        #endregion

        #region Get Active NFL Teams

        /// <summary>
        /// Obtiene equipos NFL activos (para dropdowns).
        /// VIEW: vw_ActiveNFLTeams
        /// </summary>
        public async Task<List<NFLTeamBasicVM>> GetActiveNFLTeamsAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetActiveNFLTeamsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos NFL activos");
                throw;
            }
        }

        #endregion
    }
}