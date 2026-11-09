using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using static LoginAuth.Security.Tokens;

namespace LoginAuth.Tests.infra.Endpoints;

public class Api
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract _db;
    private PasswordHasher<string> _hasher;

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
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
        _hasher = new PasswordHasher<string>();
    }

    [Test]
    public async Task RegisterNewAccount_ShouldReturnOk()
    {
        var input = AccountData();
        var response = await _client.PostAsJsonAsync("/register", input);
        var bankAcc = _db.FindAll<Account>().FirstOrDefault(a => a.Email == input.Email);
        try
        {
            Assert.That(bankAcc, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                var verifyHash = _hasher.VerifyHashedPassword(input.Email, bankAcc.PasswordHash, input.Password);
                Assert.That(verifyHash, Is.EqualTo(PasswordVerificationResult.Success));
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(bankAcc?.Email, Is.EqualTo(input.Email));
            }
        }
        finally
        {
            _db.Remove<Account>(bankAcc?.Id);
        }
    }

    [Test]
    public async Task RegisterEmailThatAlreadyExists_ShouldReturnBadRequest()
    {
        var acc = AccountData("example1@ex.com");
        _db.Add(AccountMap(acc));
        var input = await _client.PostAsJsonAsync("/register", acc);
        Assert.That(input.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var r = _db.FindAll<Account>().FirstOrDefault(a => a.Email == acc.Email);
        _db.Remove<Account>(r?.Id);
    }

    [Test]
    public async Task SignIn_WithExistingAccount_ShouldReturnOk()
    {
        var input = AccountData();
        var output = await _client.PostAsJsonAsync("/register", input);
        var response = await _client.PostAsJsonAsync("/login", input);
        var bankAcc = _db.FindAll<Account>().FirstOrDefault(a => a.Email == input.Email);
        var savedRefreshToken = _db.FindAll<RefreshToken>().FirstOrDefault(a => a.AccountId == bankAcc?.Id);
        try
        {
            Assert.That(bankAcc, Is.Not.Null);
            var verifyHash = _hasher.VerifyHashedPassword(input.Email, bankAcc.PasswordHash, input.Password);
            var tokenResult = await response.Content.ReadFromJsonAsync<TokenAuth>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedRefreshToken, Is.Not.Null);
                Assert.That(tokenResult, Is.Not.Null);
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(output.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(verifyHash, Is.EqualTo(PasswordVerificationResult.Success));
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(bankAcc?.Email, Is.EqualTo(input.Email));

                Assert.That(tokenResult.AccessToken, Is.Not.Empty);
                Assert.That(tokenResult.RefreshToken, Is.Not.Empty);

                Assert.That(savedRefreshToken.Token, Is.Not.Empty);
                Assert.That(savedRefreshToken.Token, Is.EqualTo(tokenResult.RefreshToken));

                var timeProvider = _factory.Services.GetRequiredService<TimeProvider>();
                Assert.That(savedRefreshToken.ExpiresAt, Is.GreaterThan(timeProvider.GetUtcNow().UtcDateTime));
            }
        }
        finally
        {
            _db.Remove<Account>(bankAcc?.Id);
            _db.Remove<RefreshToken>(savedRefreshToken?.AccountId);
        }
    }

    [Test]
    public async Task SignIn_WithPwdLessThan8_ShouldReturnBadRequest()
    {
        var input = AccountData(pwd: "1234567");
        var response = await _client.PostAsJsonAsync("/login", input);
        var bankAcc = _db.FindAll<Account>().FirstOrDefault(a => a.Email == input.Email);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(bankAcc, Is.Null);
        }
    }

    private static Account AccountMap(FakeDTO fakeDTO) =>
        new() { Id = 0, Email = fakeDTO.Email, PasswordHash = fakeDTO.Password };

    private static FakeDTO AccountData(string? email = null, string pwd = "123456789") =>
        new() { Email = email ?? $"test_{Guid.NewGuid()}@example.com", Password = pwd };

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
