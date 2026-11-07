namespace LoginAuth.DTO;

public sealed record TwoFactorSetup(string SecretKey, string QrCodeUri);