namespace LoginAuth.Model;

public sealed class RefreshToken
{
    public required string Id { get; set; }
    public required string Token { get; set; }
    public required int AccountId { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public Account? Account { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int IsRevoked { get; set; }
    public bool IsActive => IsRevoked == 0 && DateTime.UtcNow >= ExpiresAt;
}