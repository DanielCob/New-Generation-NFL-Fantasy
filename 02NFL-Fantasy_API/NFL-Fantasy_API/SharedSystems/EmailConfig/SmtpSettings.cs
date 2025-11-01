// SharedSystems/Email/Configuration/SmtpSettings.cs
namespace NFL_Fantasy_API.SharedSystems.EmailConfig
{
    /// <summary>
    /// Configuración de SMTP obtenida desde appsettings.json (sección "Smtp").
    /// 
    /// CONFIGURACIÓN EN appsettings.json:
    /// {
    ///   "Smtp": {
    ///     "Host": "smtp.sendgrid.net",
    ///     "Port": 587,
    ///     "User": "apikey",
    ///     "Password": "SG.xxxxxxxxxxxxx",
    ///     "FromAddress": "noreply@nflfantasy.com",
    ///     "FromName": "NFL Fantasy League",
    ///     "UseStartTls": true
    ///   }
    /// }
    /// 
    /// REGISTRO EN Program.cs:
    /// builder.Services.Configure&lt;SmtpSettings&gt;(builder.Configuration.GetSection("Smtp"));
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Host del servidor SMTP.
        /// Ejemplos: smtp.sendgrid.net, smtp.gmail.com, smtp-mail.outlook.com
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// Puerto SMTP.
        /// - 587: STARTTLS (recomendado)
        /// - 465: SSL/TLS
        /// - 25: Sin encriptación (no recomendado)
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Usuario/credencial SMTP.
        /// - SendGrid: "apikey"
        /// - Gmail: tu email completo
        /// - Office365: tu email completo
        /// </summary>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// Password o API Key del proveedor SMTP.
        /// ⚠️ NUNCA hardcodear en código, usar User Secrets o Variables de Entorno.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Dirección de correo del remitente (visible para los usuarios).
        /// Ejemplo: noreply@nflfantasy.com
        /// </summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del remitente (branding en el buzón del usuario).
        /// Ejemplo: "NFL Fantasy League"
        /// </summary>
        public string FromName { get; set; } = "NFL Fantasy API";

        /// <summary>
        /// Habilitar STARTTLS (EnableSsl en SmtpClient).
        /// Debe ser true para puerto 587, false para puerto 25.
        /// </summary>
        public bool UseStartTls { get; set; } = true;
    }
}