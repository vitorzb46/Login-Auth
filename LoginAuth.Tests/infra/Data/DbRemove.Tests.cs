using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBRemove
{
    private ApiWebApplicationFactory _factory;
    private IServiceScope _scope;
    private DatabaseAbstract<Account> _db;


    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiWebApplicationFactory();

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<DatabaseAbstract<Account>>();
    }

    [Test]
    public void PersistDataThenRemoveFromDatabase()
    {
        var acc = new Account { Email = $"{Guid.NewGuid()}@gg.com", PasswordHash = "123456789" };
        bool result = _db.Add(acc);
        var saved = FindAccount(acc);
        _db.Remove(saved?.Id);
        var savedAfterRemoved = FindAccount(acc);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(savedAfterRemoved, Is.Null);
        }

        Account? FindAccount(Account acc) => _db.FindAll().FirstOrDefault(a => a.Email == acc.Email);
    }

    [Test]
    public void NullParamInDbRemove_ShouldReturnFalse()
    {
        bool isValid = _db.Remove(null);
        Assert.That(isValid, Is.False);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}