namespace LoginAuth.DTO;

public sealed record ResetPasswordRequest(string Token, string NewPwd);