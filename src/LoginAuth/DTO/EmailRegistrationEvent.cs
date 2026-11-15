using LoginAuth.Interfaces;

namespace LoginAuth.DTO;

public record EmailRegistrationEvent(int UserId, string Email) : IEmailEvent;