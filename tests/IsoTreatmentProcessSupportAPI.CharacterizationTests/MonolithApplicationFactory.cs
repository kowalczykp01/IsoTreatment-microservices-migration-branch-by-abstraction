using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class MonolithApplicationFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "isotreatment-users-issuer";
    public const string Audience = "isotreatment-users-audience";
    public const string SigningKey = "Kj9pL2mQ8rT5vW3nY6bC4dF1gH0jA9eZ2xU7yVqW3eR5tY6uI8oP9aS0dF==";

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
