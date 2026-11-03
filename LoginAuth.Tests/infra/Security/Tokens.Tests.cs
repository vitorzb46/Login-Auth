using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LoginAuth.Tests.infra.Security;

public class Tokens
{
    private IServiceScope _scope;
    private TimeProvider _timeProvider;
    private LoginAuth.Security.Tokens _tokens;
    private IConfigurationRoot _configuration;
    private ApiWebApplicationFactory _factory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
    }

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _timeProvider = _scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "ThisIsASecretKeyForTestingPurposesOnly" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokens = new LoginAuth.Security.Tokens(_configuration, _timeProvider);
    }

    [Test]
    public void GenerateToken_ShouldReturnValidTokenAndRefreshToken()
    {
        var userId = "15";
        var userEmail = "test@example.com";
        var token = _tokens.Generate(userId, userEmail);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token, Is.Not.Null);
            Assert.That(token.AccessToken, Is.Not.Empty);
            Assert.That(token.RefreshToken, Is.Not.Empty);
        }

        var handler = new JsonWebTokenHandler();
        var jwtToken = handler.ReadJsonWebToken(token.AccessToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(jwtToken.Issuer, Is.EqualTo("TestIssuer"));
            Assert.That(jwtToken.Audiences.First(), Is.EqualTo("TestAudience"));
            Assert.That(jwtToken.GetClaim(JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(userId));
            Assert.That(jwtToken.GetClaim(JwtRegisteredClaimNames.Email).Value, Is.EqualTo(userEmail));
        }
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}