using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/system-roles")]
    public class SystemRolesController : ControllerBase
    {
        private readonly ISystemRolesService _svc;

        public SystemRolesController(ISystemRolesService svc) { _svc = svc; }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<SystemRoleDTO>>> GetRoles()
            => Ok(await _svc.GetRolesAsync());

        [HttpPut("users/{targetUserId:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> ChangeUserRole(
            [FromRoute] int targetUserId, [FromBody] ChangeUserSystemRoleDTO dto)
        {
            if (!HttpContext.IsAuthenticated())
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));

            try
            {
                var res = await _svc.ChangeUserRoleAsync(
                    HttpContext.GetUserId(),
                    targetUserId,
                    dto,
                    HttpContext.GetClientIpAddress(),
                    HttpContext.GetUserAgent()
                );

                return Ok(ApiResponseDTO.SuccessResponse($"Rol actualizado a {res.NewRoleCode}.", res));
            }
            catch (SqlException ex)
            {
                // Devuelve el mensaje del THROW del SP como 400 en vez de 500
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error interno al cambiar rol."));
            }
        }

        // using Microsoft.Data.SqlClient;
        [HttpGet("users/{userId:int}/changes")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> GetUserRoleChanges([FromRoute] int userId, [FromQuery] int top = 50)
        {
            if (!HttpContext.IsAuthenticated())
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));

            try
            {
                var actorUserId = HttpContext.GetUserId(); // <-- ACTOR correcto
                var rows = await _svc.GetUserRoleChangesAsync(actorUserId, userId, top);
                return Ok(ApiResponseDTO.SuccessResponse("Historial obtenido.", rows));
            }
            catch (SqlException ex) when (ex.Number == 50210 || ex.Number == 50220)
            {
                // Mensaje del SP: Solo un ADMIN...
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (SqlException ex)
            {
                return BadRequest(ApiResponseDTO.ErrorResponse(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error interno al obtener historial."));
            }
        }
    }

}
