using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.LogicLayer.GameLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;
using NFL_Fantasy_API.SharedSystems.Security.Extensions;

namespace NFL_Fantasy_API.LogicLayer.GameLogic.Controllers.Auth
{
    /// <summary>
    /// Controller para gestión de roles del sistema.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para roles
    /// - Obtener lista de roles disponibles
    /// - Cambiar rol de un usuario
    /// - Consultar historial de cambios de rol
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren rol ADMIN
    /// - Las validaciones de permisos se hacen en el servicio
    /// - Auditoría automática de cambios
    /// 
    /// ENDPOINTS:
    /// - GET /api/system-roles - Lista de roles del sistema
    /// - PUT /api/system-roles/users/{targetUserId} - Cambiar rol de usuario
    /// - GET /api/system-roles/users/{userId}/changes - Historial de cambios
    /// </summary>
    [ApiController]
    [Route("api/system-roles")]
    [Authorize(Policy = "AdminOnly")] // Aplicado a TODOS los endpoints del controller
    public class SystemRolesController : ControllerBase
    {
        private readonly ISystemRolesService _systemRolesService;
        private readonly ILogger<SystemRolesController> _logger;

        public SystemRolesController(
            ISystemRolesService systemRolesService,
            ILogger<SystemRolesController> logger)
        {
            _systemRolesService = systemRolesService;
            _logger = logger;
        }

        /// <summary>
        /// Cambia el rol de sistema de un usuario.
        /// PUT /api/system-roles/users/{targetUserId}
        /// </summary>
        /// <param name="targetUserId">ID del usuario a modificar</param>
        /// <param name="dto">Nuevo rol a asignar</param>
        /// <returns>ApiResponseDTO con el resultado del cambio</returns>
        /// <response code="200">Rol cambiado exitosamente</response>
        /// <response code="400">Rol inválido o usuario no existe</response>
        /// <response code="403">Sin permisos o intento de modificar super admin</response>
        /// <remarks>
        /// REGLAS DE NEGOCIO:
        /// - Solo ADMIN puede cambiar roles
        /// - Se registra auditoría del cambio
        /// </remarks>
        [HttpPut("users/{targetUserId:int}")]
        public async Task<ActionResult<ApiResponseDTO>> ChangeUserRole(
            [FromRoute] int targetUserId,
            [FromBody] ChangeUserSystemRoleDTO dto)
        {
            var actorUserId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _systemRolesService.ChangeUserRoleAsync(
                actorUserId,
                targetUserId,
                dto,
                sourceIp,
                userAgent
            );
            if (result is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo cambiar el rol del usuario."));

            _logger.LogInformation(
                "Admin {ActorUserId} changed role of User {TargetUserId} to {NewRole} from {IP}",
                actorUserId,
                targetUserId,
                result.NewRoleCode,
                sourceIp
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                $"Rol actualizado exitosamente a {result.NewRoleCode}.",
                result
            ));
        }

        /// <summary>
        /// Obtiene el historial de cambios de rol de un usuario.
        /// GET /api/system-roles/users/{userId}/changes
        /// </summary>
        /// <param name="userId">ID del usuario del cual consultar historial</param>
        /// <param name="top">Número máximo de registros a retornar (default: 50, max: 200)</param>
        /// <returns>Lista de cambios de rol ordenados por fecha descendente</returns>
        /// <response code="200">Historial obtenido exitosamente</response>
        /// <response code="403">Sin permisos para ver el historial</response>
        /// <remarks>
        /// NOTAS:
        /// - Solo ADMIN puede consultar historiales
        /// - Se retornan los cambios más recientes primero
        /// - Incluye: fecha, rol anterior, rol nuevo, quién hizo el cambio
        /// </remarks>
        [HttpGet("users/{userId:int}/changes")]
        public async Task<ActionResult<ApiResponseDTO>> GetUserRoleChanges(
            [FromRoute] int userId,
            [FromQuery] int top = 50)
        {
            // Validar rango de 'top'
            if (top < 1) top = 50;
            if (top > 200) top = 200;

            var actorUserId = this.UserId();

            var changes = await _systemRolesService.GetUserRoleChangesAsync(
                actorUserId,
                userId,
                top
            );
            if (changes is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo obtener el historial de cambios de rol."));

            _logger.LogInformation(
                "Admin {ActorUserId} consulted role change history for User {UserId}",
                actorUserId,
                userId
            );

            return Ok(ApiResponseDTO.SuccessResponse(
                "Historial de cambios de rol obtenido exitosamente.",
                changes
            ));
        }

        /// <summary>
        /// Obtiene lista de roles del sistema disponibles.
        /// GET /api/system-roles
        /// </summary>
        /// <returns>Lista de roles del sistema</returns>
        /// <response code="200">Roles obtenidos exitosamente</response>
        /// <remarks>
        /// ROLES DEL SISTEMA:
        /// - USER: Usuario estándar
        /// - ADMIN: Administrador del sistema
        /// - BRAND_MANAGER: Manager de marca (futuro)
        /// 
        /// Usado para dropdowns y selección de roles en la UI.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<ApiResponseDTO>> GetSystemRoles()
        {
            var roles = await _systemRolesService.GetSystemRolesAsync();
            if (roles is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudieron obtener los roles del sistema."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Roles del sistema obtenidos exitosamente.",
                roles
            ));
        }

        /// <summary>
        /// Obtiene lista completa de usuarios con sus roles y estadísticas.
        /// GET /api/system-roles/users
        /// </summary>
        /// <returns>Lista de usuarios con roles completos</returns>
        /// <response code="200">Usuarios obtenidos exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Solo para usuarios ADMIN.
        /// 
        /// INCLUYE:
        /// - Información básica del usuario
        /// - Rol del sistema asignado
        /// - Estadísticas (ligas, equipos, etc.)
        /// - Estado de cuenta
        /// 
        /// Usado para panel de administración de usuarios.
        /// </remarks>
        [HttpGet("users")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponseDTO>> GetUsersWithRoles()
        {
            var users = await _systemRolesService.GetUsersWithRolesAsync();
            if (users is null) return BadRequest(ApiResponseDTO.ErrorResponse("No se pudo obtener la lista de usuarios con roles."));

            return Ok(ApiResponseDTO.SuccessResponse(
                "Usuarios con roles obtenidos exitosamente.",
                users
            ));
        }
    }
}