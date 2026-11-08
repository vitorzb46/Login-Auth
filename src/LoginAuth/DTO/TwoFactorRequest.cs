namespace LoginAuth.DTO;

public sealed record TwoFactorRequest(string Email, string Code);