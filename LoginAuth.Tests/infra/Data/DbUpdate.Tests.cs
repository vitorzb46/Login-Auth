using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBUpdate
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
    public void UpdateAccountAtDatabaseThenRemoveFromDB()
    {
        var input = new Account { Email = "teste@zz.com", PasswordHash = "123456789" };
        Account? inputUpdate = null;
        try
        {
            bool res = _db.Add(input);
            var dbOutput = FindAccount(input);
            Assert.That(dbOutput, Is.Not.Null);

            inputUpdate = new Account { Id = dbOutput.Id, Email = "testeUpdated@zz.com", PasswordHash = "123456789" };
            bool isUpdated = _db.Update(inputUpdate);
            var updateOutput = FindAccount(inputUpdate);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(res, Is.True);
                Assert.That(isUpdated, Is.True);
                Assert.That(updateOutput, Is.Not.Null);
                Assert.That(updateOutput?.Email, Is.EqualTo(inputUpdate.Email));
                Assert.That(updateOutput?.PasswordHash, Is.EqualTo(inputUpdate.PasswordHash));
            }
        }
        finally
        {
            _db.Remove(inputUpdate!.Id);
        }
        Account? FindAccount(Account acc) => _db.FindAll().FirstOrDefault(a => a.Email == acc.Email);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}