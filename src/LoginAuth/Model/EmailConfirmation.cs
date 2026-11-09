namespace LoginAuth.Model;

public sealed class EmailConfirmation
{
    public required string AccountId { get; set; }
    public required string Code { get; set; }
    public required DateTime ExpiresAt { get; set; }
}