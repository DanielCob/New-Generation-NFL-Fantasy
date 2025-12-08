using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;

namespace NFL_Fantasy_API.SharedSystems.DatabaseConfig
{
    /// <summary>
    /// Servicio que sincroniza sectorB como copia EXACTA de sectorA mediante BACKUP/RESTORE.
    /// - TODA la app escribe y lee en {BaseDb}_sectorA.
    /// - Cada X minutos (o manualmente) clonamos {BaseDb}_sectorA -> {BaseDb}_sectorB.
    /// - Es ineficiente pero garantiza que sectorB es una foto exacta de sectorA.
    /// </summary>
    public class DatabaseSyncService : BackgroundService, IDatabaseSyncService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSyncService> _logger;
        private readonly TimeSpan _syncInterval;
        private readonly string _masterConnectionString;
        private readonly string _sourceDbName; // {BaseDb}_sectorA
        private readonly string _targetDbName; // {BaseDb}_sectorB
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private readonly bool _replicationEnabled;

        public DatabaseSyncService(
            IConfiguration configuration,
            ILogger<DatabaseSyncService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var intervalMinutes = configuration.GetValue<int>("DatabaseReplication:SyncIntervalMinutes", 5);
            _syncInterval = TimeSpan.FromMinutes(intervalMinutes);

            var baseConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection no encontrada");

            var builder = new SqlConnectionStringBuilder(baseConnectionString);
            var originalDbName = builder.InitialCatalog;

            _sourceDbName = $"{originalDbName}_sectorA";
            _targetDbName = $"{originalDbName}_sectorB";

            var masterBuilder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = "master"
            };
            _masterConnectionString = masterBuilder.ConnectionString;

            _replicationEnabled = configuration.GetValue<bool>("DatabaseReplication:EnableReplication", true);

            _logger.LogInformation(
                "DatabaseSyncService inicializado. Intervalo: {Interval} minutos. Replicación: {Enabled}. Origen: {Source}, Destino: {Target}",
                intervalMinutes,
                _replicationEnabled ? "HABILITADA" : "DESHABILITADA",
                _sourceDbName,
                _targetDbName);
        }

        /// <summary>
        /// Llamado por el controlador para una sincronización manual inmediata.
        /// </summary>
        public async Task TriggerSyncAsync(CancellationToken cancellationToken = default)
        {
            await RunSyncInternalAsync("MANUAL", cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_replicationEnabled)
            {
                _logger.LogInformation(
                    "DatabaseSyncService: replicación deshabilitada por configuración. No se ejecutarán syncs periódicos.");
                return;
            }

            _logger.LogInformation("DatabaseSyncService iniciado. Primera sincronización en {Interval}.", _syncInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await RunSyncInternalAsync("TIMER", stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("DatabaseSyncService detenido.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la sincronización de bases de datos");
                }
            }
        }

        private async Task RunSyncInternalAsync(string source, CancellationToken cancellationToken)
        {
            // Evitar syncs concurrentes
            if (!await _syncLock.WaitAsync(TimeSpan.Zero, cancellationToken))
            {
                _logger.LogWarning(
                    "Se intentó iniciar una sincronización {Source} mientras otra estaba en curso. Se ignora.",
                    source);
                return;
            }

            try
            {
                _logger.LogInformation("========================================");
                _logger.LogInformation(
                    "Iniciando sincronización {Source} de bases de datos (A -> B con BACKUP/RESTORE)...",
                    source == "TIMER" ? "periódica" : "manual");

                await SynchronizeDatabasesAsync(cancellationToken);

                _logger.LogInformation("Sincronización {Source} completada exitosamente.",
                    source == "TIMER" ? "periódica" : "manual");
                _logger.LogInformation("========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización {Source} de bases de datos",
                    source == "TIMER" ? "periódica" : "manual");
                throw;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Clona sectorA -> sectorB mediante BACKUP y RESTORE con REPLACE.
        /// </summary>
        private async Task SynchronizeDatabasesAsync(CancellationToken cancellationToken)
        {
            await CloneDatabaseUsingBackupRestoreAsync(
                _masterConnectionString,
                _sourceDbName,
                _targetDbName,
                cancellationToken);
        }

        /// <summary>
        /// Hace BACKUP de sourceDbName y RESTORE sobre targetDbName (REPLACE).
        /// </summary>
        private async Task CloneDatabaseUsingBackupRestoreAsync(
            string masterConnStr,
            string sourceDbName,
            string targetDbName,
            CancellationToken cancellationToken)
        {
            using var connection = new SqlConnection(masterConnStr);
            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Clonando {SourceDb} -> {TargetDb} (BACKUP/RESTORE completo)...",
                sourceDbName, targetDbName);

            // 1. Obtener info de archivos de la DB origen
            var filePathsQuery = @"
                SELECT 
                    physical_name,
                    type_desc,
                    name AS logical_name
                FROM sys.master_files
                WHERE database_id = DB_ID(@SourceDbName)
            ";

            string? dataFilePath = null;
            string? logFilePath = null;
            string? dataLogicalName = null;
            string? logLogicalName = null;

            using (var filePathsCmd = new SqlCommand(filePathsQuery, connection))
            {
                filePathsCmd.Parameters.AddWithValue("@SourceDbName", sourceDbName);

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
            }

            if (string.IsNullOrEmpty(dataFilePath) || string.IsNullOrEmpty(logFilePath))
            {
                throw new InvalidOperationException(
                    $"No se pudieron obtener las rutas de archivos de la base de datos origen {sourceDbName}");
            }

            var backupDir = Path.GetDirectoryName(dataFilePath) ?? "";
            var backupFile = Path.Combine(backupDir, $"{sourceDbName}_to_{targetDbName}_sync.bak");
            var newDataFile = Path.Combine(backupDir, $"{targetDbName}.mdf");
            var newLogFile = Path.Combine(backupDir, $"{targetDbName}_log.ldf");

            try
            {
                // 2. Si existe el target, ponerlo en SINGLE_USER para poder reemplazarlo
                var checkDbCmd = new SqlCommand(
                    "SELECT database_id FROM sys.databases WHERE name = @DbName",
                    connection);
                checkDbCmd.Parameters.AddWithValue("@DbName", targetDbName);
                var exists = await checkDbCmd.ExecuteScalarAsync(cancellationToken);

                if (exists != null)
                {
                    var setSingleUserSql =
                        $"ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                    using var singleCmd = new SqlCommand(setSingleUserSql, connection)
                    {
                        CommandTimeout = 300
                    };
                    await singleCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // 3. BACKUP de sectorA
                _logger.LogInformation("Creando backup temporal de {SourceDb} en {BackupFile}...",
                    sourceDbName, backupFile);

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

                // 4. RESTORE sobre sectorB con REPLACE
                _logger.LogInformation("Restaurando backup sobre {TargetDb}...", targetDbName);

                var restoreSql = @"
                    RESTORE DATABASE [" + targetDbName + @"]
                    FROM DISK = @BackupFile
                    WITH 
                        MOVE @DataLogicalName TO @NewDataFile,
                        MOVE @LogLogicalName TO @NewLogFile,
                        REPLACE, STATS = 10
                ";

                using (var restoreCmd = new SqlCommand(restoreSql, connection))
                {
                    restoreCmd.CommandTimeout = 600;
                    restoreCmd.Parameters.AddWithValue("@BackupFile", backupFile);
                    restoreCmd.Parameters.AddWithValue("@DataLogicalName", dataLogicalName);
                    restoreCmd.Parameters.AddWithValue("@NewDataFile", newDataFile);
                    restoreCmd.Parameters.AddWithValue("@LogLogicalName", logLogicalName);
                    restoreCmd.Parameters.AddWithValue("@NewLogFile", newLogFile);
                    await restoreCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                // 5. Volver a MULTI_USER
                var setMultiUserSql = $"ALTER DATABASE [{targetDbName}] SET MULTI_USER;";
                using (var multiCmd = new SqlCommand(setMultiUserSql, connection)
                {
                    CommandTimeout = 300
                })
                {
                    await multiCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogInformation("✅ Clonación {SourceDb} -> {TargetDb} completada correctamente.",
                    sourceDbName, targetDbName);

                // 6. Limpiar backup temporal
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                    _logger.LogDebug("Backup temporal eliminado: {BackupFile}", backupFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clonando base de datos {SourceDb} -> {TargetDb}",
                    sourceDbName, targetDbName);

                try
                {
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }
                }
                catch
                {
                    // ignorar errores al borrar el backup
                }

                throw;
            }
        }
    }
}
