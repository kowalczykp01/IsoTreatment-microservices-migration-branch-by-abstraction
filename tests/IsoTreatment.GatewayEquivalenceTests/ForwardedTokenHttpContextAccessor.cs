using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace IsoTreatment.GatewayEquivalenceTests;

// Stands in for an incoming monolith request that JwtBearer has already authenticated with
// SaveToken on: TreatmentServiceReminderGateway reads the token back with GetTokenAsync.
internal sealed class ForwardedTokenHttpContextAccessor : IHttpContextAccessor
{
    public ForwardedTokenHttpContextAccessor(string token)
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new AuthenticatedWithToken(token))
            .BuildServiceProvider();

        HttpContext = new DefaultHttpContext { RequestServices = services };
    }

    public HttpContext? HttpContext { get; set; }

    private sealed class AuthenticatedWithToken(string token) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            var properties = new AuthenticationProperties();
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = token } });

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Bearer");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();
    }
}
