// Controllers/LocationController.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        #region Provinces
        /// <summary>
        /// Get all provinces
        /// </summary>
        /// <returns>List of all provinces</returns>
        [HttpGet("provinces")]
        public async Task<ActionResult<IEnumerable<ProvinceDTO>>> GetProvinces()
        {
            try
            {
                var provinces = await _locationService.GetProvincesAsync();
                return Ok(provinces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get province by ID
        /// </summary>
        /// <param name="id">Province ID</param>
        /// <returns>Province information</returns>
        [HttpGet("provinces/{id}")]
        public async Task<ActionResult<ProvinceDTO>> GetProvinceById(int id)
        {
            try
            {
                var province = await _locationService.GetProvinceByIdAsync(id);

                if (province == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Province not found"
                    });
                }

                return Ok(province);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create a new province
        /// </summary>
        /// <param name="request">Province creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("provinces")]
        public async Task<ActionResult<ApiResponseDTO>> CreateProvince([FromBody] CreateProvinceDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _locationService.CreateProvinceAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetProvinceById), new { id = 0 }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }
        #endregion

        #region Cantons
        /// <summary>
        /// Get cantons by province ID
        /// </summary>
        /// <param name="provinceId">Province ID</param>
        /// <returns>List of cantons in the province</returns>
        [HttpGet("cantons/by-province/{provinceId}")]
        public async Task<ActionResult<IEnumerable<CantonDTO>>> GetCantonsByProvince(int provinceId)
        {
            try
            {
                var cantons = await _locationService.GetCantonsByProvinceAsync(provinceId);
                return Ok(cantons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get canton by ID
        /// </summary>
        /// <param name="id">Canton ID</param>
        /// <returns>Canton information</returns>
        [HttpGet("cantons/{id}")]
        public async Task<ActionResult<CantonDTO>> GetCantonById(int id)
        {
            try
            {
                var canton = await _locationService.GetCantonByIdAsync(id);

                if (canton == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Canton not found"
                    });
                }

                return Ok(canton);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create a new canton
        /// </summary>
        /// <param name="request">Canton creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("cantons")]
        public async Task<ActionResult<ApiResponseDTO>> CreateCanton([FromBody] CreateCantonDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _locationService.CreateCantonAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetCantonById), new { id = 0 }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }
        #endregion

        #region Districts
        /// <summary>
        /// Get districts by canton ID
        /// </summary>
        /// <param name="cantonId">Canton ID</param>
        /// <returns>List of districts in the canton</returns>
        [HttpGet("districts/by-canton/{cantonId}")]
        public async Task<ActionResult<IEnumerable<DistrictDTO>>> GetDistrictsByCanton(int cantonId)
        {
            try
            {
                var districts = await _locationService.GetDistrictsByCantonAsync(cantonId);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get district by ID
        /// </summary>
        /// <param name="id">District ID</param>
        /// <returns>District information</returns>
        [HttpGet("districts/{id}")]
        public async Task<ActionResult<DistrictDTO>> GetDistrictById(int id)
        {
            try
            {
                var district = await _locationService.GetDistrictByIdAsync(id);

                if (district == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "District not found"
                    });
                }

                return Ok(district);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create a new district
        /// </summary>
        /// <param name="request">District creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("districts")]
        public async Task<ActionResult<ApiResponseDTO>> CreateDistrict([FromBody] CreateDistrictDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _locationService.CreateDistrictAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetDistrictById), new { id = 0 }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }
        #endregion
    }
}