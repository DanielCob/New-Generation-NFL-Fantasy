using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.Fantasy;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.GameDatabase.Interfaces;

namespace NFL_Fantasy_API.DataAccessLayer.GameDatabase.Implementations.NflDetails
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de equipos NFL.
    /// Responsabilidad: Construcción de parámetros y ejecución de SPs/Views.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class NFLTeamDataAccess
    {
        private readonly IDatabaseHelper _db;

        public NFLTeamDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region Create NFL Team

        /// <summary>
        /// Crea un nuevo equipo NFL.
        /// SP: app.sp_CreateNFLTeam
        /// </summary>
        public async Task<CreateNFLTeamResponseDTO?> CreateNFLTeamAsync(
            CreateNFLTeamDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@TeamName", dto.TeamName),
                SqlParameterExtensions.CreateParameter("@City", dto.City),
                SqlParameterExtensions.CreateParameter("@TeamImageUrl", dto.TeamImageUrl),
                SqlParameterExtensions.CreateParameter("@TeamImageWidth", dto.TeamImageWidth),
                SqlParameterExtensions.CreateParameter("@TeamImageHeight", dto.TeamImageHeight),
                SqlParameterExtensions.CreateParameter("@TeamImageBytes", dto.TeamImageBytes),
                SqlParameterExtensions.CreateParameter("@ThumbnailUrl", dto.ThumbnailUrl),
                SqlParameterExtensions.CreateParameter("@ThumbnailWidth", dto.ThumbnailWidth),
                SqlParameterExtensions.CreateParameter("@ThumbnailHeight", dto.ThumbnailHeight),
                SqlParameterExtensions.CreateParameter("@ThumbnailBytes", dto.ThumbnailBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureAsync(
                "app.sp_CreateNFLTeam",
                parameters,
                reader => new CreateNFLTeamResponseDTO
                {
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    City = reader.GetSafeString("City"),
                    Message = reader.GetSafeString("Message")
                }
            );
        }

        #endregion

        #region List NFL Teams

        /// <summary>
        /// Lista equipos NFL con paginación y filtros.
        /// SP: app.sp_ListNFLTeams
        /// </summary>
        public async Task<ListNFLTeamsResponseDTO> ListNFLTeamsAsync(ListNFLTeamsRequestDTO request)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@PageNumber", request.PageNumber),
                SqlParameterExtensions.CreateParameter("@PageSize", request.PageSize),
                SqlParameterExtensions.CreateParameter("@SearchTerm", request.SearchTerm),
                SqlParameterExtensions.CreateParameter("@FilterCity", request.FilterCity),
                SqlParameterExtensions.CreateParameter("@FilterIsActive", request.FilterIsActive)
            };

            var response = new ListNFLTeamsResponseDTO();

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_ListNFLTeams", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // Leer equipos
            while (await reader.ReadAsync())
            {
                response.Teams.Add(new NFLTeamListItemDTO
                {
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    City = reader.GetSafeString("City"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
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

        #region Get NFL Team Details

        /// <summary>
        /// Obtiene detalles completos de un equipo NFL.
        /// SP: app.sp_GetNFLTeamDetails (retorna 3 result sets)
        /// RS1: Información del equipo
        /// RS2: Historial de cambios
        /// RS3: Jugadores activos
        /// </summary>
        public async Task<NFLTeamDetailsDTO?> GetNFLTeamDetailsAsync(int nflTeamId)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@NFLTeamID", nflTeamId)
            };

            NFLTeamDetailsDTO? details = null;

            // Obtener connection string usando reflection
            var connStr = _db.GetType()
                .GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_db) as string;

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException("No se pudo obtener la cadena de conexión.");
            }

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand("app.sp_GetNFLTeamDetails", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            // RS1: Información del equipo
            if (await reader.ReadAsync())
            {
                details = new NFLTeamDetailsDTO
                {
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    City = reader.GetSafeString("City"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    TeamImageWidth = reader.GetSafeInt16("TeamImageWidth") == 0
                        ? null : reader.GetSafeInt16("TeamImageWidth"),
                    TeamImageHeight = reader.GetSafeInt16("TeamImageHeight") == 0
                        ? null : reader.GetSafeInt16("TeamImageHeight"),
                    TeamImageBytes = reader.GetSafeNullableInt32("TeamImageBytes"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl"),
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
                    details.ChangeHistory.Add(new NFLTeamChangeDTO
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

            // RS3: Jugadores activos
            if (details != null && await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    details.ActivePlayers.Add(new PlayerBasicDTO
                    {
                        NFLPlayerID = reader.GetSafeInt32("NFLPlayerID"),  // CORREGIDO
                        FirstName = reader.GetSafeString("FirstName"),
                        LastName = reader.GetSafeString("LastName"),
                        FullName = reader.GetSafeString("FullName"),
                        Position = reader.GetSafeString("Position"),
                        NFLTeamID = reader.GetSafeInt32("NFLTeamID"),  // AGREGADO (es required ahora)
                        InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                        IsActive = reader.GetSafeBool("IsActive")
                    });
                }
            }

            return details;
        }

        #endregion

        #region Update NFL Team

        /// <summary>
        /// Actualiza un equipo NFL existente.
        /// SP: app.sp_UpdateNFLTeam
        /// </summary>
        public async Task<string> UpdateNFLTeamAsync(
            int nflTeamId,
            UpdateNFLTeamDTO dto,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLTeamID", nflTeamId),
                SqlParameterExtensions.CreateParameter("@TeamName", dto.TeamName),
                SqlParameterExtensions.CreateParameter("@City", dto.City),
                SqlParameterExtensions.CreateParameter("@TeamImageUrl", dto.TeamImageUrl),
                SqlParameterExtensions.CreateParameter("@TeamImageWidth", dto.TeamImageWidth),
                SqlParameterExtensions.CreateParameter("@TeamImageHeight", dto.TeamImageHeight),
                SqlParameterExtensions.CreateParameter("@TeamImageBytes", dto.TeamImageBytes),
                SqlParameterExtensions.CreateParameter("@ThumbnailUrl", dto.ThumbnailUrl),
                SqlParameterExtensions.CreateParameter("@ThumbnailWidth", dto.ThumbnailWidth),
                SqlParameterExtensions.CreateParameter("@ThumbnailHeight", dto.ThumbnailHeight),
                SqlParameterExtensions.CreateParameter("@ThumbnailBytes", dto.ThumbnailBytes),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_UpdateNFLTeam",
                parameters
            );
        }

        #endregion

        #region Deactivate / Reactivate

        /// <summary>
        /// Desactiva un equipo NFL.
        /// SP: app.sp_DeactivateNFLTeam
        /// </summary>
        public async Task<string> DeactivateNFLTeamAsync(
            int nflTeamId,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLTeamID", nflTeamId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_DeactivateNFLTeam",
                parameters
            );
        }

        /// <summary>
        /// Reactiva un equipo NFL desactivado.
        /// SP: app.sp_ReactivateNFLTeam
        /// </summary>
        public async Task<string> ReactivateNFLTeamAsync(
            int nflTeamId,
            int actorUserId,
            string? sourceIp,
            string? userAgent)
        {
            var parameters = new SqlParameter[]
            {
                SqlParameterExtensions.CreateParameter("@ActorUserID", actorUserId),
                SqlParameterExtensions.CreateParameter("@NFLTeamID", nflTeamId),
                SqlParameterExtensions.CreateParameter("@SourceIp", sourceIp),
                SqlParameterExtensions.CreateParameter("@UserAgent", userAgent)
            };

            return await _db.ExecuteStoredProcedureForMessageAsync(
                "app.sp_ReactivateNFLTeam",
                parameters
            );
        }

        #endregion

        #region Get Active NFL Teams

        /// <summary>
        /// Obtiene equipos NFL activos desde VIEW.
        /// VIEW: vw_ActiveNFLTeams
        /// </summary>
        public async Task<List<NFLTeamBasicVM>> GetActiveNFLTeamsAsync()
        {
            return await _db.ExecuteViewAsync(
                "vw_ActiveNFLTeams",
                reader => new NFLTeamBasicVM
                {
                    NFLTeamID = reader.GetSafeInt32("NFLTeamID"),
                    TeamName = reader.GetSafeString("TeamName"),
                    City = reader.GetSafeString("City"),
                    TeamImageUrl = reader.GetSafeNullableString("TeamImageUrl"),
                    ThumbnailUrl = reader.GetSafeNullableString("ThumbnailUrl")
                },
                orderBy: "TeamName"
            );
        }

        #endregion
    }
}