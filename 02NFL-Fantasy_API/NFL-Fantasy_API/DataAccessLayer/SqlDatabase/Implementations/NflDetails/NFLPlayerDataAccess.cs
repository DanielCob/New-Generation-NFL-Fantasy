using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de jugadores NFL.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/Views.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class NFLPlayerDataAccess
    {
        private readonly IDatabaseHelper _db;

        public NFLPlayerDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Create NFL Player

        /// <summary>
        /// Crea un nuevo jugador NFL.
        /// SP: app.sp_CreateNFLPlayer
        /// </summary>
        public async Task<CreateNFLPlayerResponseDTO?> CreateNFLPlayerAsync(
            CreateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@FirstName", dto.FirstName),
                SqlParameterExtensions.CreateParameter("@LastName", dto.LastName),
                SqlParameterExtensions.CreateParameter("@Position", dto.Position),
                SqlParameterExtensions.CreateParameter("@NFLTeamID", dto.NFLTeamID),
                SqlParameterExtensions.CreateParameter("@InjuryStatus", dto.InjuryStatus),
                SqlParameterExtensions.CreateParameter("@InjuryDescription", dto.InjuryDescription),
                SqlParameterExtensions.CreateParameter("@PhotoUrl", dto.PhotoUrl),
                SqlParameterExtensions.CreateParameter("@PhotoWidth", dto.PhotoWidth),
                SqlParameterExtensions.CreateParameter("@PhotoHeight", dto.PhotoHeight),
                SqlParameterExtensions.CreateParameter("@PhotoBytes", dto.PhotoBytes),
                SqlParameterExtensions.CreateParameter("@PhotoThumbnailUrl", dto.PhotoThumbnailUrl),
                SqlParameterExtensions.CreateParameter("@ThumbnailWidth", dto.ThumbnailWidth),
                SqlParameterExtensions.CreateParameter("@ThumbnailHeight", dto.ThumbnailHeight),
                SqlParameterExtensions.CreateParameter("@ThumbnailBytes", dto.ThumbnailBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_CreateNFLPlayer",
                parameters,
                reader => new CreateNFLPlayerResponseDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    Message = reader.GetSafeString("Message")
                }
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
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@PageNumber", request.PageNumber),
                SqlParameterExtensions.CreateParameter("@PageSize", request.PageSize),
                SqlParameterExtensions.CreateParameter("@SearchTerm", request.SearchTerm),
                SqlParameterExtensions.CreateParameter("@FilterPosition", request.FilterPosition),
                SqlParameterExtensions.CreateParameter("@FilterNFLTeamID", request.FilterNFLTeamID),
                SqlParameterExtensions.CreateParameter("@FilterIsActive", request.FilterIsActive)
            };

            var response = new ListNFLPlayersResponseDTO();

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_ListNFLPlayers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Leer jugadores
            while (await reader.ReadAsync())
            {
                response.Players.Add(new NFLPlayerListItemDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeString("NFLTeamName"),
                    NFLTeamCity = reader.GetSafeString("NFLTeamCity"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    InjuryDescription = reader.GetSafeNullableString("InjuryDescription"),
                    PhotoUrl = reader.GetSafeNullableString("PhotoUrl"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt")
                });

                // Leer metadatos de la primera fila
                if (response.TotalRecords == 0)
                {
                    response.TotalRecords = reader.GetSafeInt32("TotalRecords");
                    response.CurrentPage = reader.GetSafeInt32("CurrentPage");
                    response.PageSize = reader.GetSafeInt32("PageSize");
                    response.TotalPages = reader.GetSafeInt32("TotalPages");
                }
            }

            return response;
        }

        #endregion

        #region Get NFL Player Details

        /// <summary>
        /// Obtiene detalles completos de un jugador NFL.
        /// SP: app.sp_GetNFLPlayerDetails (retorna 3 result sets)
        /// RS1: Información del jugador
        /// RS2: Historial de cambios
        /// RS3: Equipos fantasy actuales
        /// </summary>
        public async Task<NFLPlayerDetailsDTO?> GetNFLPlayerDetailsAsync(int nflPlayerId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@NFLPlayerID", nflPlayerId)
            };

            NFLPlayerDetailsDTO? details = null;

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetNFLPlayerDetails", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // RS1: Información del jugador
            if (await reader.ReadAsync())
            {
                details = new NFLPlayerDetailsDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeString("NFLTeamName"),
                    NFLTeamCity = reader.GetSafeString("NFLTeamCity"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    InjuryDescription = reader.GetSafeNullableString("InjuryDescription"),
                    PhotoUrl = reader.GetSafeNullableString("PhotoUrl"),
                    PhotoWidth = reader.GetSafeInt16("PhotoWidth") == 0
                        ? null : reader.GetSafeInt16("PhotoWidth"),
                    PhotoHeight = reader.GetSafeInt16("PhotoHeight") == 0
                        ? null : reader.GetSafeInt16("PhotoHeight"),
                    PhotoBytes = reader.GetSafeNullableInt32("PhotoBytes"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    ThumbnailWidth = reader.GetSafeInt16("ThumbnailWidth") == 0
                        ? null : reader.GetSafeInt16("ThumbnailWidth"),
                    ThumbnailHeight = reader.GetSafeInt16("ThumbnailHeight") == 0
                        ? null : reader.GetSafeInt16("ThumbnailHeight"),
                    ThumbnailBytes = reader.GetSafeNullableInt32("ThumbnailBytes"),
                    IsActive = reader.GetSafeBool("IsActive"),
                    CreatedAt = reader.GetSafeDateTime("CreatedAt"),
                    CreatedByName = reader.GetSafeNullableString("CreatedByName"),
                    UpdatedAt = reader.GetSafeDateTime("UpdatedAt"),
                    UpdatedByName = reader.GetSafeNullableString("UpdatedByName")
                };
            }

            // RS2: Historial de cambios
            if (details != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    details.ChangeHistory.Add(new NFLPlayerChangeDTO
                    {
                        ChangeID = reader.GetSafeInt64("ChangeID"),
                        FieldName = reader.GetSafeString("FieldName"),
                        OldValue = reader.GetSafeNullableString("OldValue"),
                        NewValue = reader.GetSafeNullableString("NewValue"),
                        ChangedAt = reader.GetSafeDateTime("ChangedAt"),
                        ChangedByName = reader.GetSafeString("ChangedByName")
                    });
                }
            }

            // RS3: Equipos fantasy actuales
            if (details != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    details.CurrentFantasyTeams.Add(new FantasyTeamWithPlayerDTO
                    {
                        TeamID = reader.GetSafeInt32("TeamID"),
                        TeamName = reader.GetSafeString("TeamName"),
                        LeagueName = reader.GetSafeString("LeagueName"),
                        LeagueID = reader.GetSafeInt32("LeagueID"),
                        SeasonLabel = reader.GetSafeString("SeasonLabel"),
                        AcquisitionType = reader.GetSafeString("AcquisitionType"),
                        AcquisitionDate = reader.GetSafeDateTime("AcquisitionDate"),
                        ManagerName = reader.GetSafeString("ManagerName")
                    });
                }
            }

            return details;
        }

        #endregion

        #region Update NFL Player

        /// <summary>
        /// Actualiza un jugador NFL existente.
        /// SP: app.sp_UpdateNFLPlayer
        /// </summary>
        public async Task<string> UpdateNFLPlayerAsync(
            int nflPlayerId,
            UpdateNFLPlayerDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLPlayerID", nflPlayerId),
                SqlParameterExtensions.CreateParameter("@FirstName", dto.FirstName),
                SqlParameterExtensions.CreateParameter("@LastName", dto.LastName),
                SqlParameterExtensions.CreateParameter("@Position", dto.Position),
                SqlParameterExtensions.CreateParameter("@NFLTeamID", dto.NFLTeamID),
                SqlParameterExtensions.CreateParameter("@InjuryStatus", dto.InjuryStatus),
                SqlParameterExtensions.CreateParameter("@InjuryDescription", dto.InjuryDescription),
                SqlParameterExtensions.CreateParameter("@PhotoUrl", dto.PhotoUrl),
                SqlParameterExtensions.CreateParameter("@PhotoWidth", dto.PhotoWidth),
                SqlParameterExtensions.CreateParameter("@PhotoHeight", dto.PhotoHeight),
                SqlParameterExtensions.CreateParameter("@PhotoBytes", dto.PhotoBytes),
                SqlParameterExtensions.CreateParameter("@PhotoThumbnailUrl", dto.PhotoThumbnailUrl),
                SqlParameterExtensions.CreateParameter("@ThumbnailWidth", dto.ThumbnailWidth),
                SqlParameterExtensions.CreateParameter("@ThumbnailHeight", dto.ThumbnailHeight),
                SqlParameterExtensions.CreateParameter("@ThumbnailBytes", dto.ThumbnailBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_UpdateNFLPlayer",
                parameters
            );
        }

        #endregion

        #region Deactivate / Reactivate

        /// <summary>
        /// Desactiva un jugador NFL.
        /// SP: app.sp_DeactivateNFLPlayer
        /// </summary>
        public async Task<string> DeactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLPlayerID", nflPlayerId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_DeactivateNFLPlayer",
                parameters
            );
        }

        /// <summary>
        /// Reactiva un jugador NFL desactivado.
        /// SP: app.sp_ReactivateNFLPlayer
        /// </summary>
        public async Task<string> ReactivateNFLPlayerAsync(
            int nflPlayerId,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLPlayerID", nflPlayerId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_ReactivateNFLPlayer",
                parameters
            );
        }

        #endregion

        #region Available Players

        /// <summary>
        /// Lista jugadores disponibles (no en ningún roster activo).
        /// VIEW: vw_AvailablePlayers
        /// </summary>
        public async Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position)
        {
            var whereClause = !string.IsNullOrEmpty(position) ? $"Position = '{position}'" : null;

            return await _db.ExecuteViewAsync(
                "vw_AvailablePlayers",
                reader => new AvailablePlayerDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                    NFLTeamCity = reader.GetSafeNullableString("NFLTeamCity"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl")
                },
                whereClause: whereClause,
                orderBy: "FullName"
            );
        }

        #endregion

        #region Players by NFL Team

        /// <summary>
        /// Obtiene jugadores de un equipo NFL específico.
        /// VIEW: vw_PlayersByNFLTeam
        /// </summary>
        public async Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId)
        {
            return await _db.ExecuteViewAsync(
                "vw_PlayersByNFLTeam",
                reader => new PlayerBasicDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = nflTeamId,
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    IsActive = reader.GetSafeBool("PlayerIsActive")
                },
                whereClause: $"NFLTeamID = {nflTeamId}",
                orderBy: "Position, FullName"
            );
        }

        #endregion

        #region Get Active NFL Players

        /// <summary>
        /// Obtiene jugadores NFL activos desde VIEW.
        /// VIEW: vw_ActiveNFLPlayers
        /// </summary>
        public async Task<List<PlayerBasicDTO>> GetActiveNFLPlayersAsync(string? position)
        {
            var whereClause = !string.IsNullOrEmpty(position) ? $"Position = '{position}'" : null;

            return await _db.ExecuteViewAsync(
                "vw_ActiveNFLPlayers",
                reader => new PlayerBasicDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    IsActive = true
                },
                whereClause: whereClause,
                orderBy: "FullName"
            );
        }

        #endregion

        #region Get Player by ID

        /// <summary>
        /// Obtiene un jugador específico por ID.
        /// VIEW: vw_Players con WHERE
        /// </summary>
        public async Task<PlayerBasicDTO?> GetPlayerByIdAsync(int nflPlayerId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_Players",
                reader => new PlayerBasicDTO
                {
                    NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive")
                },
                whereClause: $"NFLPlayerID = {nflPlayerId}"
            );

            return results.FirstOrDefault();
        }

        #endregion
    }
}