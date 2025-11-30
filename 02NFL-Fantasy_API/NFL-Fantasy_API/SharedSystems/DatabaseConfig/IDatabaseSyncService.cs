namespace NFL_Fantasy_API.SharedSystems.DatabaseConfig
{
    /// <summary>
    /// Contrato para poder disparar manualmente una sincronización SectorB -> SectorA.
    /// La misma implementación es usada por el BackgroundService y por el controlador.
    /// </summary>
    public interface IDatabaseSyncService
    {
        /// <summary>
        /// Ejecuta inmediatamente un ciclo de sincronización SectorB -> SectorA.
        /// </summary>
        Task TriggerSyncAsync(CancellationToken cancellationToken = default);
    }
}
