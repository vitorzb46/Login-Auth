using LoginAuth.Data.Abstraction;
using LoginAuth.ErrorMessages;
using LoginAuth.Model;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LoginAuth.Security;

public sealed class Tokens
{
    public sealed record TokenAuth(string AccessToken, string RefreshToken);
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly byte[] key = [];
    private readonly string JwtKey = "Jwt:SecretKey";
    private readonly string JwtIssuer = "Jwt:Issuer";
    private readonly string JwtAudience = "Jwt:Audience";

    public Tokens(IConfiguration configuration, TimeProvider timeProvider)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;

        string secretKey = _configuration[JwtKey]
            ?? throw new NullReferenceException(SystemErrorMessage.SecretKeyNotFound);

        key = Encoding.ASCII.GetBytes(secretKey);
    }

    public TokenAuth? PersistNewRefreshToken(Account account, DatabaseAbstract db)
    {
        var newTokens = Generate(account.Id.ToString(), account.Email);
        var currentUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var rt = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = account.Id,
            Token = newTokens.RefreshToken,
            ExpiresAt = currentUtc.AddDays(7)
        };
        var op = db.Add(rt);
        if(op is null) return null;

        return newTokens;
    }

    public TokenAuth Generate(string userId, string userEmail) =>
        new(AcessToken(userId, userEmail), RefreshToken());

    private string AcessToken(string userId, string userEmail)
    {
        JsonWebTokenHandler handler = new();
        return handler.CreateToken(GetTokenDescriptor(userId, userEmail));
    }

    private static string RefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private SecurityTokenDescriptor GetTokenDescriptor(string userId, string userEmail)
    {
        var currentUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return new()
        {
            Subject = GetClaimsIdentity(userId, userEmail),
            IssuedAt = currentUtc,
            NotBefore = currentUtc,
            Expires = currentUtc.AddMinutes(15),
            Issuer = _configuration[JwtIssuer],
            Audience = _configuration[JwtAudience],
            SigningCredentials = GetSigningCredentials()
        };
    }

    private SigningCredentials GetSigningCredentials() =>
        new(new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

    private static ClaimsIdentity GetClaimsIdentity(string userId, string userEmail) =>
        new([new Claim(JwtRegisteredClaimNames.Sub, userId),
             new Claim(JwtRegisteredClaimNames.Email, userEmail),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())]);
}