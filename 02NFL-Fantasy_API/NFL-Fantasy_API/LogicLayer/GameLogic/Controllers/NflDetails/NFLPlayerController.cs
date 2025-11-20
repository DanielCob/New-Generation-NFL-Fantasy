using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.NflDetails
{
    /// <summary>
    /// Controller de gestión de jugadores NFL (CRUD).
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para jugadores NFL
    /// - Crear, listar, actualizar jugadores NFL
    /// - Activar/desactivar jugadores
    /// - Consultar detalles completos
    /// - Listar jugadores disponibles para draft/FA
    /// - Gestionar noticias y designaciones de jugadores (Feature 10.3)
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Crear/Actualizar/Activar/Desactivar requieren rol ADMIN
    /// - Agregar/Eliminar noticias requieren rol ADMIN (Feature 10.3)
    /// 
    /// ENDPOINTS PRINCIPALES:
    /// - POST /api/nflplayer - Crear jugador NFL (ADMIN)
    /// - GET /api/nflplayer - Listar jugadores con filtros y paginación
    /// - GET /api/nflplayer/{id} - Detalles completos
    /// - PUT /api/nflplayer/{id} - Actualizar jugador (ADMIN)
    /// - POST /api/nflplayer/{id}/deactivate - Desactivar (ADMIN)
    /// - POST /api/nflplayer/{id}/reactivate - Reactivar (ADMIN)
    /// - GET /api/nflplayer/available - Jugadores disponibles para draft/FA
    /// - GET /api/nflplayer/by-nfl-team/{nflTeamId} - Jugadores de un equipo NFL
    /// - GET /api/nflplayer/active - Jugadores activos para dropdowns
    /// 
    /// ENDPOINTS DE NOTICIAS (Feature 10.3):
    /// - POST /api/nflplayer/news - Agregar noticia de jugador (ADMIN)
    /// - DELETE /api/nflplayer/news/{newsId} - Eliminar noticia (ADMIN)
    /// - GET /api/nflplayer/{playerId}/news - Feed de noticias de jugador
    /// - GET /api/nflplayer/news/{newsId} - Detalles de noticia específica
    /// - GET /api/nflplayer/by-designation - Jugadores por designación (IR, OUT, etc.)
    /// 
    /// Feature: Gestión de Jugadores NFL (CRUD) + Feature 10.3: Estado de Jugador
    /// </summary>
    [ApiController]
    [Route("api/nflplayer")]
    [Authorize]
    public class NFLPlayerController : ControllerBase
    {
        private readonly INFLPlayerService _nflPlayerService;
        private readonly ILogger<NFLPlayerController> _logger;

        public NFLPlayerController(INFLPlayerService nflPlayerService, ILogger<NFLPlayerController> logger)
        {
            _nflPlayerService = nflPlayerService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo jugador NFL manualmente.
        /// POST /api/nflplayer
        /// </summary>
        /// <param name="dto">Datos del jugador a crear</param>
        /// <returns>Datos del jugador creado con su ID</returns>
        /// <response code="201">Jugador creado exitosamente</response>
        /// <response code="400">Datos inválidos o jugador duplicado</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear jugadores NFL.
        /// Validación de unicidad: (FirstName, LastName, NFLTeamID) debe ser único.
        /// Feature: Crear jugador NFL
        /// </remarks>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateNFLPlayer([FromBody] CreateNFLPlayerDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.CreateNFLPlayerAsync(
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo crear el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created NFL player: {PlayerName} from {IP}",
                    actorUserId,
                    $"{dto.FirstName} {dto.LastName}",
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetNFLPlayerDetails),
                    new { id = ((CreateNFLPlayerResponseDTO?)result.Data)?.NFLPlayerID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Crea múltiples jugadores NFL mediante batch.
        /// POST /api/nflplayer/batch
        /// </summary>
        /// <param name="dtos">Lista de jugadores a crear</param>
        /// <returns>Resultados de la creación en batch</returns>
        /// <response code="200">Proceso completado con resumen de éxitos y errores</response>
        /// <response code="400">No se proporcionaron jugadores</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear jugadores NFL.
        /// Procesa cada jugador individualmente y retorna un resumen completo.
        /// Los errores individuales no detienen el proceso completo.
        /// Feature: Crear jugadores NFL en batch
        /// </remarks>
        [HttpPost("batch")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateNFLPlayersBatch([FromBody] List<CreateNFLPlayerDTO> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(
                    "No se proporcionaron jugadores para crear."
                ));
            }

            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var dto in dtos)
            {
                try
                {
                    var result = await _nflPlayerService.CreateNFLPlayerAsync(
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
                        var createdPlayer = (CreateNFLPlayerResponseDTO?)result.Data;
                        results.Add(new
                        {
                            NFLPlayerID = createdPlayer?.NFLPlayerID ?? 0,
                            PlayerName = $"{dto.FirstName} {dto.LastName}",
                            Position = dto.Position,
                            NFLTeamID = dto.NFLTeamID,
                            Success = true
                        });

                        _logger.LogInformation(
                            "User {UserID} created NFL player in batch: {PlayerName}",
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
                "User {UserID} completed batch creation: {SuccessCount} players created, {ErrorCount} errors from {IP}",
                actorUserId,
                results.Count,
                errors.Count,
                sourceIp
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Proceso completado. {results.Count} jugadores creados, {errors.Count} errores.",
                new
                {
                    CreatedPlayers = results,
                    Errors = errors,
                    TotalProcessed = dtos.Count,
                    SuccessCount = results.Count,
                    ErrorCount = errors.Count
                }
            ));
        }

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros.
        /// GET /api/nflplayer
        /// </summary>
        /// <param name="request">Filtros de búsqueda y paginación</param>
        /// <returns>Lista paginada de jugadores NFL</returns>
        /// <response code="200">Jugadores obtenidos exitosamente</response>
        /// <remarks>
        /// FILTROS:
        /// - Búsqueda por nombre (FirstName, LastName, FullName)
        /// - Posición (QB, RB, WR, TE, K, DEF, etc.)
        /// - Equipo NFL
        /// - Estado activo/inactivo
        /// - Paginación: 50 por página (máx 100)
        /// 
        /// Feature: Listar jugadores NFL
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDTO>> ListNFLPlayers([FromQuery] ListNFLPlayersRequestDTO request)
        {
            var result = await _nflPlayerService.ListNFLPlayersAsync(request);
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores NFL."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores NFL obtenidos exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL.
        /// GET /api/nflplayer/{id}
        /// </summary>
        /// <param name="id">ID del jugador NFL</param>
        /// <returns>Información completa del jugador</returns>
        /// <response code="200">Detalles obtenidos exitosamente</response>
        /// <response code="404">Jugador no encontrado</response>
        /// <remarks>
        /// RETORNA:
        /// - Información del jugador
        /// - Historial de cambios (últimos 20)
        /// - Equipos fantasy actuales que tienen este jugador
        /// 
        /// Feature: Ver detalles de jugador NFL
        /// </remarks>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDTO>> GetNFLPlayerDetails(int id)
        {
            var details = await _nflPlayerService.GetNFLPlayerDetailsAsync(id);

            if (details == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Jugador NFL no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Detalles de jugador NFL obtenidos exitosamente.",
                details
            ));
        }

        /// <summary>
        /// Actualiza un jugador NFL existente.
        /// PUT /api/nflplayer/{id}
        /// </summary>
        /// <param name="id">ID del jugador a actualizar</param>
        /// <param name="dto">Datos a actualizar</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Jugador actualizado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Jugador no encontrado</response>
        /// <remarks>
        /// Solo ADMIN puede actualizar jugadores NFL.
        /// Validación de unicidad: (FirstName, LastName, NFLTeamID) debe ser único.
        /// Feature: Modificar jugador NFL
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateNFLPlayer(
            int id,
            [FromBody] UpdateNFLPlayerDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.UpdateNFLPlayerAsync(
                id,
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo actualizar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} updated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Desactiva un jugador NFL.
        /// POST /api/nflplayer/{id}/deactivate
        /// </summary>
        /// <param name="id">ID del jugador a desactivar</param>
        /// <returns>Confirmación de desactivación</returns>
        /// <response code="200">Jugador desactivado exitosamente</response>
        /// <response code="400">No se puede desactivar (está en roster activo)</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Valida que no esté en roster activo de equipos en temporada actual.
        /// Solo ADMIN puede desactivar jugadores NFL.
        /// Feature: Desactivar jugador NFL
        /// </remarks>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> DeactivateNFLPlayer(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.DeactivateNFLPlayerAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo desactivar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} deactivated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Reactiva un jugador NFL desactivado.
        /// POST /api/nflplayer/{id}/reactivate
        /// </summary>
        /// <param name="id">ID del jugador a reactivar</param>
        /// <returns>Confirmación de reactivación</returns>
        /// <response code="200">Jugador reactivado exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede reactivar jugadores NFL.
        /// Feature: Reactivar jugador NFL
        /// </remarks>
        [HttpPost("{id}/reactivate")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> ReactivateNFLPlayer(int id)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.ReactivateNFLPlayerAsync(
                id,
                actorUserId,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo reactivar el jugador NFL."));

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} reactivated NFL player {PlayerID} from {IP}",
                    actorUserId,
                    id,
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lista jugadores disponibles (no están en ningún roster activo).
        /// GET /api/nflplayer/available
        /// </summary>
        /// <param name="position">Filtrar por posición (opcional)</param>
        /// <returns>Lista de jugadores disponibles</returns>
        /// <response code="200">Jugadores disponibles obtenidos exitosamente</response>
        /// <remarks>
        /// Usado para:
        /// - Draft de jugadores
        /// - Free agency
        /// - Waiver wire
        /// 
        /// Solo retorna jugadores que NO están en ningún roster activo.
        /// VIEW: vw_AvailablePlayers
        /// </remarks>
        [HttpGet("available")]
        public async Task<ActionResult<ApiResponseDTO>> GetAvailablePlayers(
            [FromQuery] string? position = null)
        {
            var players = await _nflPlayerService.GetAvailablePlayersAsync(position);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores disponibles."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores disponibles obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// GET /api/nflplayer/by-nfl-team/{nflTeamId}
        /// </summary>
        /// <param name="nflTeamId">ID del equipo NFL</param>
        /// <returns>Lista de jugadores del equipo</returns>
        /// <response code="200">Jugadores del equipo obtenidos exitosamente</response>
        /// <remarks>
        /// Retorna todos los jugadores activos del equipo NFL especificado.
        /// VIEW: vw_PlayersByNFLTeam
        /// </remarks>
        [HttpGet("by-nfl-team/{nflTeamId}")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayersByNFLTeam(int nflTeamId)
        {
            var players = await _nflPlayerService.GetPlayersByNFLTeamAsync(nflTeamId);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores del equipo NFL."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores del equipo obtenidos exitosamente.",
                players
            ));
        }

        /// <summary>
        /// Obtiene jugadores NFL activos (para dropdowns).
        /// GET /api/nflplayer/active
        /// </summary>
        /// <param name="position">Filtrar por posición (opcional)</param>
        /// <returns>Lista de jugadores activos</returns>
        /// <response code="200">Jugadores activos obtenidos exitosamente</response>
        /// <remarks>
        /// Usado para selectores/dropdowns en la UI.
        /// VIEW: vw_ActiveNFLPlayers
        /// </remarks>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponseDTO>> GetActiveNFLPlayers(
            [FromQuery] string? position = null)
        {
            var players = await _nflPlayerService.GetActiveNFLPlayersAsync(position);
            if (players is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores NFL activos."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Jugadores NFL activos obtenidos exitosamente.",
                players
            ));
        }

        #region Batch Reports

        /// <summary>
        /// Crea un reporte de importación batch de jugadores NFL.
        /// POST /api/nflplayer/batch-report
        /// </summary>
        /// <param name="dto">Datos del reporte a crear</param>
        /// <returns>Datos del reporte creado con su ID</returns>
        /// <response code="201">Reporte creado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede crear reportes de batch.
        /// Se llama después de completar un proceso de importación batch.
        /// Feature: Reportes de importación batch
        /// </remarks>
        [HttpPost("batch-report")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> CreateBatchReport([FromBody] CreateNFLPlayerBatchReportDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.CreateBatchReportAsync(
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo crear el reporte de batch."));
            }

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} created batch report {BatchReportID}: {SuccessCount} successes, {ErrorCount} errors from {IP}",
                    actorUserId,
                    ((CreateNFLPlayerBatchReportResponseDTO?)result.Data)?.BatchReportID ?? 0,
                    dto.SuccessCount,
                    dto.ErrorCount,
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetBatchReportById),
                    new { id = ((CreateNFLPlayerBatchReportResponseDTO?)result.Data)?.BatchReportID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Lista todos los reportes de batch con paginación.
        /// GET /api/nflplayer/batch-reports
        /// </summary>
        /// <param name="request">Parámetros de paginación y ordenamiento</param>
        /// <returns>Lista paginada de reportes</returns>
        /// <response code="200">Reportes obtenidos exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede ver reportes de batch.
        /// Paginación: 50 por página (máx 100)
        /// Ordenamiento por: BatchReportID, CreatedAt, TotalProcessed, SuccessCount, ErrorCount
        /// Feature: Listar reportes de batch
        /// </remarks>
        [HttpGet("batch-reports")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> GetAllBatchReports([FromQuery] ListNFLPlayerBatchReportsRequestDTO request)
        {
            var actorUserId = this.UserId();

            var result = await _nflPlayerService.GetAllBatchReportsAsync(request, actorUserId);

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los reportes de batch."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Reportes de batch obtenidos exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene un reporte de batch específico por ID.
        /// GET /api/nflplayer/batch-report/{id}
        /// </summary>
        /// <param name="id">ID del reporte</param>
        /// <returns>Detalles completos del reporte</returns>
        /// <response code="200">Reporte obtenido exitosamente</response>
        /// <response code="404">Reporte no encontrado</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo ADMIN puede ver reportes de batch.
        /// Feature: Ver detalles de reporte de batch
        /// </remarks>
        [HttpGet("batch-report/{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> GetBatchReportById(int id)
        {
            var actorUserId = this.UserId();

            var details = await _nflPlayerService.GetBatchReportByIdAsync(id, actorUserId);

            if (details == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Reporte de batch no encontrado."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Reporte de batch obtenido exitosamente.",
                details
            ));
        }

        #endregion

        #region Player News (Feature 10.3)

        /// <summary>
        /// Agrega una noticia a un jugador NFL.
        /// POST /api/nflplayer/news
        /// </summary>
        /// <param name="dto">Datos de la noticia a agregar</param>
        /// <returns>Confirmación de creación con ID de la noticia</returns>
        /// <response code="201">Noticia agregada exitosamente</response>
        /// <response code="400">Datos inválidos o validación fallida</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Jugador no encontrado o inactivo</response>
        /// <remarks>
        /// Solo ADMIN puede agregar noticias.
        /// 
        /// NOTICIAS DE LESIÓN:
        /// - Requieren InjurySummary (máx 30 caracteres)
        /// - Requieren Designation: O, D, Q, P, FP, IR, PUP, SUS
        /// - Actualizan automáticamente CurrentDesignation del jugador
        /// 
        /// NOTICIAS REGULARES:
        /// - InjurySummary y Designation deben ser NULL
        /// - No modifican CurrentDesignation
        /// 
        /// DESIGNACIONES:
        /// - O (OUT): No jugará
        /// - D (DOUBTFUL): Muy poco probable (~25%)
        /// - Q (QUESTIONABLE): Probabilidad ~50%
        /// - P (PROBABLE): Casi seguro que juega
        /// - FP (FULL PRACTICE): Participación completa en práctica
        /// - IR (INJURED RESERVE): Fuera por periodo extendido
        /// - PUP (PHYSICALLY UNABLE): No habilitado hasta cumplir requisitos
        /// - SUS (SUSPENDED): Suspendido por sanción
        /// 
        /// Feature 10.3 - US 1: Agregar noticia de jugador
        /// </remarks>
        [HttpPost("news")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> AddPlayerNews([FromBody] AddNFLPlayerNewsDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.AddPlayerNewsAsync(
                dto,
                actorUserId,
                sourceIp,
                userAgent
            );

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo agregar la noticia."));
            }

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} added news to player {PlayerID}: IsInjury={IsInjury}, Designation={Designation} from {IP}",
                    actorUserId,
                    dto.NFLPlayerID,
                    dto.IsInjury,
                    dto.Designation,
                    sourceIp
                );

                return CreatedAtAction(
                    nameof(GetPlayerNewsById),
                    new { newsId = ((AddNFLPlayerNewsResponseDTO?)result.Data)?.NewsID ?? 0 },
                    result
                );
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Elimina una noticia de jugador y revierte su designación.
        /// DELETE /api/nflplayer/news/{newsId}
        /// </summary>
        /// <param name="newsId">ID de la noticia a eliminar</param>
        /// <returns>Confirmación de eliminación con designación revertida</returns>
        /// <response code="200">Noticia eliminada exitosamente</response>
        /// <response code="400">Error al eliminar</response>
        /// <response code="403">No eres ADMIN</response>
        /// <response code="404">Noticia no encontrada o ya eliminada</response>
        /// <remarks>
        /// Solo ADMIN puede eliminar noticias.
        /// 
        /// REVERSIÓN DE DESIGNACIÓN:
        /// - Si la noticia eliminada tenía designación, el sistema busca la designación previa
        /// - Busca en el historial de noticias (no eliminadas) la designación más reciente anterior
        /// - Si no hay designación previa, CurrentDesignation queda en NULL
        /// - El cambio se registra en NFLPlayerChangeLog
        /// 
        /// Feature 10.3 - US 2: Eliminar noticia de jugador y revertir designación
        /// </remarks>
        [HttpDelete("news/{newsId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> DeletePlayerNews(long newsId)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _nflPlayerService.DeletePlayerNewsAsync(
                newsId,
                actorUserId,
                sourceIp,
                userAgent
            );

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo eliminar la noticia."));
            }

            if (result.Success)
            {
                var responseData = result.Data as DeleteNFLPlayerNewsResponseDTO;

                _logger.LogInformation(
                    "User {UserID} deleted news {NewsID}, reverted designation to: {RevertedDesignation} from {IP}",
                    actorUserId,
                    newsId,
                    responseData?.RevertedDesignation ?? "NULL",
                    sourceIp
                );
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene el feed de noticias de un jugador específico.
        /// GET /api/nflplayer/{playerId}/news
        /// </summary>
        /// <param name="playerId">ID del jugador NFL</param>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 20, máx: 50)</param>
        /// <returns>Lista paginada de noticias del jugador</returns>
        /// <response code="200">Noticias obtenidas exitosamente</response>
        /// <response code="404">Jugador no encontrado</response>
        /// <remarks>
        /// RETORNA:
        /// - Noticias en orden cronológico inverso (más recientes primero)
        /// - Solo noticias activas (IsDeleted = false)
        /// - Información del autor de cada noticia
        /// - Paginación: 20 por página (máx 50)
        /// 
        /// Accesible por cualquier usuario autenticado.
        /// 
        /// Feature 10.3: Listar noticias de jugador
        /// </remarks>
        [HttpGet("{playerId}/news")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayerNewsFeed(
            int playerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var request = new GetNFLPlayerNewsFeedRequestDTO
            {
                NFLPlayerID = playerId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _nflPlayerService.GetPlayerNewsFeedAsync(request);

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener las noticias del jugador."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Noticias obtenidas exitosamente.",
                result
            ));
        }

        /// <summary>
        /// Obtiene detalles completos de una noticia específica.
        /// GET /api/nflplayer/news/{newsId}
        /// </summary>
        /// <param name="newsId">ID de la noticia</param>
        /// <returns>Detalles completos de la noticia</returns>
        /// <response code="200">Noticia obtenida exitosamente</response>
        /// <response code="404">Noticia no encontrada</response>
        /// <remarks>
        /// RETORNA:
        /// - Información completa de la noticia
        /// - Datos del jugador asociado
        /// - Información de creación y eliminación (si aplica)
        /// 
        /// Accesible por cualquier usuario autenticado.
        /// 
        /// Feature 10.3: Ver detalles de noticia
        /// </remarks>
        [HttpGet("news/{newsId}")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayerNewsById(long newsId)
        {
            var details = await _nflPlayerService.GetPlayerNewsByIdAsync(newsId);

            if (details == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse(
                    "Noticia no encontrada."
                ));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Noticia obtenida exitosamente.",
                details
            ));
        }

        /// <summary>
        /// Lista jugadores filtrados por designación (IR, OUT, etc.).
        /// GET /api/nflplayer/by-designation
        /// </summary>
        /// <param name="designation">Designación a filtrar (O, D, Q, P, FP, IR, PUP, SUS)</param>
        /// <param name="nflTeamId">Filtrar por equipo NFL (opcional)</param>
        /// <param name="position">Filtrar por posición (opcional)</param>
        /// <returns>Lista de jugadores con la designación especificada</returns>
        /// <response code="200">Jugadores obtenidos exitosamente</response>
        /// <response code="400">Designación inválida</response>
        /// <remarks>
        /// DESIGNACIONES VÁLIDAS:
        /// - O (OUT): Jugadores que no jugarán
        /// - D (DOUBTFUL): Muy poco probable que jueguen
        /// - Q (QUESTIONABLE): Cuestionables para jugar
        /// - P (PROBABLE): Probables para jugar
        /// - FP (FULL PRACTICE): Participación completa
        /// - IR (INJURED RESERVE): En reserva de lesionados
        /// - PUP (PHYSICALLY UNABLE): Incapaces físicamente
        /// - SUS (SUSPENDED): Suspendidos
        /// 
        /// USOS:
        /// - Validaciones de lineups (jugadores en IR no pueden jugar)
        /// - Reportes de lesiones por equipo
        /// - Análisis de disponibilidad
        /// 
        /// Accesible por cualquier usuario autenticado.
        /// 
        /// Feature 10.3: Listar jugadores por estado
        /// </remarks>
        [HttpGet("by-designation")]
        public async Task<ActionResult<ApiResponseDTO>> GetPlayersByDesignation(
            [FromQuery] string designation,
            [FromQuery] int? nflTeamId = null,
            [FromQuery] string? position = null)
        {
            var request = new GetPlayersByDesignationRequestDTO
            {
                Designation = designation,
                NFLTeamID = nflTeamId,
                Position = position
            };

            var result = await _nflPlayerService.GetPlayersByDesignationAsync(request);

            if (result is null)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los jugadores por designación."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Jugadores con designación '{designation}' obtenidos exitosamente.",
                result
            ));
        }

        #endregion
    }
}