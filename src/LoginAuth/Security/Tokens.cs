using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LoginAuth.ErrorMessages;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LoginAuth.Security;

public sealed class Tokens
{
    public sealed record TokenAuth(string AccessToken, string RefreshToken);
    private readonly IConfiguration _configuration;
    private readonly byte[] key = [];
    private readonly string JwtKey = "Jwt:SecretKey";
    private readonly string JwtIssuer = "Jwt:Issuer";
    private readonly string JwtAudience = "Jwt:Audience";

    public Tokens(IConfiguration configuration)
    {
        _configuration = configuration;
        string secretKey = _configuration[JwtKey] 
            ?? throw new NullReferenceException(SystemErrorMessage.SecretKeyNotFound);

        key = Encoding.ASCII.GetBytes(secretKey);
    }

    public TokenAuth Generate(string userId, string userEmail) => 
        new(AcessToken(userId, userEmail), Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

    private string AcessToken(string userId, string userEmail)
    {
        JsonWebTokenHandler handler = new();
        return handler.CreateToken(GetTokenDescriptor(userId, userEmail));
    }

    private SecurityTokenDescriptor GetTokenDescriptor(string userId, string userEmail) =>
        new()
        {
            Subject = GetClaimsIdentity(userId, userEmail),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _configuration[JwtIssuer],
            Audience = _configuration[JwtAudience],
            SigningCredentials = GetSigningCredentials()
        };

    private SigningCredentials GetSigningCredentials() => 
        new(new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);

    private static ClaimsIdentity GetClaimsIdentity(string userId, string userEmail) =>
        new([new Claim(JwtRegisteredClaimNames.Sub, userId),
             new Claim(JwtRegisteredClaimNames.Email, userEmail),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())]);
}