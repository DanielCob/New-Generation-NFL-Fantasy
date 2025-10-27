using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;

namespace NFL_Fantasy_API.Services.Interfaces
{
    public interface ISeasonService
    {
        Task<SeasonVM?> GetCurrentSeasonAsync();
        Task<SeasonVM?> CreateSeasonAsync(CreateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent);
        Task<string> DeactivateSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent);
        Task<string> DeleteSeasonAsync(int seasonId, bool confirm, int actorUserId, string? ip, string? userAgent);
        Task<SeasonVM?> UpdateSeasonAsync(int seasonId, UpdateSeasonRequestDTO dto, int actorUserId, string? ip, string? userAgent);
        Task<List<SeasonWeekVM>> GetSeasonWeeksAsync(int seasonId);
        Task<SeasonVM?> GetSeasonByIdAsync(int seasonId);
    }
}
