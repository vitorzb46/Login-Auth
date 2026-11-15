namespace LoginAuth.Interfaces;

public interface IEmailEvent
{
    int UserId { get; }
    string Email { get; }
}