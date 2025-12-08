using Microsoft.Data.SqlClient;

namespace NFL_Fantasy_API.SharedSystems.DatabaseConfig
{
    /// <summary>
    /// Servicio que inicializa y crea las bases de datos sectorA y sectorB al arranque.
    /// Usa BACKUP/RESTORE para clonar completamente la base de datos original.
    /// </summary>
    public class DatabaseInitializationService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(
            IConfiguration configuration,
            ILogger<DatabaseInitializationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Iniciando configuración de replicación de bases de datos...");

            var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(defaultConnection))
            {
                throw new InvalidOperationException("DefaultConnection no encontrada en appsettings.json");
            }

            var builder = new SqlConnectionStringBuilder(defaultConnection);
            var serverName = builder.DataSource;
            var originalDbName = builder.InitialCatalog;

            var masterConnStr = new SqlConnectionStringBuilder(defaultConnection)
            {
                InitialCatalog = "master"
            }.ConnectionString;

            _logger.LogInformation("Servidor: {Server}", serverName);
            _logger.LogInformation("Base de datos original: {Database}", originalDbName);

            try
            {
                await CloneDatabaseUsingBackupRestoreAsync(masterConnStr, originalDbName, "sectorA", cancellationToken);
                await CloneDatabaseUsingBackupRestoreAsync(masterConnStr, originalDbName, "sectorB", cancellationToken);

                _logger.LogInformation("Replicación de bases de datos configurada exitosamente.");
                _logger.LogInformation("========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar la replicación de bases de datos");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deteniendo servicio de inicialización de bases de datos...");
            return Task.CompletedTask;
        }

        private async Task CloneDatabaseUsingBackupRestoreAsync(
            string masterConnStr,
            string sourceDbName,
            string sector,
            CancellationToken cancellationToken)
        {
            var targetDbName = $"{sourceDbName}_{sector}";

            _logger.LogInformation("Verificando base de datos: {DbName}", targetDbName);

            using var connection = new SqlConnection(masterConnStr);
            await connection.OpenAsync(cancellationToken);

            // Verificar si ya existe
            var checkDbCmd = new SqlCommand(
                "SELECT database_id FROM sys.databases WHERE name = @DbName",
                connection);
            checkDbCmd.Parameters.AddWithValue("@DbName", targetDbName);

            var exists = await checkDbCmd.ExecuteScalarAsync(cancellationToken);

            if (exists != null)
            {
                _logger.LogInformation("Base de datos {DbName} ya existe. Omitiendo creación.", targetDbName);
                return;
            }

            _logger.LogInformation("Clonando base de datos de {Source} a {Target} usando BACKUP/RESTORE...", sourceDbName, targetDbName);

            // Obtener rutas de archivos de la DB original
            var filePathsQuery = @"
                SELECT 
                    physical_name,
                    type_desc,
                    name AS logical_name
                FROM sys.master_files
                WHERE database_id = DB_ID(@DbName)
            ";

            using var filePathsCmd = new SqlCommand(filePathsQuery, connection);
            filePathsCmd.Parameters.AddWithValue("@DbName", sourceDbName);

            string? dataFilePath = null;
            string? logFilePath = null;
            string? dataLogicalName = null;
            string? logLogicalName = null;

            using var reader = await filePathsCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var physicalName = reader.GetString(0);
                var typeDesc = reader.GetString(1);
                var logicalName = reader.GetString(2);

                if (typeDesc == "ROWS")
                {
                    dataFilePath = physicalName;
                    dataLogicalName = logicalName;
                }
                else if (typeDesc == "LOG")
                {
                    logFilePath = physicalName;
                    logLogicalName = logicalName;
                }
            }
            reader.Close();

            if (string.IsNullOrEmpty(dataFilePath) || string.IsNullOrEmpty(logFilePath))
            {
                throw new InvalidOperationException($"No se pudieron obtener las rutas de archivos de {sourceDbName}");
            }

            // Calcular rutas para el backup y archivos de la nueva DB
            var backupDir = Path.GetDirectoryName(dataFilePath) ?? "";
            var backupFile = Path.Combine(backupDir, $"{sourceDbName}_temp_backup.bak");

            var newDataFile = Path.Combine(backupDir, $"{targetDbName}.mdf");
            var newLogFile = Path.Combine(backupDir, $"{targetDbName}_log.ldf");

            try
            {
                // PASO 1: BACKUP de la base de datos original
                _logger.LogInformation("Creando backup temporal de {Source}...", sourceDbName);

                var backupSql = $@"
                    BACKUP DATABASE [{sourceDbName}] 
                    TO DISK = @BackupFile 
                    WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10
                ";

                using (var backupCmd = new SqlCommand(backupSql, connection))
                {
                    backupCmd.CommandTimeout = 600; // 10 minutos
                    backupCmd.Parameters.AddWithValue("@BackupFile", backupFile);
                    await backupCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogInformation("Backup creado: {BackupFile}", backupFile);

                // PASO 2: RESTORE a la nueva base de datos
                _logger.LogInformation("Restaurando como {Target}...", targetDbName);

                var restoreSql = $@"
                    RESTORE DATABASE [{targetDbName}]
                    FROM DISK = @BackupFile
                    WITH 
                        MOVE @DataLogicalName TO @NewDataFile,
                        MOVE @LogLogicalName TO @NewLogFile,
                        REPLACE, STATS = 10
                ";

                using (var restoreCmd = new SqlCommand(restoreSql, connection))
                {
                    restoreCmd.CommandTimeout = 600; // 10 minutos
                    restoreCmd.Parameters.AddWithValue("@BackupFile", backupFile);
                    restoreCmd.Parameters.AddWithValue("@DataLogicalName", dataLogicalName);
                    restoreCmd.Parameters.AddWithValue("@NewDataFile", newDataFile);
                    restoreCmd.Parameters.AddWithValue("@LogLogicalName", logLogicalName);
                    restoreCmd.Parameters.AddWithValue("@NewLogFile", newLogFile);
                    await restoreCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogInformation("✅ Base de datos {DbName} clonada exitosamente.", targetDbName);

                // PASO 3: Eliminar archivo de backup temporal
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                    _logger.LogDebug("Backup temporal eliminado: {BackupFile}", backupFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clonando base de datos {Target}", targetDbName);

                // Limpiar si falló
                try
                {
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }

                    var dropCmd = new SqlCommand($"DROP DATABASE IF EXISTS [{targetDbName}]", connection);
                    await dropCmd.ExecuteNonQueryAsync(cancellationToken);
                }
                catch { /* Ignorar errores al limpiar */ }

                throw;
            }
        }
    }
}