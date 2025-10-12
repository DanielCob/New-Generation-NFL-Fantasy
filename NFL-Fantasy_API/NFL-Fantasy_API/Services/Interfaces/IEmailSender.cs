using System.Threading;
using System.Threading.Tasks;

namespace NFL_Fantasy_API.Services.Interfaces
{
    /// <summary>
    /// Contrato para componentes de envío de correo electrónico.
    /// Implementaciones típicas: SMTP, proveedores externos (SendGrid, SES, etc.).
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Envía un correo electrónico.
        /// </summary>
        /// <param name="to">Dirección de destino.</param>
        /// <param name="subject">Asunto del correo.</param>
        /// <param name="htmlBody">Contenido en HTML del mensaje.</param>
        /// <param name="textBody">Contenido en texto plano (opcional).</param>
        /// <param name="ct">Token de cancelación.</param>
        Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
    }
}
