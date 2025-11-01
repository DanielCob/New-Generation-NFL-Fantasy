// SharedSystems/Email/Templates/EmailTemplates.cs
namespace NFL_Fantasy_API.SharedSystems.EmailConfig
{
    /// <summary>
    /// Plantillas HTML centralizadas para correos transaccionales.
    /// 
    /// PROPÓSITO:
    /// - Centralizar diseño y branding de emails
    /// - Facilitar mantenimiento (un solo lugar para cambios visuales)
    /// - Garantizar consistencia en todas las comunicaciones
    /// - Separar presentación de lógica de envío
    /// 
    /// CARACTERÍSTICAS:
    /// - Diseño responsive (mobile-friendly)
    /// - Estilos inline (compatibilidad con clientes de email)
    /// - Versiones HTML + texto plano para cada template
    /// - Branding centralizado
    /// 
    /// USO TÍPICO:
    /// <code>
    /// var html = EmailTemplates.PasswordReset("Mi App", resetUrl, DateTime.UtcNow.AddHours(1));
    /// var text = EmailTemplates.PasswordResetPlainText("Mi App", resetUrl, DateTime.UtcNow.AddHours(1));
    /// await _emailSender.SendAsync(user.Email, "Restablecer contraseña", html, text);
    /// </code>
    /// 
    /// MEJORAS FUTURAS:
    /// 📋 Usar Razor templates para mayor flexibilidad
    /// 📋 Soporte de múltiples idiomas (i18n)
    /// 📋 Templates editables desde base de datos (admin panel)
    /// 📋 Variables de personalización avanzadas
    /// 📋 A/B testing de templates
    /// 📋 Previsualización en tiempo real
    /// </summary>
    public static class EmailTemplates
    {
        #region Shared Styles & Layout

        /// <summary>
        /// Estilos CSS inline compartidos.
        /// NOTA: Los clientes de email requieren estilos inline, no pueden usar hojas CSS externas.
        /// </summary>
        private const string BaseStyles = @"
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            color: #1f2937;
            background: #ffffff;
            margin: 0;
            padding: 24px;
            line-height: 1.6;
        ";

        private const string ContainerStyles = @"
            max-width: 600px;
            margin: 0 auto;
            background: #ffffff;
        ";

        private const string ButtonStyles = @"
            display: inline-block;
            background: #2563eb;
            color: #ffffff !important;
            padding: 14px 28px;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            font-size: 16px;
        ";

        private const string FooterStyles = @"
            font-size: 12px;
            color: #6b7280;
            text-align: center;
            margin-top: 32px;
            padding-top: 24px;
            border-top: 1px solid #e5e7eb;
        ";

        /// <summary>
        /// Layout base para todos los correos.
        /// Garantiza consistencia visual y branding.
        /// </summary>
        private static string WrapInLayout(string appName, string title, string content)
        {
            return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <title>{title}</title>
</head>
<body style=""{BaseStyles}"">
    <div style=""{ContainerStyles}"">
        <!-- Header -->
        <div style=""background: #1f2937; padding: 24px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">{appName}</h1>
        </div>
        
        <!-- Content -->
        <div style=""padding: 32px 24px;"">
            <h2 style=""margin: 0 0 24px 0; color: #111827; font-size: 20px;"">{title}</h2>
            {content}
        </div>
        
        <!-- Footer -->
        <div style=""{FooterStyles}"">
            <p style=""margin: 0 0 8px 0;"">
                © {DateTime.UtcNow.Year} {appName}. Todos los derechos reservados.
            </p>
            <p style=""margin: 0; color: #9ca3af;"">
                Este es un correo automático, por favor no responder.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion

        #region Password Reset

        /// <summary>
        /// Plantilla HTML de restablecimiento de contraseña.
        /// </summary>
        /// <param name="appName">Nombre de la aplicación (branding)</param>
        /// <param name="resetUrl">URL única con token de restablecimiento</param>
        /// <param name="expiresAtUtc">Fecha de expiración del enlace (UTC)</param>
        /// <returns>HTML completo listo para enviar</returns>
        public static string PasswordReset(string appName, string resetUrl, DateTime expiresAtUtc)
        {
            var expiresFormatted = expiresAtUtc.ToString("dd/MM/yyyy HH:mm") + " UTC";

            var content = $@"
                <p style=""margin: 0 0 16px 0;"">
                    Recibimos una solicitud para restablecer tu contraseña.
                </p>
                <p style=""margin: 0 0 24px 0;"">
                    Si fuiste tú, haz clic en el siguiente botón para continuar:
                </p>
                <div style=""text-align: center; margin: 32px 0;"">
                    <a href=""{resetUrl}"" style=""{ButtonStyles}"">
                        Restablecer contraseña
                    </a>
                </div>
                <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 24px 0;"">
                    <p style=""margin: 0 0 8px 0; font-weight: 600; color: #92400e;"">
                        ⏰ Importante
                    </p>
                    <p style=""margin: 0; font-size: 14px; color: #78350f;"">
                        Este enlace expira el <strong>{expiresFormatted}</strong>.
                    </p>
                </div>
                <div style=""background: #f3f4f6; padding: 16px; border-radius: 8px; margin: 24px 0;"">
                    <p style=""margin: 0 0 8px 0; font-weight: 600; font-size: 14px; color: #374151;"">
                        🔒 Seguridad
                    </p>
                    <p style=""margin: 0; font-size: 13px; color: #6b7280;"">
                        Si no solicitaste este cambio, ignora este mensaje y tu contraseña permanecerá sin cambios.
                        Nunca compartas este enlace con nadie.
                    </p>
                </div>
            ";

            return WrapInLayout(appName, "Restablecimiento de contraseña", content);
        }

        /// <summary>
        /// Versión en texto plano del email de restablecimiento.
        /// </summary>
        public static string PasswordResetPlainText(string appName, string resetUrl, DateTime expiresAtUtc)
        {
            var expiresFormatted = expiresAtUtc.ToString("dd/MM/yyyy HH:mm") + " UTC";

            return $@"
{appName} - Restablecimiento de contraseña
═══════════════════════════════════════════

Recibimos una solicitud para restablecer tu contraseña.

Para continuar, visita el siguiente enlace:
{resetUrl}

⏰ IMPORTANTE: Este enlace expira el {expiresFormatted}.

🔒 SEGURIDAD:
Si no solicitaste este cambio, ignora este mensaje y tu contraseña permanecerá sin cambios.
Nunca compartas este enlace con nadie.

© {DateTime.UtcNow.Year} {appName}. Todos los derechos reservados.
Este es un correo automático, por favor no responder.
            ".Trim();
        }

        #endregion

        #region Email Verification

        /// <summary>
        /// Plantilla HTML de verificación de email (registro).
        /// </summary>
        public static string EmailVerification(string appName, string verificationUrl, DateTime expiresAtUtc)
        {
            var expiresFormatted = expiresAtUtc.ToString("dd/MM/yyyy HH:mm") + " UTC";

            var content = $@"
                <p style=""margin: 0 0 16px 0; font-size: 18px;"">
                    ¡Bienvenido a {appName}! 🎉
                </p>
                <p style=""margin: 0 0 24px 0;"">
                    Para completar tu registro, necesitamos verificar tu dirección de correo electrónico.
                </p>
                <div style=""text-align: center; margin: 32px 0;"">
                    <a href=""{verificationUrl}"" style=""{ButtonStyles}"">
                        Verificar mi correo
                    </a>
                </div>
                <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 24px 0;"">
                    <p style=""margin: 0; font-size: 14px; color: #78350f;"">
                        ⏰ Este enlace expira el <strong>{expiresFormatted}</strong>.
                    </p>
                </div>
                <p style=""margin: 24px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Si no creaste una cuenta en {appName}, puedes ignorar este mensaje.
                </p>
            ";

            return WrapInLayout(appName, "Verifica tu correo electrónico", content);
        }

        /// <summary>
        /// Versión en texto plano de verificación de email.
        /// </summary>
        public static string EmailVerificationPlainText(string appName, string verificationUrl, DateTime expiresAtUtc)
        {
            var expiresFormatted = expiresAtUtc.ToString("dd/MM/yyyy HH:mm") + " UTC";

            return $@"
{appName} - Verifica tu correo electrónico
═══════════════════════════════════════════

¡Bienvenido a {appName}!

Para completar tu registro, verifica tu dirección de correo visitando:
{verificationUrl}

⏰ Este enlace expira el {expiresFormatted}.

Si no creaste una cuenta en {appName}, puedes ignorar este mensaje.

© {DateTime.UtcNow.Year} {appName}. Todos los derechos reservados.
            ".Trim();
        }

        #endregion

        #region Welcome Email

        /// <summary>
        /// Email de bienvenida post-verificación.
        /// </summary>
        public static string Welcome(string appName, string userName, string dashboardUrl)
        {
            var content = $@"
                <p style=""margin: 0 0 16px 0; font-size: 18px;"">
                    ¡Hola <strong>{userName}</strong>! 👋
                </p>
                <p style=""margin: 0 0 24px 0;"">
                    Tu cuenta ha sido verificada exitosamente. Ya puedes comenzar a usar {appName}.
                </p>
                <div style=""text-align: center; margin: 32px 0;"">
                    <a href=""{dashboardUrl}"" style=""{ButtonStyles}"">
                        Ir a mi dashboard
                    </a>
                </div>
                <div style=""background: #f0f9ff; border-left: 4px solid #2563eb; padding: 16px; margin: 24px 0;"">
                    <p style=""margin: 0 0 8px 0; font-weight: 600; color: #1e40af;"">
                        💡 Consejo inicial
                    </p>
                    <p style=""margin: 0; font-size: 14px; color: #1e3a8a;"">
                        Completa tu perfil y explora las ligas disponibles para empezar.
                    </p>
                </div>
            ";

            return WrapInLayout(appName, "¡Bienvenido!", content);
        }

        #endregion

        #region Security Alerts

        /// <summary>
        /// Alerta de inicio de sesión desde nuevo dispositivo.
        /// </summary>
        public static string NewDeviceLogin(
            string appName,
            string userName,
            DateTime loginTime,
            string deviceInfo,
            string ipAddress,
            string location)
        {
            var timeFormatted = loginTime.ToString("dd/MM/yyyy HH:mm") + " UTC";

            var content = $@"
                <p style=""margin: 0 0 16px 0;"">
                    Hola <strong>{userName}</strong>,
                </p>
                <p style=""margin: 0 0 24px 0;"">
                    Detectamos un inicio de sesión en tu cuenta desde un nuevo dispositivo:
                </p>
                <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 24px 0;"">
                    <p style=""margin: 0 0 12px 0; font-size: 14px;"">
                        <strong>📅 Fecha:</strong> {timeFormatted}
                    </p>
                    <p style=""margin: 0 0 12px 0; font-size: 14px;"">
                        <strong>💻 Dispositivo:</strong> {deviceInfo}
                    </p>
                    <p style=""margin: 0 0 12px 0; font-size: 14px;"">
                        <strong>🌍 IP:</strong> {ipAddress}
                    </p>
                    <p style=""margin: 0; font-size: 14px;"">
                        <strong>📍 Ubicación:</strong> {location}
                    </p>
                </div>
                <div style=""background: #fee2e2; border-left: 4px solid #dc2626; padding: 16px; margin: 24px 0;"">
                    <p style=""margin: 0 0 8px 0; font-weight: 600; color: #991b1b;"">
                        ⚠️ ¿No fuiste tú?
                    </p>
                    <p style=""margin: 0; font-size: 14px; color: #7f1d1d;"">
                        Si no reconoces esta actividad, <strong>cambia tu contraseña inmediatamente</strong> 
                        y revisa tu cuenta.
                    </p>
                </div>
            ";

            return WrapInLayout(appName, "🔐 Nuevo inicio de sesión detectado", content);
        }

        #endregion
    }
}