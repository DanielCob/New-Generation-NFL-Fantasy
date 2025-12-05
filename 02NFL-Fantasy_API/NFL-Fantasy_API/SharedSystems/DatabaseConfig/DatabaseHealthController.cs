using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Models.DTOs;

namespace NFL_Fantasy_API.SharedSystems.DatabaseConfig
{
    /// <summary>
    /// Controlador para diagnóstico y salud de la replicación de bases de datos.
    /// Solo accesible por administradores.
    /// </summary>
    [ApiController]
    [Route("api/database-health")]
    [Authorize(Policy = "AdminOnly")]
    public class DatabaseHealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHealthController> _logger;
        private readonly IDatabaseSyncService _databaseSyncService;

        public DatabaseHealthController(
            IConfiguration configuration,
            ILogger<DatabaseHealthController> logger,
            IDatabaseSyncService databaseSyncService)
        {
            _configuration = configuration;
            _logger = logger;
            _databaseSyncService = databaseSyncService;
        }

        /// <summary>
        /// Obtiene el estado de salud de ambos sectores de base de datos.
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ApiResponseDTO>> GetDatabaseHealthStatus()
        {
            try
            {
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(baseConnectionString))
                {
                    return BadRequest(ApiResponseDTO.ErrorResponse("DefaultConnection no configurada"));
                }

                var builder = new SqlConnectionStringBuilder(baseConnectionString);
                var originalDbName = builder.InitialCatalog;

                var sectorAStatus = await CheckSectorHealthAsync(baseConnectionString, originalDbName, "sectorA");
                var sectorBStatus = await CheckSectorHealthAsync(baseConnectionString, originalDbName, "sectorB");

                var healthData = new
                {
                    timestamp = DateTime.UtcNow,
                    replicationEnabled = _configuration.GetValue<bool>("DatabaseReplication:EnableReplication", true),
                    syncIntervalMinutes = _configuration.GetValue<int>("DatabaseReplication:SyncIntervalMinutes", 5),
                    primaryReadSector = _configuration.GetValue<string>("DatabaseReplication:PrimaryReadSector", "A"),
                    sectorA = sectorAStatus,
                    sectorB = sectorBStatus,
                    inSync = sectorAStatus.RowCount == sectorBStatus.RowCount
                };

                return Ok(ApiResponseDTO.SuccessResponse("Estado de replicación obtenido", healthData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado de salud de bases de datos");
                return StatusCode(500, ApiResponseDTO.ErrorResponse($"Error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Fuerza una sincronización manual inmediata.
        /// </summary>
        [HttpPost("force-sync")]
        public async Task<ActionResult<ApiResponseDTO>> ForceDatabaseSync()
        {
            try
            {
                _logger.LogInformation("Sincronización manual forzada por administrador");

                await _databaseSyncService.TriggerSyncAsync();

                return Ok(ApiResponseDTO.SuccessResponse(
                    "Sincronización ejecutada. Revisar logs para detalles.",
                    new { initiatedAt = DateTime.UtcNow }
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forzando sincronización");
                return StatusCode(500, ApiResponseDTO.ErrorResponse($"Error: {ex.Message}"));
            }
        }

        private async Task<SectorHealthStatus> CheckSectorHealthAsync(
        string baseConnectionString,
        string originalDbName,
        string sector)
        {
            var sectorDbName = $"{originalDbName}_{sector}";
            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = sectorDbName
            };

            try
            {
                using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();

                // Obtener conteo de registros en tablas críticas (usando esquemas correctos)
                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM auth.UserAccount) AS UserCount,
                        (SELECT COUNT(*) FROM league.League) AS LeagueCount,
                        (SELECT COUNT(*) FROM league.Team) AS TeamCount,
                        (SELECT COUNT(*) FROM ref.NFLPlayer) AS PlayerCount,
                        (SELECT COUNT(*) FROM audit.UserActionLog) AS AuditLogCount
                ";

                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = 30;
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var userCount = reader.GetInt32(0);
                    var leagueCount = reader.GetInt32(1);
                    var teamCount = reader.GetInt32(2);
                    var playerCount = reader.GetInt32(3);
                    var auditLogCount = reader.GetInt32(4);
                    var totalRows = userCount + leagueCount + teamCount + playerCount + auditLogCount;

                    return new SectorHealthStatus
                    {
                        DatabaseName = sectorDbName,
                        IsConnected = true,
                        UserCount = userCount,
                        LeagueCount = leagueCount,
                        TeamCount = teamCount,
                        PlayerCount = playerCount,
                        AuditLogCount = auditLogCount,
                        RowCount = totalRows,
                        LastChecked = DateTime.UtcNow
                    };
                }

                return new SectorHealthStatus
                {
                    DatabaseName = sectorDbName,
                    IsConnected = true,
                    RowCount = 0,
                    LastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando salud de {Sector}", sector);
                return new SectorHealthStatus
                {
                    DatabaseName = sectorDbName,
                    IsConnected = false,
                    Error = ex.Message,
                    LastChecked = DateTime.UtcNow
                };
            }
        }
    }

    /// <summary>
    /// DTO para el estado de salud de un sector de base de datos.
    /// </summary>
    public class SectorHealthStatus
    {
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public int UserCount { get; set; }
        public int LeagueCount { get; set; }
        public int TeamCount { get; set; }
        public int PlayerCount { get; set; }
        public int AuditLogCount { get; set; }
        public int RowCount { get; set; }
        public DateTime LastChecked { get; set; }
        public string? Error { get; set; }
    }
}