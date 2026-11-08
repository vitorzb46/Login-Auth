namespace LoginAuth.ErrorMessages;

public sealed class AccountErrorMessage
{
    public static readonly string EmailOrPasswordInvalid = "Login or password invalid!";
    public static readonly string AccountNotFound = "Account not found!";
    public static readonly string ConfirmBeforeSetup = "2Fa needs the set up before confirmation!";
    public static readonly string TOTPInvalid = "TOTP code invalid or expired!";
    public static readonly string TwoFactorNotEnabled = "Two factors needs to be enabled!";
}