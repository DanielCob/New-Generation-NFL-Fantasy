namespace NFL_Fantasy_API.Models.DTOs.Fantasy
{
    public class AssignCoCommissionerResultDTO
    {
        public string Message { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
    }
}
