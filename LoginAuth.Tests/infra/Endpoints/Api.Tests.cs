using System.Net;
using System.Net.Http.Json;
using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.Endpoints;

public class Api
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract<Account> _db;

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
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract<Account>>();
    }

    [Test]
    public async Task RegisterNewAccount_ShouldReturnOk()
    {
        var input = AccountData();
        var response = await _client.PostAsJsonAsync("/register", input);
        AssertInputAndResponse_ThenCleanDB(input, response);
    }

    [Test]
    public async Task SignIn_WithExistingAccount_ShouldReturnOk()
    {
        var input = AccountData();
        _db.Add(input);
        var response = await _client.PostAsJsonAsync("/login", input);
        AssertInputAndResponse_ThenCleanDB(input, response);
    }

    private static Account AccountData(string? email = null, string pwd = "123456789") =>
        new(){ Email = email ?? $"test_{Guid.NewGuid()}@example.com", Password = pwd };
    
    private void AssertInputAndResponse_ThenCleanDB(Account input, HttpResponseMessage response)
    {
        var bancoAcc = _db.FindAll().FirstOrDefault(s => s.Email == input.Email && s.Password == input.Password);
        Assert.That(bancoAcc, Is.Not.Null);
        _db.Remove(bancoAcc.Id);
        var accRemoved = _db.FindByID(bancoAcc.Id);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(bancoAcc.Email, Is.EqualTo(input.Email));
            Assert.That(bancoAcc.Password, Is.EqualTo(input.Password));
            Assert.That(accRemoved, Is.Null);
        });
    }
    
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
