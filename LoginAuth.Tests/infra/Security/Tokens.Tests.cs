using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LoginAuth.Tests.infra.Security;

public class Tokens
{
    private LoginAuth.Security.Tokens _tokens;
    private IConfigurationRoot _configuration;

    [SetUp]
    public void SetUp()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "ThisIsASecretKeyForTestingPurposesOnly" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" }  
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokens = new LoginAuth.Security.Tokens(_configuration);
    }

    [Test]
    public void GenerateToken_ShouldReturnValidTokenAndRefreshToken()
    {
        var userId = "15";
        var userEmail = "test@example.com";
        var token = _tokens.Generate(userId, userEmail);
        
        Assert.Multiple(() =>
        {
            Assert.That(token, Is.Not.Null);
            Assert.That(token.AccessToken, Is.Not.Empty);
            Assert.That(token.RefreshToken, Is.Not.Empty);
        });

        var handler = new JsonWebTokenHandler();
        var jwtToken = handler.ReadJsonWebToken(token.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(jwtToken.Issuer, Is.EqualTo("TestIssuer"));
            Assert.That(jwtToken.Audiences.First(), Is.EqualTo("TestAudience"));
            Assert.That(jwtToken.GetClaim(JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(userId));
            Assert.That(jwtToken.GetClaim(JwtRegisteredClaimNames.Email).Value, Is.EqualTo(userEmail));
        });
    }
}