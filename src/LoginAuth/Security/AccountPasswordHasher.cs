using LoginAuth.DTO;
using LoginAuth.Model;
using Microsoft.AspNetCore.Identity;

namespace LoginAuth.Security;

public class AccountPasswordHasher(PasswordHasher<string> hasher)
{
    private readonly PasswordHasher<string> _hasher = hasher;

    public string HashPassword(string email, string pwdToHash) => _hasher.HashPassword(email, pwdToHash);
    
    public Account HashPassword(RegisterRequest input)
    {
        string hashPw = _hasher.HashPassword(input.Email, input.Password);
        return new() { Email = input.Email, PasswordHash = hashPw};
    }

    public bool VerifyPassword(LoginRequest input, string passwordHash)
    {
        var isValid = _hasher.VerifyHashedPassword(input.Email, passwordHash, input.Password);
        if (isValid is PasswordVerificationResult.Failed) return false;
        return true;
    }
}