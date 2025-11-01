using NFL_Fantasy_API.SharedSystems.Validators;
using Microsoft.Data.SqlClient;
using NFL_Fantasy_API.Models.DTOs.NflDetails;
using NFL_Fantasy_API.Models.ViewModels.NflDetails;
using NFL_Fantasy_API.DataAccessLayer.SqlDatabase.Implementations.NflDetails;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.NflDetails;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Implementations.NflDetails
{
    /// <summary>
    /// Implementación del servicio de gestión de temporadas NFL.
    /// RESPONSABILIDAD: Lógica de negocio y orquestación.
    /// NO construye parámetros SQL (delegado a SeasonDataAccess).
    /// </summary>
    public class SeasonService : ISeasonService
    {
        private readonly SeasonDataAccess _dataAccess;
        private readonly ILogger<SeasonService> _logger;

        public SeasonService(
            SeasonDataAccess dataAccess,
            IConfiguration configuration,
            ILogger<SeasonService> logger)
        {
            _dataAccess = dataAccess;
            _logger = logger;
        }

        #region Get Current Season

        /// <summary>
        /// Obtiene la temporada actual.
        /// VIEW: vw_CurrentSeason
        /// </summary>
        public async Task<SeasonVM?> GetCurrentSeasonAsync()
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetCurrentSeasonAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener temporada actual");
                return null;
            }
        }

        #endregion

        #region Create Season

        /// <summary>
        /// Crea una nueva temporada.
        /// SP: app.sp_CreateSeason
        /// </summary>
        public async Task<SeasonVM?> CreateSeasonAsync(
            CreateSeasonRequestDTO dto,
            int actorUserId,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a SeasonValidator
                var validationErrors = SeasonValidator.ValidateCreateSeason(dto);

                if (validationErrors.Any())
                {
                    _logger.LogWarning(
                        "Validación fallida al crear temporada: {Errors}",
                        string.Join(", ", validationErrors)
                    );
                    return null;
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.CreateSeasonAsync(
                    dto,
                    actorUserId,
                    ip,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} created Season {SeasonId} - {Label}",
                        actorUserId,
                        result.SeasonID,
                        result.Label
                    );
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al crear temporada: Actor={ActorUserId}",
                    actorUserId
                );
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear temporada: Actor={ActorUserId}",
                    actorUserId
                );
                return null;
            }
        }

        #endregion

        #region Update Season

        /// <summary>
        /// Actualiza una temporada existente.
        /// SP: app.sp_UpdateSeason
        /// </summary>
        public async Task<SeasonVM?> UpdateSeasonAsync(
            int seasonId,
            UpdateSeasonRequestDTO dto,
            int actorUserId,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a SeasonValidator
                var validationErrors = SeasonValidator.ValidateUpdateSeason(dto);

                if (validationErrors.Any())
                {
                    _logger.LogWarning(
                        "Validación fallida al actualizar temporada {SeasonId}: {Errors}",
                        seasonId,
                        string.Join(", ", validationErrors)
                    );
                    return null;
                }

                // EJECUCIÓN: Delegada a DataAccess
                var result = await _dataAccess.UpdateSeasonAsync(
                    seasonId,
                    dto,
                    actorUserId,
                    ip,
                    userAgent
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "User {ActorUserId} updated Season {SeasonId}",
                        actorUserId,
                        seasonId
                    );
                }

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al actualizar temporada {SeasonId}",
                    seasonId
                );
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar temporada {SeasonId}",
                    seasonId
                );
                return null;
            }
        }

        #endregion

        #region Deactivate Season

        /// <summary>
        /// Desactiva una temporada.
        /// SP: app.sp_DeactivateSeason
        /// </summary>
        public async Task<string> DeactivateSeasonAsync(
            int seasonId,
            bool confirm,
            int actorUserId,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a SeasonValidator
                var validationErrors = SeasonValidator.ValidateConfirmation(confirm, "desactivación");

                if (validationErrors.Any())
                {
                    var errorMessage = string.Join(", ", validationErrors);
                    _logger.LogWarning(
                        "Validación fallida al desactivar temporada {SeasonId}: {Errors}",
                        seasonId,
                        errorMessage
                    );
                    return errorMessage;
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.DeactivateSeasonAsync(
                    seasonId,
                    confirm,
                    actorUserId,
                    ip,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} deactivated Season {SeasonId}",
                    actorUserId,
                    seasonId
                );

                return message;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al desactivar temporada {SeasonId}",
                    seasonId
                );
                return $"Error al desactivar temporada: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al desactivar temporada {SeasonId}",
                    seasonId
                );
                return $"Error inesperado: {ex.Message}";
            }
        }

        #endregion

        #region Delete Season

        /// <summary>
        /// Elimina una temporada.
        /// SP: app.sp_DeleteSeason
        /// </summary>
        public async Task<string> DeleteSeasonAsync(
            int seasonId,
            bool confirm,
            int actorUserId,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                // VALIDACIÓN: Delegada a SeasonValidator
                var validationErrors = SeasonValidator.ValidateConfirmation(confirm, "eliminación");

                if (validationErrors.Any())
                {
                    var errorMessage = string.Join(", ", validationErrors);
                    _logger.LogWarning(
                        "Validación fallida al eliminar temporada {SeasonId}: {Errors}",
                        seasonId,
                        errorMessage
                    );
                    return errorMessage;
                }

                // EJECUCIÓN: Delegada a DataAccess
                var message = await _dataAccess.DeleteSeasonAsync(
                    seasonId,
                    confirm,
                    actorUserId,
                    ip,
                    userAgent
                );

                _logger.LogInformation(
                    "User {ActorUserId} deleted Season {SeasonId}",
                    actorUserId,
                    seasonId
                );

                return message;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error al eliminar temporada {SeasonId}",
                    seasonId
                );
                return $"Error al eliminar temporada: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar temporada {SeasonId}",
                    seasonId
                );
                return $"Error inesperado: {ex.Message}";
            }
        }

        #endregion

        #region Get Season Weeks

        /// <summary>
        /// Obtiene las semanas de una temporada.
        /// Query directa a league.SeasonWeek
        /// </summary>
        public async Task<List<SeasonWeekVM>> GetSeasonWeeksAsync(int seasonId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetSeasonWeeksAsync(seasonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener semanas de temporada {SeasonId}",
                    seasonId
                );
                return new List<SeasonWeekVM>();
            }
        }

        #endregion

        #region Get Season by ID

        /// <summary>
        /// Obtiene una temporada por ID.
        /// Query directa a league.Season
        /// </summary>
        public async Task<SeasonVM?> GetSeasonByIdAsync(int seasonId)
        {
            try
            {
                // EJECUCIÓN: Delegada a DataAccess
                return await _dataAccess.GetSeasonByIdAsync(seasonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener temporada {SeasonId}",
                    seasonId
                );
                return null;
            }
        }

        #endregion
    }
}