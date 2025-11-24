using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Validators;
using NFL_Fantasy_API.SharedSystems.Validators.Images;
using NFL_Fantasy_API.SharedSystems.Validators.NflDetails;

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


        /// <summary>
        /// Crea múltiples jugadores NFL mediante batch.
        /// Reutiliza CreateNFLPlayerAsync para cada jugador y agrega logging y resumen.
        /// </summary>
        public async Task<ApiResponseDTO> CreateNFLPlayersBatchAsync(
            List<CreateNFLPlayerDTO> dtos,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron jugadores para crear."
                );
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var dto in dtos)
            {
                try
                {
                    var result = await CreateNFLPlayerAsync(
                        dto,
                        actorUserId,
                        sourceIp,
                        userAgent
                    );

                    if (result is null)
                    {
                        errors.Add($"{dto.FirstName} {dto.LastName}: No se pudo crear el jugador NFL.");
                        continue;
                    }

                    if (result.Success)
                    {
                        var createdPlayer = result.Data as CreateNFLPlayerResponseDTO;

                        results.Add(new
                        {
                            NFLPlayerID = createdPlayer?.NFLPlayerID ?? 0,
                            PlayerName = $"{dto.FirstName} {dto.LastName}",
                            Position = dto.Position,
                            NFLTeamID = dto.NFLTeamID,
                            Success = true
                        });

                        _logger.LogInformation(
                            "User {ActorUserId} created NFL player in batch: {PlayerName}",
                            actorUserId,
                            $"{dto.FirstName} {dto.LastName}"
                        );
                    }
                    else
                    {
                        errors.Add($"{dto.FirstName} {dto.LastName}: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{dto.FirstName} {dto.LastName}: {ex.Message}");

                    _logger.LogError(
                        ex,
                        "Error creating NFL player in batch: {PlayerName}",
                        $"{dto.FirstName} {dto.LastName}"
                    );
                }
            }

            _logger.LogInformation(
                "User {ActorUserId} completed batch creation: {SuccessCount} players created, {ErrorCount} errors from {IP}",
                actorUserId,
                results.Count,
                errors.Count,
                sourceIp
            );

            var summary = new
            {
                CreatedPlayers = results,
                Errors = errors,
                TotalProcessed = dtos.Count,
                SuccessCount = results.Count,
                ErrorCount = errors.Count
            };

            // Siempre devolvemos SuccessResponse a nivel de servicio (el proceso como tal se completó),
            // el controller se encarga solo de mapear a HTTP.
            return ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} jugadores creados, {errors.Count} errores.",
                summary
            );
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

        #region Batch Reports

        /// <summary>
        /// Crea un reporte de importación batch de jugadores NFL.
        /// SP: app.sp_CreateNFLPlayerBatchReport
        /// </summary>
        public async Task<ApiResponseDTO> CreateBatchReportAsync(
            CreateNFLPlayerBatchReportDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: TotalProcessed debe ser igual a SuccessCount + ErrorCount
                if (dto.TotalProcessed != (dto.SuccessCount + dto.ErrorCount))
                {
                    return ApiResponseDTO.ErrorResponse(
                        "Total procesado debe ser igual a la suma de éxitos y errores."
                    );
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.CreateBatchReportAsync(
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} created batch report {BatchReportID}: {SuccessCount}/{TotalProcessed}",
                        actorUserId,
                        result.BatchReportID,
                        result.SuccessCount,
                        result.TotalProcessed
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al crear reporte de batch.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al crear reporte de batch: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear reporte de batch: Actor={ActorUserId}",
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Lista todos los reportes de batch con paginación.
        /// SP: app.sp_GetAllNFLPlayerBatchReports
        /// </summary>
        public async Task<ListNFLPlayerBatchReportsResponseDTO> GetAllBatchReportsAsync(
            ListNFLPlayerBatchReportsRequestDTO request,
            int actorUserId)
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

                    request.PageNumber = adjustedPageNumber;
                    request.PageSize = adjustedPageSize;
                }

                // Validar OrderBy
                var validOrderByFields = new[] { "BatchReportID", "CreatedAt", "TotalProcessed", "SuccessCount", "ErrorCount" };
                if (!validOrderByFields.Contains(request.OrderBy))
                {
                    request.OrderBy = "CreatedAt";
                }

                // Validar SortDirection
                if (request.SortDirection.ToUpper() != "ASC" && request.SortDirection.ToUpper() != "DESC")
                {
                    request.SortDirection = "DESC";
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetAllBatchReportsAsync(request, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar reportes de batch: Actor={ActorUserId}",
                    actorUserId
                );
                throw;
            }
        }

        /// <summary>
        /// Obtiene un reporte de batch específico por ID.
        /// SP: app.sp_GetNFLPlayerBatchReportById
        /// </summary>
        public async Task<NFLPlayerBatchReportDetailsDTO?> GetBatchReportByIdAsync(
            int batchReportId,
            int actorUserId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetBatchReportByIdAsync(batchReportId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener reporte de batch {BatchReportId}: Actor={ActorUserId}",
                    batchReportId,
                    actorUserId
                );
                throw;
            }
        }

        #endregion

        #region Player News (Feature 10.3)

        /// <summary>
        /// Agrega una noticia a un jugador NFL.
        /// SP: app.sp_AddNFLPlayerNews
        /// </summary>
        public async Task<ApiResponseDTO> AddPlayerNewsAsync(
            AddNFLPlayerNewsDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Usar validador centralizado
                var validationErrors = NFLPlayerNewsValidator.ValidateAddNews(dto);

                if (validationErrors.Any())
                {
                    return ApiResponseDTO.ErrorResponse(string.Join(" ", validationErrors));
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.AddPlayerNewsAsync(
                    dto,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} added news {NewsID} to player {NFLPlayerID} - IsInjury={IsInjury}, Designation={Designation}",
                        actorUserId,
                        result.NewsID,
                        dto.NFLPlayerID,
                        dto.IsInjury,
                        dto.Designation ?? "N/A"
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al agregar noticia de jugador.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al agregar noticia: Actor={ActorUserId}, Player={NFLPlayerID}",
                    actorUserId,
                    dto.NFLPlayerID
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al agregar noticia: Actor={ActorUserId}, Player={NFLPlayerID}",
                    actorUserId,
                    dto.NFLPlayerID
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina una noticia de jugador y revierte su designación.
        /// SP: app.sp_DeleteNFLPlayerNews
        /// </summary>
        public async Task<ApiResponseDTO> DeletePlayerNewsAsync(
            long newsId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: NewsID debe ser mayor a 0
                if (newsId <= 0)
                {
                    return ApiResponseDTO.ErrorResponse("ID de noticia inválido.");
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.DeletePlayerNewsAsync(
                    newsId,
                    actorUserId,
                    sourceIp,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} deleted news {NewsID} - Reverted to designation: {RevertedDesignation}",
                        actorUserId,
                        newsId,
                        result.RevertedDesignation ?? "NULL"
                    );

                    return ApiResponseDTO.SuccessResponse(result.Message, result);
                }

                return ApiResponseDTO.ErrorResponse("Error al eliminar noticia de jugador.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al eliminar noticia {NewsID}: Actor={ActorUserId}",
                    newsId,
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar noticia {NewsID}: Actor={ActorUserId}",
                    newsId,
                    actorUserId
                );
                return ApiResponseDTO.ErrorResponse($"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el feed de noticias de un jugador específico.
        /// SP: app.sp_GetNFLPlayerNewsFeed
        /// </summary>
        public async Task<GetNFLPlayerNewsFeedResponseDTO> GetPlayerNewsFeedAsync(
            GetNFLPlayerNewsFeedRequestDTO request)
        {
            try
            {
                // VALIDACIÓN: Delegada a PaginationValidator
                var (adjustedPageNumber, adjustedPageSize, paginationErrors) =
                    PaginationValidator.ValidateAndAdjustPagination(
                        request.PageNumber,
                        request.PageSize
                    );

                // Ajuste adicional: El feed de noticias tiene límite de 50
                if (adjustedPageSize > 50)
                {
                    adjustedPageSize = 50;
                    paginationErrors.Add("El tamaño de página para noticias no puede exceder 50.");
                }

                if (paginationErrors.Any())
                {
                    _logger.LogWarning(
                        "Parámetros de paginación ajustados en feed de noticias: {Errors}",
                        string.Join(", ", paginationErrors)
                    );

                    request.PageNumber = adjustedPageNumber;
                    request.PageSize = adjustedPageSize;
                }

                // Validar NFLPlayerID
                if (request.NFLPlayerID <= 0)
                {
                    _logger.LogWarning(
                        "Intento de obtener feed con NFLPlayerID inválido: {NFLPlayerID}",
                        request.NFLPlayerID
                    );

                    return new GetNFLPlayerNewsFeedResponseDTO
                    {
                        News = new List<NFLPlayerNewsItemDTO>(),
                        TotalRecords = 0,
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = 0
                    };
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayerNewsFeedAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener feed de noticias del jugador {NFLPlayerID}",
                    request.NFLPlayerID
                );
                throw;
            }
        }

        /// <summary>
        /// Obtiene detalles completos de una noticia específica por ID.
        /// SP: app.sp_GetNFLPlayerNewsByID
        /// </summary>
        public async Task<NFLPlayerNewsDetailsDTO?> GetPlayerNewsByIdAsync(long newsId)
        {
            try
            {
                // VALIDACIÓN: NewsID debe ser mayor a 0
                if (newsId <= 0)
                {
                    _logger.LogWarning(
                        "Intento de obtener noticia con ID inválido: {NewsID}",
                        newsId
                    );
                    return null;
                }

                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetPlayerNewsByIdAsync(newsId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener detalles de noticia {NewsID}",
                    newsId
                );
                throw;
            }
        }

        /// <summary>
        /// Lista jugadores filtrados por designación (IR, OUT, etc.).
        /// SP: app.sp_GetPlayersByDesignation
        /// </summary>
        public async Task<List<PlayerWithDesignationDTO>> GetPlayersByDesignationAsync(
            GetPlayersByDesignationRequestDTO request)
        {
            try
            {
                // VALIDACIÓN: Verificar que la designación es válida
                if (!NFLPlayerNewsValidator.IsValidDesignation(request.Designation))
                {
                    _logger.LogWarning(
                        "Intento de buscar jugadores con designación inválida: {Designation}",
                        request.Designation
                    );

                    return new List<PlayerWithDesignationDTO>();
                }

                // Normalizar designación a mayúsculas
                request.Designation = request.Designation.ToUpper();

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.GetPlayersByDesignationAsync(request);

                _logger.LogInformation(
                    "Retrieved {Count} players with designation {Designation}",
                    result.Count,
                    request.Designation
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener jugadores por designación: Designation={Designation}, NFLTeamID={NFLTeamID}, Position={Position}",
                    request.Designation,
                    request.NFLTeamID,
                    request.Position
                );
                throw;
            }
        }

        #endregion
    }
}