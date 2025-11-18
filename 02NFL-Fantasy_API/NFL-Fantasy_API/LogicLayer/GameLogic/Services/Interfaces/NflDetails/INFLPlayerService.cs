using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.NflDetails
{
    /// <summary>
    /// Servicio de gestión de jugadores NFL
    /// Feature: Gestión de Jugadores NFL (CRUD)
    /// Feature 10.3: Estado de Jugador
    /// 
    /// STORED PROCEDURES:
    /// - sp_CreateNFLPlayer, sp_UpdateNFLPlayer, sp_DeactivateNFLPlayer
    /// - sp_ReactivateNFLPlayer, sp_ListNFLPlayers, sp_GetNFLPlayerDetails
    /// - sp_CreateNFLPlayerBatchReport, sp_GetAllNFLPlayerBatchReports
    /// - sp_GetNFLPlayerBatchReportById
    /// - sp_AddNFLPlayerNews (Feature 10.3)
    /// - sp_DeleteNFLPlayerNews (Feature 10.3)
    /// - sp_GetNFLPlayerNewsFeed (Feature 10.3)
    /// - sp_GetNFLPlayerNewsByID (Feature 10.3)
    /// - sp_GetPlayersByDesignation (Feature 10.3)
    /// 
    /// VIEWS:
    /// - vw_AvailablePlayers, vw_PlayersByNFLTeam, vw_ActiveNFLPlayers
    /// - vw_Players (con filtros)
    /// 
    /// RESPONSABILIDADES:
    /// - Gestión completa de jugadores NFL (CRUD)
    /// - Gestión de batch reports de importación
    /// - Gestión de noticias y designaciones de jugadores (Feature 10.3)
    /// - Validación de lógica de negocio
    /// - Coordinación con capa de acceso a datos
    /// </summary>
    public interface INFLPlayerService
    {
        /// <summary>
        /// Crea un nuevo jugador NFL
        /// SP: app.sp_CreateNFLPlayer
        /// Feature: Crear jugador NFL
        /// </summary>
        Task<ApiResponseDTO> CreateNFLPlayerAsync(CreateNFLPlayerDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros
        /// SP: app.sp_ListNFLPlayers
        /// Feature: Listar jugadores NFL
        /// Paginación: 50 por página (máx 100)
        /// Filtros: búsqueda por nombre, posición, equipo NFL, estado activo/inactivo
        /// </summary>
        Task<ListNFLPlayersResponseDTO> ListNFLPlayersAsync(ListNFLPlayersRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL
        /// SP: app.sp_GetNFLPlayerDetails (retorna 3 result sets)
        /// Feature: Ver detalles de jugador
        /// RS1: Información del jugador
        /// RS2: Historial de cambios (últimos 20)
        /// RS3: Equipos fantasy actuales que tienen este jugador
        /// </summary>
        Task<NFLPlayerDetailsDTO?> GetNFLPlayerDetailsAsync(int nflPlayerId);

        /// <summary>
        /// Actualiza un jugador NFL existente
        /// SP: app.sp_UpdateNFLPlayer
        /// Feature: Modificar jugador NFL
        /// </summary>
        Task<ApiResponseDTO> UpdateNFLPlayerAsync(int nflPlayerId, UpdateNFLPlayerDTO dto, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Desactiva un jugador NFL
        /// SP: app.sp_DeactivateNFLPlayer
        /// Feature: Desactivar jugador NFL
        /// Valida que no esté en roster activo de equipos en temporada actual
        /// </summary>
        Task<ApiResponseDTO> DeactivateNFLPlayerAsync(int nflPlayerId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Reactiva un jugador NFL desactivado
        /// SP: app.sp_ReactivateNFLPlayer
        /// Feature: Reactivar jugador NFL
        /// </summary>
        Task<ApiResponseDTO> ReactivateNFLPlayerAsync(int nflPlayerId, int actorUserId, string? sourceIp = null, string? userAgent = null);

        /// <summary>
        /// Lista jugadores disponibles (no en ningún roster activo)
        /// VIEW: vw_AvailablePlayers
        /// Para draft y free agency
        /// </summary>
        Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position = null);

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico
        /// VIEW: vw_PlayersByNFLTeam
        /// </summary>
        Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId);

        /// <summary>
        /// Obtiene jugadores NFL activos (para dropdowns)
        /// VIEW: vw_ActiveNFLPlayers
        /// </summary>
        Task<List<PlayerBasicDTO>> GetActiveNFLPlayersAsync(string? position = null);

        /// <summary>
        /// Obtiene un jugador específico por ID
        /// VIEW: vw_Players con WHERE
        /// </summary>
        Task<PlayerBasicDTO?> GetPlayerByIdAsync(int nflPlayerId);

        #region Batch Reports

        /// <summary>
        /// Crea un reporte de importación batch de jugadores NFL
        /// SP: app.sp_CreateNFLPlayerBatchReport
        /// Feature: Crear reporte de batch
        /// </summary>
        Task<ApiResponseDTO> CreateBatchReportAsync(
            CreateNFLPlayerBatchReportDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null);

        /// <summary>
        /// Lista todos los reportes de batch con paginación
        /// SP: app.sp_GetAllNFLPlayerBatchReports
        /// Feature: Listar reportes de batch
        /// Paginación: 50 por página (máx 100)
        /// </summary>
        Task<ListNFLPlayerBatchReportsResponseDTO> GetAllBatchReportsAsync(
            ListNFLPlayerBatchReportsRequestDTO request,
            int actorUserId);

        /// <summary>
        /// Obtiene un reporte de batch específico por ID
        /// SP: app.sp_GetNFLPlayerBatchReportById
        /// Feature: Ver detalles de reporte de batch
        /// </summary>
        Task<NFLPlayerBatchReportDetailsDTO?> GetBatchReportByIdAsync(
            int batchReportId,
            int actorUserId);

        #endregion

        #region Player News (Feature 10.3)

        /// <summary>
        /// Agrega una noticia a un jugador NFL
        /// SP: app.sp_AddNFLPlayerNews
        /// Feature 10.3 - US 1: Agregar noticia de jugador
        /// 
        /// RESPONSABILIDADES:
        /// - Validar datos de entrada usando NFLPlayerNewsValidator
        /// - Verificar que el jugador existe y está activo
        /// - Si es noticia de lesión, actualizar CurrentDesignation del jugador
        /// - Registrar cambio en NFLPlayerChangeLog
        /// - Registrar auditoría
        /// </summary>
        Task<ApiResponseDTO> AddPlayerNewsAsync(
            AddNFLPlayerNewsDTO dto,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null);

        /// <summary>
        /// Elimina una noticia de jugador y revierte su designación
        /// SP: app.sp_DeleteNFLPlayerNews
        /// Feature 10.3 - US 2: Eliminar noticia de jugador y revertir designación
        /// 
        /// RESPONSABILIDADES:
        /// - Verificar que la noticia existe y no está eliminada
        /// - Si la noticia tenía designación, buscar la designación previa en el historial
        /// - Revertir CurrentDesignation del jugador al estado previo
        /// - Marcar la noticia como eliminada (soft delete)
        /// - Registrar cambio en NFLPlayerChangeLog
        /// - Registrar auditoría
        /// </summary>
        Task<ApiResponseDTO> DeletePlayerNewsAsync(
            long newsId,
            int actorUserId,
            string? sourceIp = null,
            string? userAgent = null);

        /// <summary>
        /// Obtiene el feed de noticias de un jugador específico
        /// SP: app.sp_GetNFLPlayerNewsFeed
        /// Feature 10.3: Listar noticias de jugador
        /// 
        /// RETORNA:
        /// - Lista paginada de noticias en orden cronológico inverso
        /// - Solo noticias activas (IsDeleted = false)
        /// - Información del autor de cada noticia
        /// - Metadatos de paginación
        /// </summary>
        Task<GetNFLPlayerNewsFeedResponseDTO> GetPlayerNewsFeedAsync(
            GetNFLPlayerNewsFeedRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de una noticia específica por ID
        /// SP: app.sp_GetNFLPlayerNewsByID
        /// Feature 10.3: Ver detalles de noticia
        /// 
        /// RETORNA:
        /// - Información completa de la noticia
        /// - Datos del jugador asociado
        /// - Información de creación y eliminación (si aplica)
        /// </summary>
        Task<NFLPlayerNewsDetailsDTO?> GetPlayerNewsByIdAsync(long newsId);

        /// <summary>
        /// Lista jugadores filtrados por designación (IR, OUT, etc.)
        /// SP: app.sp_GetPlayersByDesignation
        /// Feature 10.3: Listar jugadores por estado
        /// 
        /// VALIDACIONES:
        /// - Designación debe ser válida (O, D, Q, P, FP, IR, PUP, SUS)
        /// 
        /// RETORNA:
        /// - Lista de jugadores con la designación especificada
        /// - Filtros opcionales por equipo NFL y posición
        /// - Solo jugadores activos (IsActive = 1)
        /// 
        /// USOS:
        /// - Validaciones de lineups (jugadores en IR/OUT no pueden jugar)
        /// - Reportes de lesiones por equipo
        /// - Análisis de disponibilidad
        /// </summary>
        Task<List<PlayerWithDesignationDTO>> GetPlayersByDesignationAsync(
            GetPlayersByDesignationRequestDTO request);

        #endregion
    }
}