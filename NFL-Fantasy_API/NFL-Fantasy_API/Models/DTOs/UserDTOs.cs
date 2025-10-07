// Models/DTOs/UserDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    public class CreateClientDTO
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastSurname { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SecondSurname { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        public int ProvinceID { get; set; }

        [Required]
        public int CantonID { get; set; }

        public int? DistrictID { get; set; }
    }

    public class CreateEngineerDTO
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastSurname { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SecondSurname { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        public int ProvinceID { get; set; }

        [Required]
        public int CantonID { get; set; }

        public int? DistrictID { get; set; }

        [Required]
        [StringLength(200)]
        public string Career { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Specialization { get; set; }
    }

    public class CreateAdministratorDTO
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastSurname { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SecondSurname { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        public int ProvinceID { get; set; }

        [Required]
        public int CantonID { get; set; }

        public int? DistrictID { get; set; }

        [StringLength(500)]
        public string? Detail { get; set; }
    }

    public class UserResponseDTO
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastSurname { get; set; } = string.Empty;
        public string? SecondSurname { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public string CantonName { get; set; } = string.Empty;
        public string? DistrictName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EngineerResponseDTO : UserResponseDTO
    {
        public string Career { get; set; } = string.Empty;
        public string? Specialization { get; set; }
    }

    public class AdministratorResponseDTO : UserResponseDTO
    {
        public string? Detail { get; set; }
    }
}