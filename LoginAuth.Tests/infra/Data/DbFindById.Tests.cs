using LoginAuth.Data.Abstraction;
using LoginAuth.Model;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Data;

public class DBFindById
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
    public void FindByID_WithValidId_ShouldReturnMatchingAccount()
    {
        var input = new Account { Email = "ttt@gg.com", PasswordHash = "123456789" };
        var result = _db.Add(input);
        var output = FindAccount(input);
        var outputById = _db.FindByID<Account>(output?.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(output, Is.Not.Null);
            Assert.That(outputById, Is.Not.Null);
            Assert.That(outputById?.Email, Is.EqualTo(input.Email));
            Assert.That(outputById?.PasswordHash, Is.EqualTo(input.PasswordHash));
        }
        _db.Remove<Account>(output.Id);

        Account? FindAccount(Account acc) => _db.FindAll<Account>().FirstOrDefault(a => a.Email == acc.Email);
    }

    [Test]
    public void NullParamInDbFindByID_ShouldReturnNull()
    {
        var acc = _db.FindByID<Account>(null);
        Assert.That(acc, Is.Null);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}