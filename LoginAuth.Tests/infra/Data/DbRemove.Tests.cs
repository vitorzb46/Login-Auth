using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBRemove
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
    public void PersistDataThenRemoveFromDatabase()
    {
        var acc = new Account { Email = $"{Guid.NewGuid()}@gg.com", PasswordHash = "123456789" };
        var result = _db.Add(acc);
        var saved = FindAccount(acc);
        _db.Remove<Account>(saved?.Id);
        var savedAfterRemoved = FindAccount(acc);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(savedAfterRemoved, Is.Null);
        }

        Account? FindAccount(Account acc) => _db.FindAll<Account>().FirstOrDefault(a => a.Email == acc.Email);
    }

    [Test]
    public void NullParamInDbRemove_ShouldReturnFalse()
    {
        bool isValid = _db.Remove<Account>(null);
        Assert.That(isValid, Is.False);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}