namespace NFL_Fantasy_API.SharedSystems.DatabaseConfig
{
    /// <summary>
    /// Configuración para replicación de bases de datos en dos sectores.
    /// </summary>
    public class DatabaseReplicationSettings
    {
        /// <summary>
        /// Habilita o deshabilita la replicación.
        /// </summary>
        public bool EnableReplication { get; set; } = true;

        /// <summary>
        /// Intervalo en minutos para sincronización automática.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Sector primario para lectura (A o B).
        /// </summary>
        public string PrimaryReadSector { get; set; } = "A";
    }
}