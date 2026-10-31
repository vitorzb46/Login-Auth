using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Security;

public class UserPasswordHasher
{
    private ApiWebApplicationFactory _factory;
    private IServiceScope _scope;
    private PasswordHasher<string> _pwHash;

    private RegisterRequest _register;
    private LoginRequest _login;
    private AccountPasswordHasher _hasher;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiWebApplicationFactory();
    
    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
        _pwHash = _scope.ServiceProvider.GetRequiredService<PasswordHasher<string>>();
        _register = new RegisterRequest("zzz@example.com", "123456789");
        _login = new LoginRequest("zzz@example.com", "123456789");
        _hasher = new AccountPasswordHasher(_pwHash);
    }

    [Test]
    public void NewMethod()
    {
        Account hashedAccount = _hasher.HashPassword(_register);
        bool isValid = _hasher.VerifyPassword(_login, hashedAccount.PasswordHash);
        Assert.That(isValid, Is.True);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}