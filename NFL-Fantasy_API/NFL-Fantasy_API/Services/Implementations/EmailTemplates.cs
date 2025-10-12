using System;

namespace NFL_Fantasy_API.Services.Implementations
{
    /// <summary>
    /// Plantillas HTML centralizadas para correos transaccionales.
    /// </summary>
    public static class EmailTemplates
    {
        /// <summary>
        /// Plantilla de restablecimiento de contraseña.
        /// </summary>
        /// <param name="appName">Nombre de la aplicación (branding del remitente).</param>
        /// <param name="resetUrl">URL única hacia el formulario de restablecimiento (contiene el token).</param>
        /// <param name="expiresAtUtc">Expiración del enlace (UTC).</param>
        /// <returns>HTML listo para enviar.</returns>
        public static string PasswordReset(string appName, string resetUrl, DateTime expiresAtUtc)
        {
            // Se usa formato UTC legible (u) para dejar claro el huso horario
            var expires = expiresAtUtc.ToString("u");
            return $@"
<!doctype html>
<html>
  <body style=""font-family:Arial,Helvetica,sans-serif; color:#1f2937; background:#ffffff; margin:0; padding:24px;"">
    <div style=""max-width:560px; margin:0 auto;"">
      <h2 style=""margin:0 0 16px 0; color:#111827;"">{appName} – Restablecimiento de contraseña</h2>
      <p style=""line-height:1.6;"">Recibimos una solicitud para restablecer tu contraseña.</p>
      <p style=""line-height:1.6;"">
        <a href=""{resetUrl}"" 
           style=""display:inline-block; background:#2563eb; color:#fff; padding:12px 18px; border-radius:8px; text-decoration:none;"">
           Restablecer contraseña
        </a>
      </p>
      <p style=""line-height:1.6; font-size:14px; color:#4b5563;"">
        Este enlace expira el <strong>{expires} (UTC)</strong>.
        Si no solicitaste este cambio, ignora este mensaje.
      </p>
      <hr style=""border:none; border-top:1px solid #e5e7eb; margin:24px 0;"">
      <p style=""font-size:12px; color:#6b7280;"">{appName}</p>
    </div>
  </body>
</html>";
        }
    }
}
