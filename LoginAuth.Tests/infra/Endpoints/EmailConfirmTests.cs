using System.Net;
using System.Net.Http.Json;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Endpoints;

public sealed class EmailConfirmTests
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract _db;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public async Task SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
        await _db.ExecuteAsync("DELETE FROM EmailConfirmation; DELETE FROM Account;");
    }

    [Test]
    public async Task ConfirmEmail_WhenValidCode_ShouldReturnOkAndUpdateDb()
    {
        var email = "confirm@test.com";
        var code = "123456";
        var acc = _db.Add(new Account { Email = email, PasswordHash = "hash" });
        Assert.That(acc, Is.Not.Null);
        
        var confirmation = new
        {
            acc.Id, 
            Code = code, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) 
        };
        const string Query = "INSERT INTO EmailConfirmation (AccountId, Code, ExpiresAt) VALUES (@Id, @Code, @ExpiresAt)";
        var task = await _db.ExecuteAsync(Query, confirmation);
        var fromDb = _db.FindByID<EmailConfirmation>(acc.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(task, Is.True);
            Assert.That(fromDb, Is.Not.Null);
        }
        
        var request = new ConfirmEmailRequest(email, code);
        var response = await _client.PostAsJsonAsync("/email/confirmation", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var updatedUser = _db.FindAll<Account>().First(u => u.Email == email);
        Assert.That(updatedUser.EmailConfirmed, Is.EqualTo(1));
        
        var savedCode = _db.FindByID<EmailConfirmation>(acc.Id);
        Assert.That(savedCode, Is.Null);
    }

    [Test]
    public async Task ConfirmEmail_WhenCodeExpired_ShouldReturnBadRequest() => 
        await InvalidScenarioAsync("123456", "123456", -5);

    [Test]
    public async Task ConfirmEmail_WhenInvalidCode_ShouldReturnBadRequest() => 
        await InvalidScenarioAsync("000000", "123456", 15);

    private async Task InvalidScenarioAsync(string requestCode, string dbCode, double minutes)
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var email = "expired@test.com";
        var acc = _db.Add(new Account { Email = email, PasswordHash = "hash" });
        Assert.That(acc, Is.Not.Null);
        
        var confirmation = new 
        { 
            acc.Id, 
            Code = dbCode, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(minutes)
        };
        const string Query = "INSERT INTO EmailConfirmation (AccountId, Code, ExpiresAt) VALUES (@Id, @Code, @ExpiresAt)";
        var task = await _db.ExecuteAsync(Query, confirmation);
        var request = new ConfirmEmailRequest(email, requestCode);
        var response = await client.PostAsJsonAsync("/email/confirmation", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(task, Is.True);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
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
