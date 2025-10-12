using Microsoft.Extensions.Configuration;
using NFL_Fantasy_API.Data;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de jugadores NFL
    /// </summary>
    public class PlayerService : IPlayerService
    {
        private readonly DatabaseHelper _db;

        public PlayerService(IConfiguration configuration)
        {
            _db = new DatabaseHelper(configuration);
        }

        public async Task<List<PlayerBasicDTO>> ListPlayersAsync(string? position = null, int? nflTeamId = null, string? injuryStatus = null)
        {
            try
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

                return await _db.ExecuteViewAsync<PlayerBasicDTO>(
                    "vw_Players",
                    reader => new PlayerBasicDTO
                    {
                        PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastName = DatabaseHelper.GetSafeString(reader, "LastName"),
                        FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                        Position = DatabaseHelper.GetSafeString(reader, "Position"),
                        NFLTeamID = DatabaseHelper.GetSafeNullableInt32(reader, "NFLTeamID"),
                        NFLTeamName = DatabaseHelper.GetSafeNullableString(reader, "NFLTeamName"),
                        InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus"),
                        PhotoThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "PhotoThumbnailUrl"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive")
                    },
                    whereClause: whereClause,
                    orderBy: "FullName"
                );
            }
            catch
            {
                return new List<PlayerBasicDTO>();
            }
        }

        public async Task<List<AvailablePlayerDTO>> GetAvailablePlayersAsync(string? position = null)
        {
            try
            {
                var whereClause = !string.IsNullOrEmpty(position) ? $"Position = '{position}'" : null;

                return await _db.ExecuteViewAsync<AvailablePlayerDTO>(
                    "vw_AvailablePlayers",
                    reader => new AvailablePlayerDTO
                    {
                        PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                        FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                        Position = DatabaseHelper.GetSafeString(reader, "Position"),
                        NFLTeamName = DatabaseHelper.GetSafeNullableString(reader, "NFLTeamName"),
                        NFLTeamCity = DatabaseHelper.GetSafeNullableString(reader, "NFLTeamCity"),
                        InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus"),
                        PhotoThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "PhotoThumbnailUrl")
                    },
                    whereClause: whereClause,
                    orderBy: "FullName"
                );
            }
            catch
            {
                return new List<AvailablePlayerDTO>();
            }
        }

        public async Task<List<PlayerBasicDTO>> GetPlayersByNFLTeamAsync(int nflTeamId)
        {
            try
            {
                return await _db.ExecuteViewAsync<PlayerBasicDTO>(
                    "vw_PlayersByNFLTeam",
                    reader => new PlayerBasicDTO
                    {
                        PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastName = DatabaseHelper.GetSafeString(reader, "LastName"),
                        FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                        Position = DatabaseHelper.GetSafeString(reader, "Position"),
                        InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "PlayerIsActive")
                    },
                    whereClause: $"NFLTeamID = {nflTeamId}",
                    orderBy: "Position, FullName"
                );
            }
            catch
            {
                return new List<PlayerBasicDTO>();
            }
        }

        public async Task<PlayerBasicDTO?> GetPlayerByIdAsync(int playerId)
        {
            try
            {
                var results = await _db.ExecuteViewAsync<PlayerBasicDTO>(
                    "vw_Players",
                    reader => new PlayerBasicDTO
                    {
                        PlayerID = DatabaseHelper.GetSafeInt32(reader, "PlayerID"),
                        FirstName = DatabaseHelper.GetSafeString(reader, "FirstName"),
                        LastName = DatabaseHelper.GetSafeString(reader, "LastName"),
                        FullName = DatabaseHelper.GetSafeString(reader, "FullName"),
                        Position = DatabaseHelper.GetSafeString(reader, "Position"),
                        NFLTeamID = DatabaseHelper.GetSafeNullableInt32(reader, "NFLTeamID"),
                        NFLTeamName = DatabaseHelper.GetSafeNullableString(reader, "NFLTeamName"),
                        InjuryStatus = DatabaseHelper.GetSafeNullableString(reader, "InjuryStatus"),
                        PhotoThumbnailUrl = DatabaseHelper.GetSafeNullableString(reader, "PhotoThumbnailUrl"),
                        IsActive = DatabaseHelper.GetSafeBool(reader, "IsActive")
                    },
                    whereClause: $"PlayerID = {playerId}"
                );

                return results.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
