using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Configuración de SMTP obtenida desde appsettings (sección "Smtp").
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>Host del servidor SMTP (ej.: smtp.sendgrid.net).</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>Puerto SMTP (típicamente 587 para STARTTLS o 465 para SSL).</summary>
        public int Port { get; set; } = 587;

        /// <summary>Usuario/credencial SMTP (puede ser "apikey" según el proveedor).</summary>
        public string User { get; set; } = string.Empty;

        /// <summary>Password o API Key del proveedor SMTP.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Dirección del remitente que verán los usuarios.</summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>Nombre del remitente (branding en el buzón del usuario).</summary>
        public string FromName { get; set; } = "X-NFL Fantasy API";

        /// <summary>Si se debe usar STARTTLS (EnableSsl en SmtpClient).</summary>
        public bool UseStartTls { get; set; } = true;
    }

    /// <summary>
    /// Implementación de <see cref="IEmailSender"/> basada en <see cref="SmtpClient"/>.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _cfg;

        /// <summary>
        /// Crea una instancia de <see cref="SmtpEmailSender"/>.
        /// </summary>
        /// <param name="cfg">Opciones de SMTP enlazadas desde configuración.</param>
        public SmtpEmailSender(IOptions<SmtpSettings> cfg)
        {
            _cfg = cfg.Value;
        }

        /// <inheritdoc />
        public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(_cfg.FromAddress, _cfg.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            msg.To.Add(to);

            // Adjuntar alternativa en texto plano si se provee
            if (!string.IsNullOrWhiteSpace(textBody))
            {
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
            }

            using var client = new SmtpClient(_cfg.Host, _cfg.Port)
            {
                EnableSsl = _cfg.UseStartTls,
                Credentials = new NetworkCredential(_cfg.User, _cfg.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            // No hay overload async con CancellationToken; en .NET este patrón es el disponible.
            await client.SendMailAsync(msg).ConfigureAwait(false);
        }
    }
}
