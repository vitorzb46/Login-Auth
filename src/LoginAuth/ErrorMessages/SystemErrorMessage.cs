namespace LoginAuth.ErrorMessages;

public sealed class SystemErrorMessage
{
    public static readonly string ConnectionStringNotFound = "Connection string not found!";
    public static readonly string DbPathNotFound = "Database file path not found!";
    public static readonly string SecretKeyNotFound = "Token's secret key not found!";
}