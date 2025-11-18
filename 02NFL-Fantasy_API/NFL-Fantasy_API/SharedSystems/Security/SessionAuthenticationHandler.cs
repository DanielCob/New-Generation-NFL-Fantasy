using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace NFL_Fantasy_API.SharedSystems.Security
{
    /// <summary>
    /// ============================================
    /// HANDLER DE AUTENTICACIÓN DE SESIÓN
    /// ============================================
    /// 
    /// Integra el sistema de autenticación personalizado
    /// con el framework de Authentication de ASP.NET Core.
    /// 
    /// Permite usar atributos [Authorize] estándar en los controllers
    /// junto con nuestro sistema de sesiones basado en GUID.
    /// </summary>
    public class SessionAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public SessionAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Si el AuthenticationMiddleware ya estableció un usuario autenticado,
            // crear un ticket de autenticación exitoso
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            // No hay usuario autenticado en el contexto
            // (El AuthenticationMiddleware se encargará de esto)
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}