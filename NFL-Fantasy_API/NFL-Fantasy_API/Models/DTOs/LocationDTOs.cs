// Models/DTOs/LocationDTOs.cs
using System.ComponentModel.DataAnnotations;

namespace NFL_Fantasy_API.Models.DTOs
{
    public class ProvinceDTO
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProvinceDTO
    {
        [Required]
        [StringLength(100)]
        public string ProvinceName { get; set; } = string.Empty;
    }

    public class CantonDTO
    {
        public int CantonID { get; set; }
        public int ProvinceID { get; set; }
        public string CantonName { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCantonDTO
    {
        [Required]
        [StringLength(100)]
        public string CantonName { get; set; } = string.Empty;

        [Required]
        public int ProvinceID { get; set; }
    }

    public class DistrictDTO
    {
        public int DistrictID { get; set; }
        public int CantonID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public string CantonName { get; set; } = string.Empty;
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDistrictDTO
    {
        [Required]
        [StringLength(100)]
        public string DistrictName { get; set; } = string.Empty;

        [Required]
        public int CantonID { get; set; }
    }
}