using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoginAuth.Data.Abstraction;
using LoginAuth.DTO;
using LoginAuth.Model;
using LoginAuth.Security;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Security;

public partial class TwoFactors
{
    private ApiWebApplicationFactory _factory;
    private HttpClient _client;
    private IServiceScope _scope;
    private DatabaseAbstract<Account> _db;
    private TwoFactorsAuth _tfa;

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
        _tfa = _scope.ServiceProvider.GetRequiredService<TwoFactorsAuth>();
    }
    
    [Test]
    public async Task TryValidate_WithValidCurrentCode_ReturnsTrue()
    {
        string userEmail = "exemplo@123.com";
        string appName = "LoginAuth";
        
        // Generate Key - Setup
        var totpAuth = new TwoFactorsAuth();
        TwoFactorSetup setup = totpAuth.Generate(userEmail);

        string userSecretKey = setup.SecretKey;
        
        var totpGenerator = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(userSecretKey));
        string validPinCode = totpGenerator.ComputeTotp();        

        string qrCodeUrl = $"otpauth://totp/{appName}:{userEmail}?secret={userSecretKey}&issuer={appName}";

        // Validate Key - Confirm/Login
        bool isValid = totpAuth.TryValidate(userSecretKey, validPinCode, out string Message);

        Assert.That(setup, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(setup.SecretKey, Is.EqualTo(userSecretKey));
            Assert.That(setup.QrCodeUri, Is.EqualTo(qrCodeUrl));
            Assert.That(isValid, Is.True);
            Assert.That(Message, Is.EqualTo("test"));
        }
    }

    [Test]
    public async Task Setup_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/setup");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Setup_WhenFirstTime_ShouldReturnSecretKeyAndQrCode()
    {
        var acc = await AuthenticateUser("setup_first@test.com");
        var response = await _client.PostAsJsonAsync("/setup", acc);
        var result = await response.Content.ReadFromJsonAsync<TwoFactorSetup>();

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.SecretKey, Is.Not.Null.And.Not.Empty);
            Assert.That(result.QrCodeUri, Is.Not.Null.And.Not.Empty);
        }

        var dbUser = _db.FindAll().First(u => u.Email == acc.Email);
        Assert.That(dbUser.TwoFactorSecret, Is.EqualTo(result.SecretKey));
    }

    [Test]
    public async Task Setup_WhenAlreadySetup_ShouldReturnExistingSecretAndEmptyQrCode()
    {
        var acc = await AuthenticateUser("setup_repeat@test.com");
        acc.TwoFactorSecret = "JBSWY3DPEHPK3PXP";
        _db.Update(acc);
        
        var response = await _client.PostAsJsonAsync("/setup", acc);
        var result = await response.Content.ReadFromJsonAsync<TwoFactorSetup>();

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.SecretKey, Is.EqualTo(acc.TwoFactorSecret));
            Assert.That(result.QrCodeUri, Is.Empty);
        }
    }

    [Test]
    public async Task Confirm_WithoutSetup_ShouldReturnBadRequest()
    {
        _ = await AuthenticateUser("confirm_nosetup@test.com");
        var request = new { Code = "123456" };
        var response = await _client.PostAsJsonAsync("/confirm", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Confirm_WithInvalidCode_ShouldReturnBadRequest()
    {
        var acc = await AuthenticateUser("confirm_invalid@test.com");
        acc.TwoFactorSecret = "JBSWY3DPEHPK3PXP";
        _db.Update(acc);
        
        var request = new { Code = "000000" };
        var response = await _client.PostAsJsonAsync("/confirm", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Confirm_WithValidCode_ShouldEnableTwoFactorAndReturnOk()
    {
        var acc = await AuthenticateUser("confirm_valid@test.com");
        var setupData = _tfa.Generate(acc.Email);
        acc.TwoFactorSecret = setupData.SecretKey;
        _db.Update(acc);
        
        string validCode = GenerateTotpCode(setupData.SecretKey); 
        var request = new { Code = validCode };
        var response = await _client.PostAsJsonAsync("/confirm", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var dbUser = _db.FindAll().First(u => u.Email == acc.Email);
        Assert.That(dbUser.TwoFactorEnabled, Is.EqualTo(1));
    }

    private static string GenerateTotpCode(string secretKey)
    {
        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secretKey));
        return totp.ComputeTotp();
    }

    private async Task<Account> AuthenticateUser(string email = "2fa@test.com")
    {
        var acc = new Account { Email = email, PasswordHash = "dummy_hash" };
        _db.Add(acc);

        var token = _factory.Services.GetRequiredService<LoginAuth.Security.Tokens>().Generate("15", email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return acc;
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
        _client.Dispose();
    }
}