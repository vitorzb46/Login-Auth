using LoginAuth.DTO;
using LoginAuth.Security;
using Microsoft.Extensions.DependencyInjection;

namespace LoginAuth.Tests.infra.Security;

public partial class TwoFactors
{
    private ApiWebApplicationFactory _factory;
    private IServiceScope _scope;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiWebApplicationFactory();
    }

    [SetUp]
    public void SetUp()
    {
        _scope = _factory.Services.CreateScope();
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

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();
}