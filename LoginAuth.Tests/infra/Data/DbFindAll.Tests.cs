using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBFindAll
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
    public void FindAll_WhenAccountsExist_ReturnsAllAccountsFromDatabase()
    {
        var account1 = new Account { Email = "user1@gg.com", PasswordHash = "password123" };
        var account2 = new Account { Email = "user2@gg.com", PasswordHash = "password456" };

        var x1 = _db.Add(account1);
        var x2 = _db.Add(account2);
        var result = _db.FindAll<Account>().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(x1, Is.Not.Null);
            Assert.That(x2, Is.Not.Null);
            Assert.That(result, Has.Count.GreaterThanOrEqualTo(2));

            var testAccount1 = result?.FirstOrDefault(a => a.Email == account1.Email);
            var testAccount2 = result?.FirstOrDefault(a => a.Email == account2.Email);

            Assert.That(testAccount1, Is.Not.Null, "Account 1 has not been found in FindAll");
            Assert.That(testAccount1?.PasswordHash, Is.EqualTo(account1.PasswordHash));

            Assert.That(testAccount2, Is.Not.Null, "Account 2 has not been found in FindAll");
            Assert.That(testAccount2?.PasswordHash, Is.EqualTo(account2.PasswordHash));
        }

        var dbAccount1 = _db.FindAll<Account>().FirstOrDefault(a => a.Email == account1.Email);
        var dbAccount2 = _db.FindAll<Account>().FirstOrDefault(a => a.Email == account2.Email);

        _db.Remove<Account>(dbAccount1?.Id);
        _db.Remove<Account>(dbAccount2?.Id);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}