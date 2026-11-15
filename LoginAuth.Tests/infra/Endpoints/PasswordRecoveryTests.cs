using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Interfaces;
using LoginAuth.Model;
using LoginAuth.Worker;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Endpoints;

public sealed class PasswordRecoveryTests
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract _db;
    private Channel<IEmailEvent> _channel;


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
        _channel = _scope.ServiceProvider.GetRequiredService<Channel<IEmailEvent>>();   
        await _db.ExecuteAsync("DELETE FROM PasswordReset; DELETE FROM Account;");
    }
    
    [Test]
    public async Task Recover_WhenUserExists_ShouldReturnOkAndEnqueueEvent()
    {
        using var localFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var workerDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(EmailProcessingWorker));
                    
                if (workerDescriptor != null)
                    services.Remove(workerDescriptor);
            });
        });
        
        using var localClient = localFactory.CreateClient();
        using var scope = localFactory.Services.CreateScope();
        var channel = scope.ServiceProvider.GetRequiredService<Channel<IEmailEvent>>();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();

        var email = "recover_exist@test.com";
        var acc = new Account { Email = email, PasswordHash = "hash" };
        db.Add(acc);
        var request = new RecoverPasswordRequest(email);
        var response = await localClient.PostAsJsonAsync("/recover-password", request);
        
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var wasEnqueued = channel.Reader.TryRead(out var emailEvent);
            Assert.That(wasEnqueued, Is.True);
            Assert.That(emailEvent, Is.InstanceOf<PasswordRecoveryEvent>());
            Assert.That(emailEvent!.Email, Is.EqualTo(email));
        }
    }

    [Test]
    public async Task Recover_WhenUserDoesNotExist_ShouldReturnOkAndNotEnqueueEvent()
    {
        var request = new RecoverPasswordRequest("nonexistent@test.com");
        var response = await _client.PostAsJsonAsync("/recover-password", request);
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var wasEnqueued = _channel.Reader.TryRead(out _);
            Assert.That(wasEnqueued, Is.False);
        }
    }

    [Test]
    public async Task Reset_WithValidToken_ShouldUpdatePasswordAndReturnOk()
    {
        var email = "reset_valid@test.com";
        var acc = _db.Add(new Account { Email = email, PasswordHash = "old_hash" });
        Assert.That(acc, Is.Not.Null);

        var token = Guid.NewGuid().ToString("N");
        var resetRecord = new PasswordReset
        {
            AccountId = acc.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        const string Query = "INSERT INTO PasswordReset (AccountId, Token, ExpiresAt) VALUES (@AccountId, @Token, @ExpiresAt)";
        await _db.ExecuteAsync(Query, resetRecord);

        var request = new ResetPasswordRequest(token, "NewSecurePassword123!");
        var response = await _client.PostAsJsonAsync("/reset-password", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var updatedAcc = _db.FindByID<Account>(acc.Id);
            Assert.That(updatedAcc, Is.Not.Null);
            Assert.That(updatedAcc?.PasswordHash, Is.Not.EqualTo("old_hash"));
        }        
    }

    [Test]
    public async Task Reset_WithInvalidToken_ShouldReturnBadRequest()
    {
        var request = new ResetPasswordRequest("invalid-token-123", "NewSecurePassword123!");
        var response = await _client.PostAsJsonAsync("/reset-password", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Reset_WithExpiredToken_ShouldReturnBadRequest()
    {
        var email = "reset_expired@test.com";
        var acc = _db.Add(new Account { Email = email, PasswordHash = "old_hash" });
        Assert.That(acc, Is.Not.Null);

        var token = Guid.NewGuid().ToString("N");
        var resetRecord = new PasswordReset 
        { 
            AccountId = acc.Id, 
            Token = token, 
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        const string Query = "INSERT INTO PasswordReset (AccountId, Token, ExpiresAt) VALUES (@AccountId, @Token, @ExpiresAt)";
        await _db.ExecuteAsync(Query, resetRecord);

        var request = new ResetPasswordRequest(token, "NewSecurePassword123!");
        var response = await _client.PostAsJsonAsync("/reset-password", request);
        
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            var unchangedAcc = _db.FindByID<Account>(acc.Id);
            Assert.That(unchangedAcc, Is.Not.Null);
            Assert.That(unchangedAcc?.PasswordHash, Is.EqualTo("old_hash"));
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
