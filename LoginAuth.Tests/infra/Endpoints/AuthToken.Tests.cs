using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using static LoginAuth.Security.Tokens;

namespace LoginAuth.Tests.infra.Endpoints;

public class AuthToken
{
    private ApiWebApplicationFactory _factory;
    private FakeTime _fakeTime;
    private HttpClient _client;
    private IServiceScope _scope;
    private Tokens _tokens;


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _fakeTime = new FakeTime(_factory);
        _client = _factory.CreateClient();
    }

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _tokens = _scope.ServiceProvider.GetRequiredService<Tokens>();
    }

    [Test]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.PostAsync("/protected", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ProtectedEndpoint_WithValidToken_ShouldReturnOk()
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
    public async Task ProtectedEndpoint_WithTokenModified_ShouldReturnUnauthorized()
    {
        var token = _tokens.Generate("15", "zz@example.com");
        var tokenModified = string.Concat(token.AccessToken, "aksmdkasmda12");

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenModified);

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ProtectedEndpoint_200Requests_ShouldReturnOk()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = _tokens.Generate("15", "test@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        for (int i = 1; i < 200; i++)
        {
            var response = await client.PostAsync("/protected", null);
            Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    [Test]
    public async Task ProtectedEndpoint_WhenOneUserHitsLimit_AnotherUserShouldStillHaveAccess()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var tokenUserA = _tokens.Generate("UserA", "usera@test.com");
        var tokenUserB = _tokens.Generate("UserB", "userb@test.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUserA.AccessToken);
        for (int i = 0; i < 200; i++)
        {
            await client.PostAsync("/protected", null);
        }

        var responseUserA = await client.PostAsync("/protected", null);
        Assert.That(responseUserA.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUserB.AccessToken);
        var responseUserB = await client.PostAsync("/protected", null);

        Assert.That(responseUserB.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(register, Is.Not.Null);
            Assert.That(response, Is.Not.Null);
            Assert.That(output, Is.Not.Null);
        }

        _fakeTime.AdvanceTime(TimeSpan.FromDays(8));

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

    [Test]
    public async Task Login_FiveRequests_ShouldReturnOk()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var acc = AccountData();

        await ForLoop("/login", acc, 5, client);
    }

    [Test]
    public async Task Login_MoreThanFiveRequests_ShouldReturnTooManyRequests()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var acc = AccountData();
        const string RequestUri = "/login";

        await ForLoop(RequestUri, acc, 5, client);

        var invalidOutput = await client.PostAsJsonAsync(RequestUri, acc);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidOutput.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(invalidOutput.Headers.Contains("Retry-After"), Is.True);
        }
    }

    [Test]
    public async Task Register_FiveRequests_ShouldReturnOk()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await RegisterForLoop(5, client);
    }

    [Test]
    public async Task Register_MoreThanFiveRequests_ShouldReturnTooManyRequests()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        const string RequestUri = "/register";

        await RegisterForLoop(5, client);

        var acc2 = AccountData();
        var invalidOutput = await client.PostAsJsonAsync(RequestUri, acc2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidOutput.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(invalidOutput.Headers.Contains("Retry-After"), Is.True);
        }
    }

    [Test]
    public void FixedWindow_RequestsThatExceedTheLimit_ShouldReturnFalse()
    {
        var timeWindow = TimeSpan.FromMinutes(1);
        var options = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = timeWindow,
            AutoReplenishment = false
        };
        using var limiter = new FixedWindowRateLimiter(options);
        var lease1 = limiter.AttemptAcquire(4);
        var lease2 = limiter.AttemptAcquire(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lease1.IsAcquired, Is.True);
            Assert.That(lease2.IsAcquired, Is.True);
        }

        var lease3 = limiter.AttemptAcquire(1);
        Assert.That(lease3.IsAcquired, Is.False);
    }

    [Test]
    public void SlidingWindow_RequestsThatExceedTheLimit_ShouldReturnFalse()
    {
        var timeWindow = TimeSpan.FromMinutes(1);
        var options = new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
            AutoReplenishment = false
        };
        using var limiter = new SlidingWindowRateLimiter(options);
        var lease1 = limiter.AttemptAcquire(199);
        var lease2 = limiter.AttemptAcquire(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lease1.IsAcquired, Is.True);
            Assert.That(lease2.IsAcquired, Is.True);
        }

        var lease3 = limiter.AttemptAcquire(1);
        Assert.That(lease3.IsAcquired, Is.False);
    }

    private async Task RegisterForLoop(int iterations, HttpClient client)
    {
        for (int i = 0; i < iterations; i++)
        {
            var acc = AccountData();
            var output = await client.PostAsJsonAsync("/register", acc);
            Assert.That(output.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    private async Task ForLoop(string requestUri, FakeDTO acc, int iterations, HttpClient client)
    {
        for (int i = 0; i < iterations; i++)
        {
            var output = await client.PostAsJsonAsync(requestUri, acc);
            Assert.That(output.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests));
        }
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
