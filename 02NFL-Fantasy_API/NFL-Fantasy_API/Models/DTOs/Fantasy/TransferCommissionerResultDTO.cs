namespace NFL_Fantasy_API.Models.DTOs.Fantasy
{
    public class TransferCommissionerResultDTO
    {
        public string Message { get; set; } = string.Empty;
        public int NewCommissionerID { get; set; }
        public string NewCommissionerName { get; set; } = string.Empty;
    }
}
