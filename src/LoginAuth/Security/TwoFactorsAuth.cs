using LoginAuth.DTO;
using OtpNet;
using System.Security.Cryptography;

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
        Span<byte> secretBytes = stackalloc byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        return Base32Encode(secretBytes);
    }

    private static bool VerifyTotp(string secretKey, string pinCode)
    {
        var totp = new Totp(Base32ToBytes(secretKey));

        return totp.VerifyTotp(pinCode.Replace(" ", ""), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    private static byte[] Base32ToBytes(string key) => Base32Encoding.ToBytes(key);

    private static string Base32Encode(ReadOnlySpan<byte> input)
    {
        if (input.IsEmpty) return string.Empty;

        int targetLength = (int)Math.Ceiling((double)input.Length / 5.0) * 8;

        return string.Create(targetLength, input, (buffer, state) =>
        {
            byte b = 0;
            byte b2 = 5;
            int num2 = 0;

            foreach (byte b3 in state)
            {
                b = (byte)(b | (b3 >> 8 - b2));
                buffer[num2++] = ValueToChar(b);
                if (b2 < 4)
                {
                    b = (byte)((b3 >> 3 - b2) & 0x1F);
                    buffer[num2++] = ValueToChar(b);
                    b2 += 5;
                }

                b2 -= 3;
                b = (byte)((b3 << (int)b2) & 0x1F);
            }

            if (num2 != targetLength)
            {
                buffer[num2++] = ValueToChar(b);
                while (num2 != targetLength)
                {
                    buffer[num2++] = '=';
                }
            }
        });
    }

    private static char ValueToChar(byte value)
    {
        if (value < 26)
        {
            return (char)(65 + value); // A-Z
        }
        return (char)(24 + value); // 2-7
    }
}