// Models/ViewModels/EngineerViewModel.cs
namespace NFL_Fantasy_API.Models.ViewModels
{
    public class EngineerViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastSurname { get; set; } = string.Empty;
        public string? SecondSurname { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public string Career { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
        public string CantonName { get; set; } = string.Empty;
        public string? DistrictName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool HasActiveSession { get; set; }
    }
}