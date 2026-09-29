using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IsoTreatmentProcessSupportAPI.Gateways;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MonolithEntryPoint = IsoTreatmentProcessSupportAPI.Controllers.ReminderController;
using TreatmentServiceEntryPoint = TreatmentService.Api.Controllers.ReminderController;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

// Both applications declare a top-level Program, so each factory is anchored on a type unique
// to its assembly instead.
public sealed class MonolithApplicationFactory : WebApplicationFactory<MonolithEntryPoint>
{
    public const string Issuer = "isotreatment-users-issuer";
    public const string Audience = "isotreatment-users-audience";
    public const string SigningKey = "Kj9pL2mQ8rT5vW3nY6bC4dF1gH0jA9eZ2xU7yVqW3eR5tY6uI8oP9aS0dF==";

    private readonly WebApplicationFactory<TreatmentServiceEntryPoint> _treatmentService;

    // The monolith runs as configured; only the transport is replaced, so the gateway's
    // HttpClient reaches the in-memory Treatment service instead of a network address.
    public MonolithApplicationFactory(WebApplicationFactory<TreatmentServiceEntryPoint> treatmentService)
    {
        _treatmentService = treatmentService;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TreatmentService:BaseAddress", "http://localhost/");

        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IReminderGateway))
                .ConfigurePrimaryHttpMessageHandler(() => _treatmentService.Server.CreateHandler()));
    }

    public static string CreateToken(int userId, string? signingKey = null, string? issuer = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: Audience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpClient CreateClientWithTokenCookie(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"token={token}");
        return client;
    }
}
