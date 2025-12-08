using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces.NflDetails
{
    /// <summary>
    /// Contrato para la capa de acceso a datos de jugadores NFL.
    /// 
    /// Responsabilidad:
    /// - Definir las operaciones que interactúan con la base de datos
    ///   (SPs, Views, etc.) relacionadas con NFL Players y sus noticias.
    /// - No contiene lógica de negocio, solo operaciones de lectura/escritura.
    /// 
    /// Beneficios:
    /// - Permite inyección de dependencias (DI) en servicios.
    /// - Facilita el mocking en pruebas unitarias.
    /// </summary>
    public interface INFLPlayerDataAccess
    {
        #region CREATE / UPDATE / BASIC CRUD

        /// <summary>
        /// Crea un nuevo jugador NFL en la base de datos.
        /// SP: <c>app.sp_CreateNFLPlayer</c>
        /// </summary>
        /// <param name="dto">Datos del jugador a crear.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// Un <see cref="CreateNFLPlayerResponseDTO"/> con el ID y mensaje,
        /// o <c>null</c> si no se obtuvo resultado.
        /// </returns>
        Task<CreateNFLPlayerResponseDTO?> CreateNFLPlayerAsync(
            CreateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Lista jugadores NFL con paginación y filtros.
        /// SP: <c>app.sp_ListNFLPlayers</c>
        /// </summary>
        /// <param name="request">Parámetros de paginación y filtros.</param>
        /// <returns>
        /// Un <see cref="ListNFLPlayersResponseDTO"/> con jugadores y metadatos de paginación.
        /// </returns>
        Task<ListNFLPlayersResponseDTO> ListNFLPlayersAsync(
            ListNFLPlayersRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL.
        /// SP: <c>app.sp_GetNFLPlayerDetails</c>
        /// (múltiples result sets).
        /// </summary>
        /// <param name="nflPlayerId">ID del jugador NFL.</param>
        /// <returns>
        /// Un <see cref="NFLPlayerDetailsDTO"/> o <c>null</c> si no se encuentra.
        /// </returns>
        Task<NFLPlayerDetailsDTO?> GetNFLPlayerDetailsAsync(int nflPlayerId);

        /// <summary>
        /// Actualiza un jugador NFL existente.
        /// SP: <c>app.sp_UpdateNFLPlayer</c>
        /// </summary>
        /// <param name="nflPlayerId">ID del jugador a actualizar.</param>
        /// <param name="dto">Datos actualizados del jugador.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// Un <see cref="string"/> con el mensaje devuelto por el SP.
        /// </returns>
        Task<string> UpdateNFLPlayerAsync(
            int nflPlayerId,
            UpdateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Desactiva un jugador NFL (soft delete / baja lógica).
        /// SP: <c>app.sp_DeactivateNFLPlayer</c>
        /// </summary>
        /// <param name="nflPlayerId">ID del jugador a desactivar.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// Un <see cref="string"/> con el mensaje devuelto por el SP.
        /// </returns>
        Task<string> DeactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Reactiva un jugador NFL previamente desactivado.
        /// SP: <c>app.sp_ReactivateNFLPlayer</c>
        /// </summary>
        /// <param name="nflPlayerId">ID del jugador a reactivar.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// Un <see cref="string"/> con el mensaje devuelto por el SP.
        /// </returns>
        Task<string> ReactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        #endregion

        #region LISTADOS / QUERIES BÁSICOS

        /// <summary>
        /// Obtiene jugadores disponibles (no asignados a rosters activos).
        /// VIEW: <c>vw_AvailablePlayers</c>
        /// </summary>
        /// <param name="position">
        /// Posición opcional para filtrar (QB, RB, WR, etc.).
        /// </param>
        /// <returns>
        /// Lista de <see cref="AvailablePlayerDTO"/>.
        /// </returns>
        Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position);

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// VIEW: <c>vw_PlayersByNFLTeam</c>
        /// </summary>
        /// <param name="nflTeamId">ID del equipo NFL.</param>
        /// <returns>Lista de <see cref="PlayerBasicDTO"/>.</returns>
        Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId);

        /// <summary>
        /// Obtiene jugadores NFL activos (para dropdowns, etc.).
        /// VIEW: <c>vw_ActiveNFLPlayers</c>
        /// </summary>
        /// <param name="position">
        /// Posición opcional para filtrar.
        /// </param>
        /// <returns>Lista de <see cref="PlayerBasicDTO"/>.</returns>
        Task<List<PlayerBasicDTO>> GetActiveNFLPlayersAsync(string? position);

        /// <summary>
        /// Obtiene un jugador específico por ID.
        /// VIEW: <c>vw_Players</c> (con filtro por ID).
        /// </summary>
        /// <param name="nflPlayerId">ID del jugador NFL.</param>
        /// <returns>
        /// <see cref="PlayerBasicDTO"/> o <c>null</c> si no se encuentra.
        /// </returns>
        Task<PlayerBasicDTO?> GetPlayerByIdAsync(int nflPlayerId);

        #endregion

        #region BATCH REPORTS

        /// <summary>
        /// Crea un reporte de importación batch de jugadores NFL.
        /// SP: <c>app.sp_CreateNFLPlayerBatchReport</c>
        /// </summary>
        /// <param name="dto">Datos del reporte de batch (totales, errores, etc.).</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// <see cref="CreateNFLPlayerBatchReportResponseDTO"/> o <c>null</c> si no se obtuvo resultado.
        /// </returns>
        Task<CreateNFLPlayerBatchReportResponseDTO?> CreateBatchReportAsync(
            CreateNFLPlayerBatchReportDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Lista reportes de importación batch con paginación.
        /// SP: <c>app.sp_GetAllNFLPlayerBatchReports</c>
        /// </summary>
        /// <param name="request">Parámetros de paginación y ordenamiento.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <returns>
        /// <see cref="ListNFLPlayerBatchReportsResponseDTO"/> con reportes y metadatos.
        /// </returns>
        Task<ListNFLPlayerBatchReportsResponseDTO> GetAllBatchReportsAsync(
            ListNFLPlayerBatchReportsRequestDTO request,
            int actorUserId);

        /// <summary>
        /// Obtiene los detalles de un reporte batch específico.
        /// SP: <c>app.sp_GetNFLPlayerBatchReportById</c>
        /// </summary>
        /// <param name="batchReportId">ID del reporte batch.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <returns>
        /// <see cref="NFLPlayerBatchReportDetailsDTO"/> o <c>null</c>.
        /// </returns>
        Task<NFLPlayerBatchReportDetailsDTO?> GetBatchReportByIdAsync(
            int batchReportId,
            int actorUserId);

        #endregion

        #region PLAYER NEWS (Feature 10.3)

        /// <summary>
        /// Agrega una noticia a un jugador NFL.
        /// SP: <c>app.sp_AddNFLPlayerNews</c>
        /// </summary>
        /// <param name="dto">Datos de la noticia (texto, lesión, designación, etc.).</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// <see cref="AddNFLPlayerNewsResponseDTO"/> o <c>null</c> si no se obtuvo resultado.
        /// </returns>
        Task<AddNFLPlayerNewsResponseDTO?> AddPlayerNewsAsync(
            AddNFLPlayerNewsDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Elimina una noticia de jugador y revierte su designación si aplica.
        /// SP: <c>app.sp_DeleteNFLPlayerNews</c>
        /// </summary>
        /// <param name="newsId">ID de la noticia a eliminar.</param>
        /// <param name="actorUserId">ID del usuario que ejecuta la acción.</param>
        /// <param name="sourceIp">IP de origen de la petición.</param>
        /// <param name="userAgent">User-Agent del cliente.</param>
        /// <returns>
        /// <see cref="DeleteNFLPlayerNewsResponseDTO"/> o <c>null</c>.
        /// </returns>
        Task<DeleteNFLPlayerNewsResponseDTO?> DeletePlayerNewsAsync(
            long newsId,
            int actorUserId,
            string? sourceIp,
            string? userAgent);

        /// <summary>
        /// Obtiene el feed de noticias de un jugador específico, con paginación.
        /// SP: <c>app.sp_GetNFLPlayerNewsFeed</c>
        /// </summary>
        /// <param name="request">Parámetros de paginación y jugador.</param>
        /// <returns>
        /// <see cref="GetNFLPlayerNewsFeedResponseDTO"/> con noticias y metadatos.
        /// </returns>
        Task<GetNFLPlayerNewsFeedResponseDTO> GetPlayerNewsFeedAsync(
            GetNFLPlayerNewsFeedRequestDTO request);

        /// <summary>
        /// Obtiene detalles completos de una noticia específica.
        /// SP: <c>app.sp_GetNFLPlayerNewsByID</c>
        /// </summary>
        /// <param name="newsId">ID de la noticia.</param>
        /// <returns>
        /// <see cref="NFLPlayerNewsDetailsDTO"/> o <c>null</c> si no existe.
        /// </returns>
        Task<NFLPlayerNewsDetailsDTO?> GetPlayerNewsByIdAsync(long newsId);

        /// <summary>
        /// Lista jugadores filtrados por designación (IR, OUT, Q, D, etc.).
        /// SP: <c>app.sp_GetPlayersByDesignation</c>
        /// </summary>
        /// <param name="request">Filtros de designación, equipo y posición.</param>
        /// <returns>
        /// Lista de <see cref="PlayerWithDesignationDTO"/>.
        /// </returns>
        Task<List<PlayerWithDesignationDTO>> GetPlayersByDesignationAsync(
            GetPlayersByDesignationRequestDTO request);

        #endregion
    }
}
