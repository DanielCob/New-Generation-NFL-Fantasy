using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace NFL_Fantasy_API.SharedSystems.Security
{
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
            // Si tu middleware ya puso un usuario autenticado, úsalo.
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            // Si no hay usuario, no autenticar (permitirá que Authorization haga Challenge/Forbid)
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
