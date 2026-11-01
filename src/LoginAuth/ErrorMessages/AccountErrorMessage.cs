namespace LoginAuth.ErrorMessages;

public sealed class AccountErrorMessage
{
    public static readonly string EmailOrPasswordInvalid = "Login or password invalid!";
    public static readonly string AccountNotFound = "Account not found!";
    public static readonly string ConnectionStringNotFound = "Connection string not found!";
    public static readonly string DbPathNotFound = "Database file path not found!";
}