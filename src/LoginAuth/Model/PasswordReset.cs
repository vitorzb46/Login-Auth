namespace LoginAuth.Model;

public sealed class PasswordReset
{
    public int AccountId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}