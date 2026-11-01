using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoginAuth.Security;
using Microsoft.Extensions.DependencyInjection;

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

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
