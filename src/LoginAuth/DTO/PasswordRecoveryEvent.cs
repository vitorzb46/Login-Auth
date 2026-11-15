using LoginAuth.Interfaces;

namespace LoginAuth.DTO;

public record PasswordRecoveryEvent(int UserId, string Email) : IEmailEvent;