using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBFindById
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
    public void FindByID_WithValidId_ShouldReturnMatchingAccount()
    {
        var input = new Account { Email = "ttt@gg.com", Password = "123456789" };
        bool result = _db.Add(input);
        var output = FindAccount(input);
        var outputById = _db.FindByID(output?.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(output, Is.Not.Null);
            Assert.That(outputById, Is.Not.Null);
            Assert.That(outputById?.Email, Is.EqualTo(input.Email));
            Assert.That(outputById?.Password, Is.EqualTo(input.Password));
        });
        _db.Remove(output.Id);

        Account? FindAccount(Account acc) => _db.FindAll().FirstOrDefault(a => a.Email == acc.Email);
    }

    [Test]
    public void NullParamInDbFindByID_ShouldReturnNull()
    {
        Account? acc = _db.FindByID(null);
        Assert.That(acc, Is.Null);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}