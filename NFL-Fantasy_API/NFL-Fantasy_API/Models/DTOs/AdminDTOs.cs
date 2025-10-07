// Models/DTOs/AdminDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    public class UpdateClientDTO
    {
        [StringLength(50)]
        public string? Username { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastSurname { get; set; }

        [StringLength(100)]
        public string? SecondSurname { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [MinLength(6)]
        [StringLength(255)]
        public string? Password { get; set; }

        public DateTime? BirthDate { get; set; }

        public int? ProvinceID { get; set; }

        public int? CantonID { get; set; }

        public int? DistrictID { get; set; }
    }

    public class UpdateEngineerDTO : UpdateClientDTO
    {
        [StringLength(200)]
        public string? Career { get; set; }

        [StringLength(200)]
        public string? Specialization { get; set; }
    }

    public class UpdateAdministratorDTO : UpdateClientDTO
    {
        [StringLength(500)]
        public string? Detail { get; set; }
    }
}