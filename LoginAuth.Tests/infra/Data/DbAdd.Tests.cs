using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBAdd
{
    private ApiWebApplicationFactory _factory;
    private IServiceScope _scope;
    private DatabaseAbstract _db;


    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiWebApplicationFactory();

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract>();
    }

    [Test]
    public void TestIfDataIsPersistedInDatabase()
    {
        var acc = new Account { Email = $"{Guid.NewGuid()}@gg.com", PasswordHash = "123456789" };
        var result = _db.Add(acc);
        var saved = _db.FindAll<Account>().FirstOrDefault(a => a.Email == acc.Email);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved?.Email, Is.EqualTo(acc.Email));
            Assert.That(saved?.PasswordHash, Is.EqualTo(acc.PasswordHash));
        }
        _db.Remove<Account>(saved.Id);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}