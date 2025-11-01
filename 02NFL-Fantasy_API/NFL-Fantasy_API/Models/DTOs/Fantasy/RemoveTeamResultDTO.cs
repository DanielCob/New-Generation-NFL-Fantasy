namespace NFL_Fantasy_API.Models.DTOs.Fantasy
{
    public class RemoveTeamResultDTO
    {
        public int AvailableSlots { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
