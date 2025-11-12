namespace NFL_Fantasy_API.LogicLayer.EmailLogic.Services.Interfaces.Email
{
    /// <summary>
    /// Contrato para servicios de envío de correos electrónicos.
    /// 
    /// PROPÓSITO:
    /// - Abstracción que permite cambiar proveedores sin afectar lógica de negocio
    /// - Facilita testing con implementaciones mock
    /// - Centraliza la lógica de envío de emails
    /// 
    /// IMPLEMENTACIONES DISPONIBLES:
    /// - SmtpEmailSender: Envío vía SMTP estándar (actual)
    /// 
    /// IMPLEMENTACIONES FUTURAS:
    /// - SendGridEmailSender: API REST de SendGrid
    /// - MailgunEmailSender: API REST de Mailgun
    /// - MailKitEmailSender: Alternativa moderna a SmtpClient
    /// 
    /// CASOS DE USO:
    /// - Verificación de email al registrarse
    /// - Códigos de autenticación (2FA)
    /// - Recuperación de contraseña
    /// - Notificaciones de ligas/equipos
    /// - Alertas de seguridad
    /// - Invitaciones a ligas
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Envía un correo electrónico de forma asíncrona.
        /// </summary>
        /// <param name="to">
        /// Dirección de correo del destinatario.
        /// Debe ser un email válido (se recomienda validar antes de llamar).
        /// </param>
        /// <param name="subject">
        /// Asunto del correo.
        /// Debe ser descriptivo y no exceder 100 caracteres.
        /// </param>
        /// <param name="htmlBody">
        /// Cuerpo del correo en formato HTML.
        /// Se recomienda usar EmailTemplates para consistencia visual.
        /// </param>
        /// <param name="textBody">
        /// Cuerpo alternativo en texto plano (opcional pero recomendado).
        /// Fallback para clientes de correo que no soportan HTML.
        /// </param>
        /// <param name="ct">Token de cancelación para operaciones largas.</param>
        /// <returns>Task que representa la operación asíncrona de envío.</returns>
        /// <exception cref="SmtpException">Error al conectar o autenticar con el servidor SMTP.</exception>
        /// <exception cref="ArgumentException">Parámetros inválidos (email mal formado, etc.).</exception>
        /// <exception cref="InvalidOperationException">Configuración SMTP incorrecta.</exception>
        /// <remarks>
        /// EJEMPLO DE USO:
        /// <code>
        /// var html = EmailTemplates.PasswordReset("Mi App", resetUrl, expiresAt);
        /// var text = EmailTemplates.PasswordResetPlainText("Mi App", resetUrl, expiresAt);
        /// await _emailSender.SendAsync(user.Email, "Restablecer contraseña", html, text);
        /// </code>
        /// </remarks>
        Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            string? textBody = null,
            CancellationToken ct = default);
    }
}