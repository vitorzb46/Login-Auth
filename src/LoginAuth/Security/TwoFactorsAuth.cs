using LoginAuth.DTO;
using OtpNet;

namespace LoginAuth.Security;

public class TwoFactorsAuth
{
    private readonly string _appName = "LoginAuth";
    
    public TwoFactorSetup Generate(string userEmail)
    {
        string userSecretKey = RandomKey();
        string qrCodeUrl = $"otpauth://totp/{_appName}:{userEmail}?secret={userSecretKey}&issuer={_appName}";
        return new(userSecretKey, qrCodeUrl);
    }

    public bool TryValidate(string? userSecretKey, string pinCode, out string message)
    {
        message = default!;
        if (string.IsNullOrEmpty(userSecretKey)) return false;
        if (string.IsNullOrEmpty(pinCode)) return false;
        message = "test";
        return VerifyTotp(userSecretKey, pinCode);
    }

    private static string RandomKey()
    {
        byte[] secretBytes = KeyGeneration.GenerateRandomKey(20);
        string secretKey = Base32Encoding.ToString(secretBytes);
        return secretKey;
    }
    
    private static bool VerifyTotp(string secretKey, string pinCode)
    {
        var totp = new Totp(Base32ToBytes(secretKey));

        return totp.VerifyTotp(pinCode.Replace(" ", ""), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    private static byte[] Base32ToBytes(string key) => Base32Encoding.ToBytes(key);
}