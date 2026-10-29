using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace LoginAuth.Tests.infra.Endpoints;

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
    public async Task RegisterEmailAlreadyExists_ShouldReturnBadRequest()
    {
        var input1 = AccountData("example1@ex.com");
        var response = await _client.PostAsJsonAsync("/register", input1);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SignIn_WithExistingAccount_ShouldReturnOk()
    {
        var input = AccountData();
        _db.Add(input);
        var response = await _client.PostAsJsonAsync("/login", input);
        AssertInputAndResponse_ThenCleanDB(input, response);
    }

    [Test]
    public async Task SignIn_WithPwdLessThan8_ShouldReturnBadRequest()
    {
        var inputBad = AccountData(pwd: "1234567");
        var inputGood = AccountData(pwd: "123456789");
        var registro = await _client.PostAsJsonAsync("/register", inputGood);
        var responseBad = await _client.PostAsJsonAsync("/login", inputBad);        
        var responseGood = await _client.PostAsJsonAsync("/login", inputGood);
        var bancoAcc = _db.FindAll().FirstOrDefault(acc => acc.Email == inputGood.Email);
        _db.Remove(inputGood.Id);
        var accRemoved = _db.FindByID(inputGood.Id);
        Assert.Multiple(() =>
        {
            Assert.That(registro.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(bancoAcc, Is.Not.Null);
            Assert.That(bancoAcc?.Email, Is.EqualTo(inputGood.Email));
            Assert.That(bancoAcc?.Password, Is.EqualTo(inputGood.Password));
            Assert.That(responseBad.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(responseGood.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(accRemoved, Is.Null);
        });
    }

    private static Account AccountData(string? email = null, string pwd = "123456789") =>
        new() { Email = email ?? $"test_{Guid.NewGuid()}@example.com", Password = pwd };

    private void AssertInputAndResponse_ThenCleanDB(Account input, HttpResponseMessage response)
    {
        Account? bancoAcc = null;
        try
        {
            bancoAcc = _db.FindAll().FirstOrDefault(s => s.Email == input.Email && s.Password == input.Password);
            Assert.That(bancoAcc, Is.Not.Null);         
            Assert.Multiple(() =>            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));                
                Assert.That(bancoAcc.Email, Is.EqualTo(input.Email));
                Assert.That(bancoAcc.Password, Is.EqualTo(input.Password));
            });
        }
        finally
        {
            Assert.That(bancoAcc, Is.Not.Null);
            _db.Remove(bancoAcc.Id);
            var accRemoved = _db.FindByID(bancoAcc.Id);
            Assert.That(accRemoved, Is.Null);
        }
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
