using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Extensions;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Interfaces;
using NFL_Fantasy_API.Models.DTOs.NflDetails;

namespace NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails
{
    /// <summary>
    /// Capa de acceso a datos para operaciones de jugadores NFL.
    /// Responsabilidad: Ejecución de queries a VIEWs de jugadores.
    /// NO contiene lógica de negocio.
    /// </summary>
    public class PlayerDataAccess
    {
        private readonly IDatabaseHelper _db;

        public PlayerDataAccess(IConfiguration configuration, IDatabaseHelper db)
        {
            _db = db;
        }

        #region List Players

        /// <summary>
        /// Lista todos los jugadores NFL con filtros opcionales.
        /// VIEW: vw_Players
        /// </summary>
        public async Task<List<PlayerBasicDTO>> ListPlayersAsync(
            string? position,
            int? nflTeamId,
            string? injuryStatus)
        {
            var whereClauses = new List<string>();

            if (!string.IsNullOrEmpty(position))
            {
                whereClauses.Add($"Position = '{position}'");
            }

            if (nflTeamId.HasValue)
            {
                whereClauses.Add($"NFLTeamID = {nflTeamId.Value}");
            }

            if (!string.IsNullOrEmpty(injuryStatus))
            {
                whereClauses.Add($"InjuryStatus = '{injuryStatus}'");
            }

            var whereClause = whereClauses.Any() ? string.Join(" AND ", whereClauses) : null;

            return await _db.ExecuteViewAsync(
                "vw_Players",
                reader => new PlayerBasicDTO
                {
                    PlayerID = reader.GetSafeInt32("PlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeNullableInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive")
                },
                whereClause: whereClause,
                orderBy: "FullName"
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
                    PlayerID = reader.GetSafeInt32("PlayerID"),
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
                    PlayerID = reader.GetSafeInt32("PlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    IsActive = reader.GetSafeBool("PlayerIsActive")
                },
                whereClause: $"NFLTeamID = {nflTeamId}",
                orderBy: "Position, FullName"
            );
        }

        #endregion

        #region Get Player by ID

        /// <summary>
        /// Obtiene un jugador específico por ID.
        /// VIEW: vw_Players con WHERE
        /// </summary>
        public async Task<PlayerBasicDTO?> GetPlayerByIdAsync(int playerId)
        {
            var results = await _db.ExecuteViewAsync(
                "vw_Players",
                reader => new PlayerBasicDTO
                {
                    PlayerID = reader.GetSafeInt32("PlayerID"),
                    FirstName = reader.GetSafeString("FirstName"),
                    LastName = reader.GetSafeString("LastName"),
                    FullName = reader.GetSafeString("FullName"),
                    Position = reader.GetSafeString("Position"),
                    NFLTeamID = reader.GetSafeNullableInt32("NFLTeamID"),
                    NFLTeamName = reader.GetSafeNullableString("NFLTeamName"),
                    InjuryStatus = reader.GetSafeNullableString("InjuryStatus"),
                    PhotoThumbnailUrl = reader.GetSafeNullableString("PhotoThumbnailUrl"),
                    IsActive = reader.GetSafeBool("IsActive")
                },
                whereClause: $"PlayerID = {playerId}"
            );

            return results.FirstOrDefault();
        }

        #endregion
    }
}