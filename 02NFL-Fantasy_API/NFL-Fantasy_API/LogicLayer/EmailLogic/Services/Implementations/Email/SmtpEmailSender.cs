using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Interfaces.Email;
using NFL_Fantasy_API.SharedSystems.EmailConfig;

namespace NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Implementations.Email
{
    /// <summary>
    /// Implementación de <see cref="IEmailSender"/> usando SMTP estándar (.NET SmtpClient).
    /// 
    /// CARACTERÍSTICAS:
    /// - Compatible con cualquier proveedor SMTP (SendGrid, Gmail, Office365, etc.)
    /// - Soporte de HTML + texto plano alternativo
    /// - Configuración mediante Options Pattern
    /// 
    /// VENTAJAS:
    /// ✅ Funciona con cualquier servidor SMTP
    /// ✅ No requiere librerías externas
    /// ✅ Configuración simple y directa
    /// ✅ Soporte de STARTTLS/SSL
    /// 
    /// LIMITACIONES:
    /// ⚠️ SmtpClient está en modo mantenimiento (Microsoft recomienda MailKit)
    /// ⚠️ No tiene retry automático ante fallos
    /// ⚠️ No soporta templates avanzados
    /// ⚠️ Sin analytics ni tracking de emails
    /// 
    /// MEJORAS FUTURAS:
    /// 📋 Migrar a MailKit (biblioteca moderna recomendada por Microsoft)
    /// 📋 Implementar retry logic con Polly
    /// 📋 Agregar logging detallado de envíos
    /// 📋 Validar formato de email antes de enviar
    /// 📋 Queue de emails con background worker
    /// 
    /// PROVEEDORES SMTP COMUNES:
    /// - SendGrid: smtp.sendgrid.net:587 (user: "apikey", pass: API Key)
    /// - Gmail: smtp.gmail.com:587 (requiere App Password)
    /// - Office365: smtp-mail.outlook.com:587
    /// - Mailgun: smtp.mailgun.org:587
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        /// <summary>
        /// Constructor con inyección de dependencias.
        /// </summary>
        /// <param name="settings">Configuración SMTP desde appsettings.json</param>
        /// <param name="logger">Logger para diagnóstico y auditoría</param>
        public SmtpEmailSender(
            IOptions<SmtpSettings> settings,
            ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Validar configuración al inicializar
            ValidateConfiguration();
        }

        /// <inheritdoc />
        public async Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            string? textBody = null,
            CancellationToken ct = default)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(to))
                {
                    throw new ArgumentException("El destinatario no puede estar vacío.", nameof(to));
                }

                if (string.IsNullOrWhiteSpace(subject))
                {
                    throw new ArgumentException("El asunto no puede estar vacío.", nameof(subject));
                }

                if (string.IsNullOrWhiteSpace(htmlBody))
                {
                    throw new ArgumentException("El cuerpo del email no puede estar vacío.", nameof(htmlBody));
                }

                _logger.LogInformation(
                    "Iniciando envío de email a {To} con asunto: {Subject}",
                    to,
                    subject
                );

                // Crear mensaje de correo
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromAddress, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                // Agregar destinatario
                message.To.Add(to);

                // Agregar vista alternativa en texto plano (fallback para clientes sin HTML)
                if (!string.IsNullOrWhiteSpace(textBody))
                {
                    var plainView = AlternateView.CreateAlternateViewFromString(
                        textBody,
                        null,
                        "text/plain"
                    );
                    message.AlternateViews.Add(plainView);
                }

                // Configurar cliente SMTP
                using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.UseStartTls,
                    Credentials = new NetworkCredential(_settings.User, _settings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 segundos timeout
                };

                // Enviar correo
                // NOTA: SmtpClient.SendMailAsync() no tiene overload con CancellationToken
                // Para versiones futuras, considerar migrar a MailKit
                await smtpClient.SendMailAsync(message).ConfigureAwait(false);

                _logger.LogInformation(
                    "Email enviado exitosamente a {To}",
                    to
                );
            }
            catch (SmtpException ex)
            {
                _logger.LogError(
                    ex,
                    "Error SMTP al enviar email a {To}: {Message}",
                    to,
                    ex.Message
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al enviar email a {To}",
                    to
                );
                throw;
            }
        }

        /// <summary>
        /// Valida que la configuración SMTP sea correcta al inicializar el servicio.
        /// </summary>
        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                errors.Add("Host SMTP no configurado.");
            }

            if (_settings.Port <= 0 || _settings.Port > 65535)
            {
                errors.Add($"Puerto SMTP inválido: {_settings.Port}");
            }

            if (string.IsNullOrWhiteSpace(_settings.User))
            {
                errors.Add("Usuario SMTP no configurado.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                errors.Add("Password SMTP no configurado.");
            }

            if (string.IsNullOrWhiteSpace(_settings.FromAddress))
            {
                errors.Add("FromAddress no configurado.");
            }

            if (errors.Any())
            {
                var errorMessage = string.Join(" ", errors);
                _logger.LogError("Configuración SMTP inválida: {Errors}", errorMessage);
                throw new InvalidOperationException($"Configuración SMTP inválida: {errorMessage}");
            }

            _logger.LogInformation(
                "Configuración SMTP validada correctamente: {Host}:{Port}",
                _settings.Host,
                _settings.Port
            );
        }
    }
}