namespace LoginAuth.Model;

public sealed class Account
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
}