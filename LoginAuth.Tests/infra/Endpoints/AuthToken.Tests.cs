using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static LoginAuth.Security.Tokens;

namespace LoginAuth.Tests.infra.Endpoints;

public class AuthToken
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private Tokens _tokens;


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _tokens = _scope.ServiceProvider.GetRequiredService<Tokens>();
    }

    [Test]
    public async Task EndpointProtected_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsync("/protected", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EndpointProtected_WithValidToken_ShouldReturnOk()
    {
        const string Expected = "User authenticated!";
        const string UserId = "15";
        const string UserEmail = "zz@example.com";

        var token = _tokens.Generate(UserId, UserEmail);
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();

        Assert.That(content, Does.Contain(Expected));
        Assert.That(content, Does.Contain(UserId));
        Assert.That(content, Does.Contain(UserEmail));
    }

    [Test]
    public async Task EndpointProtected_WithTokenModified_ShouldReturnUnauthorized()
    {
        var token = _tokens.Generate("15", "zz@example.com");
        var tokenModified = string.Concat(token.AccessToken, "aksmdkasmda12");

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenModified);

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var fakeToken = "invalid-token-123456";
        var response = await _client.PostAsJsonAsync("/refresh", new { RefreshToken = fakeToken });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RefreshToken_WhenExpired_ShouldReturnUnauthorized()
    {
        var acc = AccountData();
        var register = await _client.PostAsJsonAsync("/register", acc);
        var response = await _client.PostAsJsonAsync("/login", acc);

        var output = await response.Content.ReadFromJsonAsync<TokenAuth>();
        var fakeTime = _factory.Services.GetRequiredService<TimeProvider>() as FakeTimeProvider;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(register, Is.Not.Null);
            Assert.That(response, Is.Not.Null);
            Assert.That(output, Is.Not.Null);
            Assert.That(fakeTime, Is.Not.Null);
        }

        fakeTime.Advance(TimeSpan.FromDays(8));

        var refreshToken = new { output.RefreshToken };
        var invalidRequest = await _client.PostAsJsonAsync("/refresh", refreshToken);
        Assert.That(invalidRequest.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RefreshToken_WhenRevoked_ShouldReturnUnauthorized()
    {
        var acc = AccountData();
        var register = await _client.PostAsJsonAsync("/register", acc);
        var response = await _client.PostAsJsonAsync("/login", acc);
        var output = await response.Content.ReadFromJsonAsync<TokenAuth>();
        Assert.That(output, Is.Not.Null);

        var refreshToken = output.RefreshToken;
        var db = _factory.Services.GetRequiredService<DatabaseAbstract<RefreshToken>>();
        var tokenDb = db.FindAll().FirstOrDefault(a => a.Token == refreshToken);
        Assert.That(tokenDb, Is.Not.Null);

        tokenDb.IsRevoked = 1;
        db.Update(tokenDb);

        var rToken = new { output.RefreshToken };
        var invalidRequest = await _client.PostAsJsonAsync("/refresh", rToken);
        Assert.That(invalidRequest.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static FakeDTO AccountData(string? email = null, string pwd = "123456789") =>
        new() { Email = email ?? $"test_{Guid.NewGuid()}@example.com", Password = pwd };

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
