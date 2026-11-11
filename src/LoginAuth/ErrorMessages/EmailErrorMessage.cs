namespace LoginAuth.ErrorMessages;

public sealed class EmailErrorMessage
{
    public static readonly string InvalidCode = "Invalid code.";
    public static readonly string ExpiredCode = "Code expired. Re-send another one.";
    public static readonly string Success = "E-mail confirmed successfully!";
    public static readonly string InternalServerError = "dummy";
}