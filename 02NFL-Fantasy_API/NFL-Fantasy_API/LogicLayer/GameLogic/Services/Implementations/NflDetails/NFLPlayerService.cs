using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Validators;
using NFL_Fantasy_API.SharedSystems.Validators.Images;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Implementations.NflDetails
{
    /// <summary>
    /// Implementación del servicio de gestión de jugadores NFL.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a NFLPlayerDataAccess).
    /// Feature: Gestión de Jugadores NFL (CRUD)
    /// </summary>
    public class NFLPlayerService : INFLPlayerService
    {
        private readonly NFLPlayerDataAccess _dataAccess;
        private readonly ILogger<NFLPlayerService> _logger;

        public NFLPlayerService(
            NFLPlayerDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<NFLPlayerService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Create NFL Player

        /// <summary>
        /// Crea un nuevo jugador NFL.
        /// SP: app.sp_CreateNFLPlayer
        /// </summary>
        public async Task<ApiResponseDTO> CreateNFLPlayerAsync(
            CreateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Reutilizamos TeamBrandingValidator para imágenes
                var imageErrors = TeamBrandingValidator.ValidateTeamImage(
                    dto.PhotoWidth,
                    dto.PhotoHeight,
                    dto.PhotoBytes
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
                var result = await _dataAccess.CreateNFLPlayerAsync(
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} created NFL Player {NFLPlayerID} - {PlayerName}",
                        actorUserId,
                        result.NFLPlayerID,
                        result.FullName
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear jugador NFL.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al crear jugador NFL: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear jugador NFL: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region List NFL Players

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros.
        /// SP: app.sp_ListNFLPlayers
        /// </summary>
        public async Task<ListNFLPlayersResponseDTO> ListNFLPlayersAsync(ListNFLPlayersRequestDTO request)
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
                return await _dataAccess.ListNFLPlayersAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar jugadores NFL");
                throw;
            }
        }

        #endregion

        #region Get NFL Player Details

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL.
        /// SP: app.sp_GetNFLPlayerDetails (3 result sets)
        /// </summary>
        public async Task<NFLPlayerDetailsDTO?> GetNFLPlayerDetailsAsync(int nflPlayerId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetNFLPlayerDetailsAsync(nflPlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener detalles de jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                throw;
            }
        }

        #endregion

        #region Update NFL Player

        /// <summary>
        /// Actualiza un jugador NFL existente.
        /// SP: app.sp_UpdateNFLPlayer
        /// </summary>
        public async Task<ApiResponseDTO> UpdateNFLPlayerAsync(
            int nflPlayerId,
            UpdateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Reutilizamos TeamBrandingValidator si hay imágenes
                var imageErrors = TeamBrandingValidator.ValidateTeamImage(
                    dto.PhotoWidth,
                    dto.PhotoHeight,
                    dto.PhotoBytes
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
                var message = await _dataAccess.UpdateNFLPlayerAsync(
                    nflPlayerId,
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} updated NFL Player {NFLPlayerId}",
                    actorUserId,
                    nflPlayerId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al actualizar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse($"Error al actualizar jugador NFL: {ex.Message}");
            }
        }

        #endregion

        #region Deactivate / Reactivate

        /// <summary>
        /// Desactiva un jugador NFL.
        /// SP: app.sp_DeactivateNFLPlayer
        /// Requiere autorización: AdminOnly (verificar en controller)
        /// </summary>
        public async Task<ApiResponseDTO> DeactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.DeactivateNFLPlayerAsync(
                    nflPlayerId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} deactivated NFL Player {NFLPlayerId}",
                    actorUserId,
                    nflPlayerId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al desactivar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al desactivar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse($"Error al desactivar jugador NFL: {ex.Message}");
            }
        }

        /// <summary>
        /// Reactiva un jugador NFL desactivado.
        /// SP: app.sp_ReactivateNFLPlayer
        /// Requiere autorización: AdminOnly (verificar en controller)
        /// </summary>
        public async Task<ApiResponseDTO> ReactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.ReactivateNFLPlayerAsync(
                    nflPlayerId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} reactivated NFL Player {NFLPlayerId}",
                    actorUserId,
                    nflPlayerId
                );

                return ApiResponseDTO.SuccessResponse(message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al reactivar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al reactivar jugador NFL {NFLPlayerId}",
                    nflPlayerId
                );
                return ApiResponseDTO.ErrorResponse($"Error al reactivar jugador NFL: {ex.Message}");
            }
        }

        #endregion

        #region Available Players

        /// <summary>
        /// Lista jugadores disponibles (no en ningún roster activo).
        /// VIEW: vw_AvailablePlayers
        /// </summary>
        public async Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetAvailablePlayersAsync(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores disponibles: Position={Position}",
                    position
                );
                throw;
            }
        }

        #endregion

        #region Players by NFL Team

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// VIEW: vw_PlayersByNFLTeam
        /// </summary>
        public async Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayersByNFLTeamAsync(nflTeamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores de equipo NFL {NFLTeamId}",
                    nflTeamId
                );
                throw;
            }
        }

        #endregion

        #region Get Active NFL Players

        /// <summary>
        /// Obtiene jugadores NFL activos (para dropdowns).
        /// VIEW: vw_ActiveNFLPlayers
        /// </summary>
        public async Task<List<PlayerBasicDTO>> GetActiveNFLPlayersAsync(string? position = null)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetActiveNFLPlayersAsync(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores NFL activos: Position={Position}",
                    position
                );
                throw;
            }
        }

        #endregion

        #region Get Player by ID

        /// <summary>
        /// Obtiene un jugador específico por ID.
        /// VIEW: vw_Players con WHERE
        /// </summary>
        public async Task<PlayerBasicDTO?> GetPlayerByIdAsync(int nflPlayerId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayerByIdAsync(nflPlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugador {NFLPlayerId}",
                    nflPlayerId
                );
                throw;
            }
        }

        #endregion
    }
}